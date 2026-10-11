using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using PaymentsApi.Data;

namespace PaymentsApi.Services;

public enum ImportOutcome
{
    Imported,
    AlreadyImported,
    Failed
}

public record ImportRowResult(int Row, string Reference, ImportOutcome Outcome, string? Message);

public record ImportResult(int Imported, int AlreadyImported, int Failed, List<ImportRowResult> Rows);

public interface IPaymentImportService
{
    Task<ImportResult> ImportXmlAsync(Stream xml, CancellationToken cancellationToken = default);
}

// Imports a batch of payments from an XML file, like the payment files banks and
// billing systems exchange. Each row is checked on its own, so one bad row doesn't
// stop the rest, and every payment still goes through PaymentService's rules.
//
// Expected format:
// <PaymentBatch>
//   <Payment reference="BANK-0001" invoiceId="1" amountCents="2500" />
// </PaymentBatch>
public class PaymentImportService : IPaymentImportService
{
    public const int MaxRows = 500;
    private const long MaxCents = 100_000_000;

    private readonly PaymentsDbContext _db;
    private readonly IPaymentService _payments;

    public PaymentImportService(PaymentsDbContext db, IPaymentService payments)
    {
        _db = db;
        _payments = payments;
    }

    public async Task<ImportResult> ImportXmlAsync(Stream xml, CancellationToken cancellationToken = default)
    {
        var payments = await ReadPaymentsAsync(xml, cancellationToken);
        var rows = new List<ImportRowResult>();

        for (var i = 0; i < payments.Count; i++)
        {
            var element = payments[i];
            var rowNumber = i + 1;
            var reference = ((string?)element.Attribute("reference"))?.Trim() ?? string.Empty;

            try
            {
                var (invoiceId, amountCents) = ParseRow(element, reference);

                // The file's reference becomes the idempotency key, so uploading the
                // same file twice (or a file with a repeated row) never charges twice.
                var key = $"import:{reference}";
                if (await _db.Payments.AnyAsync(p => p.IdempotencyKey == key, cancellationToken))
                {
                    rows.Add(new ImportRowResult(rowNumber, reference, ImportOutcome.AlreadyImported, null));
                    continue;
                }

                await _payments.RecordPaymentAsync(invoiceId, amountCents, key);
                rows.Add(new ImportRowResult(rowNumber, reference, ImportOutcome.Imported, null));
            }
            catch (Exception ex) when (ex is PaymentRuleException or NotFoundException)
            {
                rows.Add(new ImportRowResult(rowNumber, reference, ImportOutcome.Failed, ex.Message));
            }
        }

        return new ImportResult(
            rows.Count(r => r.Outcome == ImportOutcome.Imported),
            rows.Count(r => r.Outcome == ImportOutcome.AlreadyImported),
            rows.Count(r => r.Outcome == ImportOutcome.Failed),
            rows);
    }

    private static async Task<List<XElement>> ReadPaymentsAsync(Stream xml, CancellationToken cancellationToken)
    {
        // DTDs are refused outright. That blocks XML External Entity (XXE) attacks,
        // where a file tries to make the server read local files or call other servers.
        var settings = new XmlReaderSettings
        {
            Async = true,
            DtdProcessing = DtdProcessing.Prohibit,
            IgnoreComments = true
        };

        XDocument document;
        try
        {
            using var reader = XmlReader.Create(xml, settings);
            document = await XDocument.LoadAsync(reader, LoadOptions.None, cancellationToken);
        }
        catch (XmlException ex)
        {
            throw new PaymentRuleException($"The file isn't valid XML: {ex.Message}");
        }

        if (document.Root is null || document.Root.Name.LocalName != "PaymentBatch")
        {
            throw new PaymentRuleException("The root element must be <PaymentBatch>.");
        }

        var payments = document.Root.Elements("Payment").ToList();
        if (payments.Count == 0)
        {
            throw new PaymentRuleException("The batch has no <Payment> rows.");
        }

        if (payments.Count > MaxRows)
        {
            throw new PaymentRuleException($"A batch can have at most {MaxRows} payments.");
        }

        return payments;
    }

    private static (int InvoiceId, long AmountCents) ParseRow(XElement element, string reference)
    {
        if (reference.Length is 0 or > 80)
        {
            throw new PaymentRuleException("Each payment needs a reference of 1 to 80 characters.");
        }

        if (!int.TryParse((string?)element.Attribute("invoiceId"), NumberStyles.None, CultureInfo.InvariantCulture, out var invoiceId))
        {
            throw new PaymentRuleException("invoiceId must be a whole number.");
        }

        if (!long.TryParse((string?)element.Attribute("amountCents"), NumberStyles.None, CultureInfo.InvariantCulture, out var amountCents))
        {
            throw new PaymentRuleException("amountCents must be a whole number of cents, like 2500 for $25.00.");
        }

        if (amountCents > MaxCents)
        {
            throw new PaymentRuleException("A single payment can't be more than $1,000,000.00.");
        }

        return (invoiceId, amountCents);
    }
}

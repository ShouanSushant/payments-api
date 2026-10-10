import { useMutation } from "@apollo/client";
import { voidInvoiceMutation } from "../graphql/operations";
import { centsToDollars } from "../money";
import { errorMessage } from "../errors";
import { statusLabels, type Invoice, type Notify } from "../types";

export function InvoiceTable({ invoices, onDone }: { invoices: Invoice[]; onDone: Notify }) {
  const [voidInvoice, { loading }] = useMutation(voidInvoiceMutation);

  async function handleVoid(id: number) {
    try {
      await voidInvoice({ variables: { invoiceId: id } });
      onDone(`Invoice #${id} voided.`);
    } catch (error) {
      onDone(errorMessage(error), true);
    }
  }

  return (
    <section className="card" aria-labelledby="invoices-heading">
      <h2 id="invoices-heading">Invoices</h2>
      {invoices.length === 0 ? (
        <p className="hint">No invoices yet.</p>
      ) : (
        <table>
          <thead>
            <tr>
              <th>#</th>
              <th>Customer</th>
              <th>Description</th>
              <th className="num">Total</th>
              <th className="num">Paid</th>
              <th className="num">Outstanding</th>
              <th>Status</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {/* React escapes these values, so a customer named "<script>" shows up as plain text. */}
            {invoices.map((inv) => (
              <tr key={inv.id}>
                <td>{inv.id}</td>
                <td>{inv.customerName}</td>
                <td>{inv.description}</td>
                <td className="num">{centsToDollars(inv.totalCents)}</td>
                <td className="num">{centsToDollars(inv.paidCents)}</td>
                <td className="num">{centsToDollars(inv.outstandingCents)}</td>
                <td>
                  <span className={`badge ${inv.status.toLowerCase()}`}>{statusLabels[inv.status]}</span>
                </td>
                <td>
                  {/* Only invoices with nothing paid can be voided; the server enforces this too. */}
                  {inv.status === "OPEN" && (
                    <button className="small" disabled={loading} onClick={() => handleVoid(inv.id)}>
                      Void
                    </button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  );
}

using PaymentsApi.Models;

namespace PaymentsApi.Data;

public static class SeedData
{
    // Adds a little sample data the first time the app runs, so the dashboard isn't empty.
    public static void Seed(PaymentsDbContext db)
    {
        if (db.Customers.Any())
        {
            return;
        }

        var maple = new Customer { Name = "Maple Leaf Coffee", Email = "billing@mapleleafcoffee.example" };
        var lakeshore = new Customer { Name = "Lakeshore Bikes", Email = "accounts@lakeshorebikes.example" };

        var partlyPaid = new Invoice
        {
            Customer = maple,
            Description = "POS terminal rental - September",
            TotalCents = 12_500,
            Status = InvoiceStatus.PartiallyPaid
        };
        partlyPaid.Payments.Add(new Payment { AmountCents = 5_000 });

        db.Customers.AddRange(maple, lakeshore);
        db.Invoices.AddRange(
            partlyPaid,
            new Invoice { Customer = maple, Description = "Card processing fees - September", TotalCents = 4_275 },
            new Invoice { Customer = lakeshore, Description = "Online checkout setup", TotalCents = 29_900 });

        db.SaveChanges();
    }
}

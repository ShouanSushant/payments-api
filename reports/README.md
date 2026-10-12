# SSRS reports

Two SQL Server Reporting Services (SSRS) reports built in Microsoft Report Builder on top of the Payments API data.

| Report | File | What it shows |
|---|---|---|
| Outstanding Invoices by Customer | `OutstandingByCustomer.rdl` | Per customer: invoices, amount invoiced, amount paid (payments minus refunds) and what's still owed, with a grand total. Voided invoices are left out. |
| Payments by Day | `PaymentsByDay.rdl` | Per day: number of payments, money received, money refunded and the net, with a grand total. |

## How it works

The API stores its data in SQLite. `setup-sqlserver.sql` copies it into a SQL Server database called `PaymentsReporting` and creates two views the reports read from:

- `dbo.vw_OutstandingByCustomer` works out "paid" the same way the API does: payments minus refunds, per invoice.
- `dbo.vw_PaymentsByDay` puts payments and refunds side by side for each day.

Money is stored as integer cents (like in the API) and only converted to dollars in the views.

## Run it

1. Install SQL Server Express, SQL Server Management Studio (SSMS) and Microsoft Report Builder (all free).
2. In SSMS, connect to `localhost\SQLEXPRESS` (Windows Authentication, tick *Trust server certificate*), open `setup-sqlserver.sql` and run it (F5). The two checks at the end should return rows.
3. Open either `.rdl` file in Report Builder and click **Run**. The reports connect with:
   `Data Source=localhost\SQLEXPRESS;Initial Catalog=PaymentsReporting;Encrypt=True;TrustServerCertificate=True`

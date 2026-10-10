# Payments API

A small invoicing and payments service built with **C# / ASP.NET Core 8**, **Entity Framework Core** and **SQLite**, with unit tests in **xUnit** and a plain HTML/CSS/JavaScript dashboard.

Customers get invoices, invoices take payments (full or partial), and payments can be refunded. The API enforces the rules that matter in payments: no overpaying, no refunding more than was paid, no double charges on retries.

## Features

- **Customers, invoices, payments, refunds** through a REST API
- **Invoice status** (Open → PartiallyPaid → Paid, or Void) worked out from the payments, so it can't drift out of sync
- **Idempotency keys**: if a client retries the same payment (double click, timeout), the customer is only charged once
- **Money stored as integer cents**, never floating point
- **API key authentication**: only a SHA-256 hash of the key is stored, compared in constant time
- **Input validation** on every request, plus clean JSON error responses (RFC 7807 problem details)
- **Swagger UI** for trying the API in the browser
- **Dashboard** page to create customers and invoices, take payments and issue refunds
- **Unit tests** covering the payment, refund and security rules

## Run it

You need the [.NET 8 SDK](https://dotnet.microsoft.com/download) (installed with Visual Studio 2022).

```bash
dotnet run --project src/PaymentsApi
```

Then open:

- Dashboard: http://localhost:5080
- Swagger: http://localhost:5080/swagger (click **Authorize** and enter the key)

The dev API key is `dev-key-change-me`. A `payments.db` SQLite file is created on first run with some sample data. Delete it to start fresh.

In Visual Studio: open `PaymentsApi.sln`, set **PaymentsApi** as the startup project and press F5.

## Run the tests

```bash
dotnet test
```

Each test runs against a fresh in-memory SQLite database.

## API

All `/api` requests need the header `X-Api-Key`. Amounts are in cents (`2500` = $25.00).

| Method | Route | What it does |
|---|---|---|
| GET | `/api/customers` | List customers |
| POST | `/api/customers` | Create a customer `{ name, email }` |
| GET | `/api/invoices` | List invoices with payments |
| GET | `/api/invoices/{id}` | One invoice |
| POST | `/api/invoices` | Create an invoice `{ customerId, totalCents, description }` |
| POST | `/api/invoices/{id}/void` | Void an invoice with nothing paid on it |
| POST | `/api/invoices/{id}/payments` | Pay `{ amountCents }`, optional `Idempotency-Key` header |
| POST | `/api/payments/{id}/refunds` | Refund `{ amountCents, reason }` |

Example:

```bash
curl -X POST http://localhost:5080/api/invoices/1/payments \
  -H "X-Api-Key: dev-key-change-me" \
  -H "Idempotency-Key: 7b1e5a52-checkout-42" \
  -H "Content-Type: application/json" \
  -d '{"amountCents": 2500}'
```

Sending that same request again returns the same payment instead of charging twice.

## Design notes

- **Layers:** controllers only handle HTTP. All business rules live in `PaymentService`, and the pure math lives in `InvoiceMath`, which keeps the rules easy to test.
- **Entities vs DTOs:** database entities never go straight out of the API. Requests and responses have their own classes, so internal fields don't leak and the database can change without breaking clients.
- **Transactions:** a payment and the invoice status change are saved in one `SaveChanges` call, so either both happen or neither does.
- **Database-level safety:** a unique index on `IdempotencyKey` backs up the idempotency check in code.
- **Changing the API key:** generate a SHA-256 hash of your new key and put it in `appsettings.json` under `Security:ApiKeySha256`. In PowerShell:

  ```powershell
  $bytes = [Text.Encoding]::UTF8.GetBytes("your-new-key")
  -join ([Security.Cryptography.SHA256]::Create().ComputeHash($bytes) | ForEach-Object { $_.ToString("X2") })
  ```

## What I'd add next

- Swap SQLite for SQL Server or PostgreSQL by changing the provider and connection string
- EF Core migrations instead of `EnsureCreated`
- Integration tests with `WebApplicationFactory`
- Row-level locking or optimistic concurrency for two payments landing at the same moment
- JWT auth with per-user roles

## How I built it

I built this with help from an AI coding assistant, then reviewed, ran and tested every part myself. I used it to scaffold the project and talk through design choices like idempotency keys and storing money as cents. The tests in `tests/PaymentsApi.Tests` are how I checked the rules actually hold.

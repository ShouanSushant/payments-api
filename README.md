# Payments API

[![CI](https://github.com/ShouanSushant/payments-api/actions/workflows/ci.yml/badge.svg)](https://github.com/ShouanSushant/payments-api/actions/workflows/ci.yml)

A small invoicing and payments service built with **C# / ASP.NET Core 8**, **Entity Framework Core** and **SQLite**, with a **GraphQL** endpoint (Hot Chocolate), unit tests in **xUnit**, and a **React + TypeScript** dashboard that talks to the API through **Apollo Client**.

Customers get invoices, invoices take payments (full or partial), and payments can be refunded. The API enforces the rules that matter in payments: no overpaying, no refunding more than was paid, no double charges on retries.

## Features

- **Customers, invoices, payments, refunds** through a REST API
- **Invoice status** (Open → PartiallyPaid → Paid, or Void) worked out from the payments, so it can't drift out of sync
- **Idempotency keys**: if a client retries the same payment (double click, timeout), the customer is only charged once
- **Money stored as integer cents**, never floating point
- **API key authentication**: only a SHA-256 hash of the key is stored, compared in constant time
- **Input validation** on every request, plus clean JSON error responses (RFC 7807 problem details)
- **GraphQL endpoint** (Hot Chocolate) alongside the REST API, sharing the same service layer
- **Swagger UI** for trying the API in the browser
- **React + TypeScript dashboard** (Vite, Apollo Client) to create customers and invoices, take payments and issue refunds, with TypeScript types generated from the GraphQL schema
- **Tests at three levels**: xUnit for the service rules and GraphQL operations, Vitest for React components, and Playwright for end-to-end browser tests against the running app
- **CI on every pull request** (GitHub Actions) and a **Dockerfile** that builds the whole app into one image

## Run it

You need the [.NET 8 SDK](https://dotnet.microsoft.com/download) (installed with Visual Studio 2022) and [Node.js 22 LTS](https://nodejs.org).

Build the dashboard once (it's written into `src/PaymentsApi/wwwroot`), then start the app:

```bash
cd client
npm install
npm run build
cd ../src/PaymentsApi
dotnet run
```

Then open:

- Dashboard: http://localhost:5080 (enter the API key at the top right)
- GraphQL editor: http://localhost:5080/graphql
- Swagger: http://localhost:5080/swagger (click **Authorize** and enter the key)

The dev API key is `dev-key-change-me`. A `payments.db` SQLite file is created on first run with some sample data. Delete it to start fresh.

In Visual Studio: open `PaymentsApi.sln`, set **PaymentsApi** as the startup project and press F5.

### Working on the dashboard

Keep the .NET app running, and in another terminal:

```bash
cd client
npm run dev
```

Vite serves the dashboard at http://localhost:5173 with hot reload and forwards `/graphql` to the .NET app. After changing `schema.graphql` or any query in `client/src`, run `npm run codegen` to regenerate the TypeScript types in `client/src/gql`.

## Run the tests

```bash
dotnet test            # xUnit: service rules, API key check, GraphQL operations
cd client
npm test               # Vitest: React components and money parsing
npm run e2e            # Playwright: builds the dashboard, starts the app, tests it in Chromium
```

Each .NET test runs against a fresh in-memory SQLite database. Before the first `npm run e2e`, install the browser once with `npx playwright install chromium`.

All three run on every pull request in GitHub Actions (`.github/workflows/ci.yml`).

## Run it with Docker

```bash
docker build -t payments-api .
docker run -p 8080:8080 -v payments-data:/data payments-api
```

Then open http://localhost:8080. The Dockerfile builds the React dashboard with Node, publishes the API with the .NET SDK, and copies only the result into the smaller ASP.NET runtime image. The SQLite database lives in the `payments-data` volume, so data survives restarts.

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

## GraphQL

The same operations are also available as GraphQL at `/graphql`, built with [Hot Chocolate](https://chillicream.com/docs/hotchocolate). Open http://localhost:5080/graphql in a browser for the query editor, and add the header `X-Api-Key: dev-key-change-me` in its connection settings.

```graphql
mutation {
  recordPayment(invoiceId: 1, amountCents: 2500, idempotencyKey: "checkout-42") {
    status
    outstandingCents
  }
}
```

Both APIs call the same `PaymentService`, so the rules are enforced once. Business rule errors come back with a `PAYMENT_RULE` or `NOT_FOUND` code in `extensions.code`.

`schema.graphql` is generated from the C# code. After changing the GraphQL types, regenerate it:

```bash
cd src/PaymentsApi
dotnet run -- schema export --output ../../schema.graphql
```

## Project layout

```
src/PaymentsApi/          ASP.NET Core app
  Controllers/            REST endpoints (HTTP only)
  GraphQL/                Hot Chocolate query, mutation and type definitions
  Services/               PaymentService (business rules) and InvoiceMath
  Data/, Models/, Dtos/   EF Core context, entities, request/response shapes
tests/PaymentsApi.Tests/  xUnit tests
client/                   React + TypeScript dashboard (Vite)
  src/graphql/            GraphQL operations used by the dashboard
  src/gql/                Types generated from schema.graphql (don't edit by hand)
  e2e/                    Playwright tests
schema.graphql            GraphQL schema exported from the .NET app
```

## Design notes

- **Layers:** controllers only handle HTTP. All business rules live in `PaymentService`, and the pure math lives in `InvoiceMath`, which keeps the rules easy to test.
- **Entities vs DTOs:** database entities never go straight out of the API. Requests and responses have their own classes, so internal fields don't leak and the database can change without breaking clients.
- **Transactions:** a payment and the invoice status change are saved in one `SaveChanges` call, so either both happen or neither does.
- **Database-level safety:** a unique index on `IdempotencyKey` backs up the idempotency check in code.
- **One service, two APIs:** REST and GraphQL are thin layers over the same `PaymentService`, so a rule can't be enforced in one API and forgotten in the other.
- **GraphQL and the DbContext:** Hot Chocolate resolves sibling fields in parallel by default, but EF Core's `DbContext` can only run one query at a time. The schema uses serial execution, and a test queries `customers` and `invoices` together to prove it.
- **Typed frontend:** the dashboard's TypeScript types are generated from `schema.graphql`, so renaming a field on the server breaks the frontend build instead of failing silently at runtime.
- **Changing the API key:** generate a SHA-256 hash of your new key and put it in `appsettings.json` under `Security:ApiKeySha256`. In PowerShell:

  ```powershell
  $bytes = [Text.Encoding]::UTF8.GetBytes("your-new-key")
  -join ([Security.Cryptography.SHA256]::Create().ComputeHash($bytes) | ForEach-Object { $_.ToString("X2") })
  ```

## What I'd add next

- Swap SQLite for SQL Server or PostgreSQL by changing the provider and connection string
- EF Core migrations instead of `EnsureCreated`
- Integration tests with `WebApplicationFactory` for the REST endpoints
- Deploy the Docker image to Azure App Service
- Row-level locking or optimistic concurrency for two payments landing at the same moment
- JWT auth with per-user roles

## How I built it

I built this with help from an AI coding assistant, then reviewed, ran and tested every part myself. I used it to scaffold the project and talk through design choices like idempotency keys, storing money as cents, and how to add GraphQL next to the REST API. Each feature went in through its own pull request with CI, and the tests are how I checked the rules actually hold.

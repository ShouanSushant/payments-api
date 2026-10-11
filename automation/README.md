# Power Automate: import payments from a CSV

A Power Automate Desktop flow that reads payments from a CSV file and posts each one to the Payments API, the way an office might load a day's payments without anyone typing them in.

## What it does

1. Reads `samples/payments.csv` (columns: `Reference`, `InvoiceId`, `AmountCents`).
2. For each row, calls `POST /api/invoices/{InvoiceId}/payments` with the API key and an `Idempotency-Key` built from the row's reference (`csv-<Reference>`).
3. Writes the HTTP status for each row to `samples/payments-results.txt`.

Running the flow twice doesn't charge anyone twice: the second run sends the same idempotency keys, and the API returns the original payments. A row that breaks a rule (like `PA-0003`, which pays more than the invoice owes) comes back as `400` and the flow carries on with the next row.

## Run it

1. Start the API: `cd src/PaymentsApi` then `dotnet run`.
2. Open Power Automate Desktop, create a new flow, and paste the contents of `import-payments.flow.txt` into the designer (Ctrl+V). The actions appear ready to run.
3. Update the two file paths in the flow if your repo isn't at `C:\Users\SHOUAN\source\repos\PaymentsApi`.
4. Run the flow, then open `samples/payments-results.txt`. You should see `200` for `PA-0001` and `PA-0002` and `400` for `PA-0003`.
5. Run it again. The results file gets the same lines added, but the database still has only one payment per reference.

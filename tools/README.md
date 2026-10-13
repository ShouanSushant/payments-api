# Tools

## `xml_batch_report.py`: check a payment file before importing it

A Python script that reads a payment batch XML file, checks every row with the same rules as the API's `POST /api/payments/import`, and writes a report you can open in a browser (HTML) or Excel (CSV). Nothing is sent to the API or the database, so it's safe to run on any file.

```
python tools/xml_batch_report.py samples/payment-batch.xml
python tools/xml_batch_report.py samples/payment-batch-with-errors.xml --out out
```

It checks:

- the file is under 1 MB, is valid XML, has a `<PaymentBatch>` root and 1 to 500 `<Payment>` rows
- no DTDs, which blocks XXE and "billion laughs" attacks (the API has the same rule)
- each row has a reference of 1 to 80 characters that isn't repeated in the file
- `invoiceId` and `amountCents` are whole numbers, and the amount is between 1 cent and $1,000,000.00

It exits with `0` when every row is OK, `1` when some rows failed, and `2` when the file can't be used at all, so it can run in a script or pipeline.

`samples/payment-batch-with-errors.xml` has a mistake on purpose in most rows to show what gets caught.

Run the tests (Python 3.10+, standard library only):

```
cd tools
python -m unittest -v
```

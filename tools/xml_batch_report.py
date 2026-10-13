"""Checks a payment batch XML file before it's imported and writes a report.

Uses the same rules as the API's XML import (PaymentImportService.cs), so you can
see which rows would fail without touching the database. Writes two files:

  <name>-report.html  a page you can open in a browser or send to someone
  <name>-report.csv   the same rows for Excel

Usage:
  python tools/xml_batch_report.py samples/payment-batch.xml
  python tools/xml_batch_report.py samples/payment-batch-with-errors.xml --out out
"""

from __future__ import annotations

import argparse
import csv
import html
import sys
import xml.etree.ElementTree as ET
from dataclasses import dataclass
from datetime import datetime
from pathlib import Path

MAX_ROWS = 500
MAX_CENTS = 100_000_000  # $1,000,000.00, same limit as the API
MAX_FILE_BYTES = 1_000_000  # the API rejects bigger uploads


@dataclass
class RowResult:
    row: int
    reference: str
    invoice_id: str
    amount_cents: str
    ok: bool
    message: str

    @property
    def amount(self) -> str:
        return f"${int(self.amount_cents) / 100:,.2f}" if self.amount_cents.isdigit() else ""


class BatchError(Exception):
    """The whole file is unusable (not a problem with one row)."""


def load_payments(path: Path) -> list[ET.Element]:
    data = path.read_bytes()
    if len(data) > MAX_FILE_BYTES:
        raise BatchError(f"The file is {len(data):,} bytes; the limit is {MAX_FILE_BYTES:,}.")

    # Same rule as the API: no DTDs at all. That blocks XXE and "billion laughs"
    # files, which use entity definitions to read local files or blow up memory.
    if b"<!DOCTYPE" in data.upper():
        raise BatchError("DTDs aren't allowed in payment files.")

    try:
        root = ET.fromstring(data)
    except ET.ParseError as ex:
        raise BatchError(f"The file isn't valid XML: {ex}") from ex

    if root.tag != "PaymentBatch":
        raise BatchError("The root element must be <PaymentBatch>.")

    payments = root.findall("Payment")
    if not payments:
        raise BatchError("The batch has no <Payment> rows.")
    if len(payments) > MAX_ROWS:
        raise BatchError(f"A batch can have at most {MAX_ROWS} payments.")
    return payments


def check_row(row: int, element: ET.Element, seen: set[str]) -> RowResult:
    reference = (element.get("reference") or "").strip()
    invoice_id = (element.get("invoiceId") or "").strip()
    amount_cents = (element.get("amountCents") or "").strip()

    def result(ok: bool, message: str) -> RowResult:
        return RowResult(row, reference, invoice_id, amount_cents, ok, message)

    if not 1 <= len(reference) <= 80:
        return result(False, "Each payment needs a reference of 1 to 80 characters.")
    if reference in seen:
        return result(False, "Same reference as an earlier row; the API would skip it as already imported.")
    seen.add(reference)
    if not invoice_id.isdigit():
        return result(False, "invoiceId must be a whole number.")
    if not amount_cents.isdigit():
        return result(False, "amountCents must be a whole number of cents, like 2500 for $25.00.")
    if int(amount_cents) == 0:
        return result(False, "amountCents must be more than 0.")
    if int(amount_cents) > MAX_CENTS:
        return result(False, "A single payment can't be more than $1,000,000.00.")
    return result(True, "")


def check_batch(path: Path) -> list[RowResult]:
    seen: set[str] = set()
    return [check_row(i, element, seen) for i, element in enumerate(load_payments(path), start=1)]


def write_csv(results: list[RowResult], out: Path) -> None:
    with out.open("w", newline="", encoding="utf-8") as f:
        writer = csv.writer(f)
        writer.writerow(["Row", "Reference", "InvoiceId", "AmountCents", "Amount", "Status", "Problem"])
        for r in results:
            writer.writerow([r.row, r.reference, r.invoice_id, r.amount_cents, r.amount, "OK" if r.ok else "Failed", r.message])


def write_html(results: list[RowResult], source: Path, out: Path) -> None:
    ok = [r for r in results if r.ok]
    failed = [r for r in results if not r.ok]
    total = sum(int(r.amount_cents) for r in ok) / 100
    e = html.escape

    rows = "\n".join(
        f"<tr class=\"{'ok' if r.ok else 'bad'}\"><td>{r.row}</td><td>{e(r.reference)}</td>"
        f"<td>{e(r.invoice_id)}</td><td class=\"num\">{e(r.amount)}</td>"
        f"<td>{'OK' if r.ok else 'Failed'}</td><td>{e(r.message)}</td></tr>"
        for r in results
    )

    out.write_text(f"""<!doctype html>
<html lang="en"><head><meta charset="utf-8"><title>Batch check: {e(source.name)}</title>
<style>
  body {{ font-family: system-ui, sans-serif; margin: 2rem; color: #1d2320; }}
  table {{ border-collapse: collapse; width: 100%; max-width: 960px; }}
  th, td {{ border-bottom: 1px solid #dde2de; padding: .4rem .6rem; text-align: left; }}
  .num {{ text-align: right; font-variant-numeric: tabular-nums; }}
  tr.bad td {{ background: #f8e3e5; }}
  .summary {{ margin: 1rem 0; }}
</style></head>
<body>
<h1>Batch check: {e(source.name)}</h1>
<p class="summary">{len(results)} rows: <strong>{len(ok)} OK</strong> (${total:,.2f}), <strong>{len(failed)} failed</strong>.
Checked {datetime.now():%Y-%m-%d %H:%M}.</p>
<table>
<thead><tr><th>Row</th><th>Reference</th><th>Invoice</th><th class="num">Amount</th><th>Status</th><th>Problem</th></tr></thead>
<tbody>
{rows}
</tbody></table>
</body></html>
""", encoding="utf-8")


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Check a payment batch XML file and write HTML + CSV reports.")
    parser.add_argument("file", type=Path, help="the XML batch file")
    parser.add_argument("--out", type=Path, default=Path("out"), help="folder for the reports (default: out)")
    args = parser.parse_args(argv)

    try:
        results = check_batch(args.file)
    except (BatchError, OSError) as ex:
        print(f"Can't use {args.file}: {ex}", file=sys.stderr)
        return 2

    args.out.mkdir(parents=True, exist_ok=True)
    html_path = args.out / f"{args.file.stem}-report.html"
    csv_path = args.out / f"{args.file.stem}-report.csv"
    write_html(results, args.file, html_path)
    write_csv(results, csv_path)

    failed = sum(not r.ok for r in results)
    print(f"{len(results)} rows, {len(results) - failed} OK, {failed} failed")
    print(f"Wrote {html_path} and {csv_path}")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())

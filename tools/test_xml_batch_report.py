import tempfile
import unittest
from pathlib import Path

from xml_batch_report import BatchError, check_batch, main

SAMPLES = Path(__file__).resolve().parent.parent / "samples"


def write(tmp: str, text: str) -> Path:
    path = Path(tmp) / "batch.xml"
    path.write_text(text, encoding="utf-8")
    return path


class CheckBatchTests(unittest.TestCase):
    def test_good_sample_has_no_failures(self):
        results = check_batch(SAMPLES / "payment-batch.xml")
        self.assertEqual(3, len(results))
        self.assertTrue(all(r.ok for r in results))

    def test_bad_sample_flags_each_problem(self):
        results = check_batch(SAMPLES / "payment-batch-with-errors.xml")
        self.assertEqual([True, False, False, False, False, False, True], [r.ok for r in results])
        self.assertIn("reference", results[1].message)
        self.assertIn("invoiceId", results[2].message)
        self.assertIn("whole number of cents", results[3].message)
        self.assertIn("1,000,000", results[4].message)
        self.assertIn("earlier row", results[5].message)

    def test_dtd_is_rejected(self):
        with tempfile.TemporaryDirectory() as tmp:
            path = write(tmp, '<?xml version="1.0"?><!DOCTYPE x [<!ENTITY e SYSTEM "file:///etc/passwd">]><PaymentBatch/>')
            with self.assertRaisesRegex(BatchError, "DTD"):
                check_batch(path)

    def test_wrong_root_is_rejected(self):
        with tempfile.TemporaryDirectory() as tmp:
            with self.assertRaisesRegex(BatchError, "PaymentBatch"):
                check_batch(write(tmp, "<Payments><Payment/></Payments>"))

    def test_main_writes_both_reports(self):
        with tempfile.TemporaryDirectory() as tmp:
            code = main([str(SAMPLES / "payment-batch-with-errors.xml"), "--out", tmp])
            self.assertEqual(1, code)
            html = (Path(tmp) / "payment-batch-with-errors-report.html").read_text(encoding="utf-8")
            self.assertIn("2 OK", html)
            self.assertIn("5 failed", html)
            self.assertTrue((Path(tmp) / "payment-batch-with-errors-report.csv").exists())


if __name__ == "__main__":
    unittest.main()

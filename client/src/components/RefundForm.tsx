import { useState, type FormEvent } from "react";
import { useMutation } from "@apollo/client";
import { refundPaymentMutation } from "../graphql/operations";
import { centsToDollars, dollarsToCents } from "../money";
import { errorMessage } from "../errors";
import type { Invoice, Notify } from "../types";
import { Field } from "./Field";

export function RefundForm({ invoices, onDone }: { invoices: Invoice[]; onDone: Notify }) {
  // Every payment that still has money left to refund.
  const refundable = invoices.flatMap((invoice) =>
    invoice.payments
      .filter((p) => p.amountCents > p.refundedCents)
      .map((payment) => ({ invoice, payment })),
  );

  const [paymentId, setPaymentId] = useState("");
  const [amount, setAmount] = useState("");
  const [reason, setReason] = useState("");
  const [refundPayment, { loading }] = useMutation(refundPaymentMutation);

  const selected = refundable.some(({ payment }) => String(payment.id) === paymentId)
    ? paymentId
    : String(refundable[0]?.payment.id ?? "");

  async function submit(event: FormEvent) {
    event.preventDefault();
    try {
      const amountCents = dollarsToCents(amount);
      const id = Number(selected);
      await refundPayment({ variables: { paymentId: id, amountCents, reason } });
      setAmount("");
      setReason("");
      onDone(`Refunded ${centsToDollars(amountCents)} on payment #${id}.`);
    } catch (error) {
      onDone(errorMessage(error), true);
    }
  }

  return (
    <section className="card" aria-labelledby="refund-heading">
      <h2 id="refund-heading">Refund a payment</h2>
      <form onSubmit={submit}>
        <Field id="refundPayment" label="Payment">
          <select id="refundPayment" value={selected} onChange={(e) => setPaymentId(e.target.value)} required>
            {refundable.length === 0 ? (
              <option value="">No payments to refund</option>
            ) : (
              refundable.map(({ invoice, payment }) => (
                <option key={payment.id} value={payment.id}>
                  Payment #{payment.id} on invoice #{invoice.id}:{" "}
                  {centsToDollars(payment.amountCents - payment.refundedCents)} refundable
                </option>
              ))
            )}
          </select>
        </Field>
        <Field id="refundAmount" label="Amount ($)">
          <input
            id="refundAmount"
            inputMode="decimal"
            placeholder="10.00"
            value={amount}
            onChange={(e) => setAmount(e.target.value)}
            required
          />
        </Field>
        <Field id="refundReason" label="Reason">
          <input
            id="refundReason"
            maxLength={200}
            placeholder="Damaged item"
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            required
          />
        </Field>
        <button type="submit" disabled={loading || refundable.length === 0}>
          Issue refund
        </button>
      </form>
    </section>
  );
}

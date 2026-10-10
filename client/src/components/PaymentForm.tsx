import { useState, type FormEvent } from "react";
import { useMutation } from "@apollo/client";
import { recordPaymentMutation } from "../graphql/operations";
import { centsToDollars, dollarsToCents } from "../money";
import { errorMessage } from "../errors";
import type { Invoice, Notify } from "../types";
import { Field } from "./Field";

type Props = {
  invoices: Invoice[];
  onDone: Notify;
  // Injectable so tests can use a fixed key instead of a random one.
  newIdempotencyKey?: () => string;
};

export function PaymentForm({ invoices, onDone, newIdempotencyKey = () => crypto.randomUUID() }: Props) {
  const payable = invoices.filter((i) => i.status === "OPEN" || i.status === "PARTIALLY_PAID");
  const [invoiceId, setInvoiceId] = useState("");
  const [amount, setAmount] = useState("");
  const [recordPayment, { loading }] = useMutation(recordPaymentMutation);

  // Fall back to the first payable invoice if the chosen one was just paid off.
  const selected = payable.some((i) => String(i.id) === invoiceId) ? invoiceId : String(payable[0]?.id ?? "");

  async function submit(event: FormEvent) {
    event.preventDefault();
    try {
      const amountCents = dollarsToCents(amount);
      const id = Number(selected);
      // A new key per click. If this exact request is retried, the server
      // recognizes the key and won't charge twice.
      await recordPayment({ variables: { invoiceId: id, amountCents, idempotencyKey: newIdempotencyKey() } });
      setAmount("");
      onDone(`Payment of ${centsToDollars(amountCents)} recorded on invoice #${id}.`);
    } catch (error) {
      onDone(errorMessage(error), true);
    }
  }

  return (
    <section className="card" aria-labelledby="payment-heading">
      <h2 id="payment-heading">Record a payment</h2>
      <form onSubmit={submit}>
        <Field id="payInvoice" label="Invoice">
          <select id="payInvoice" value={selected} onChange={(e) => setInvoiceId(e.target.value)} required>
            {payable.length === 0 ? (
              <option value="">No invoices waiting for payment</option>
            ) : (
              payable.map((i) => (
                <option key={i.id} value={i.id}>
                  #{i.id} {i.customerName}: {centsToDollars(i.outstandingCents)} owing
                </option>
              ))
            )}
          </select>
        </Field>
        <Field id="payAmount" label="Amount ($)">
          <input
            id="payAmount"
            inputMode="decimal"
            placeholder="25.00"
            value={amount}
            onChange={(e) => setAmount(e.target.value)}
            required
          />
        </Field>
        {/* Disabled while the request is in flight, so a double click can't send it twice. */}
        <button type="submit" disabled={loading || payable.length === 0}>
          Record payment
        </button>
        <p className="hint">Each click sends a unique idempotency key, so a retry can never charge twice.</p>
      </form>
    </section>
  );
}

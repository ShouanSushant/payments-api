import { useState, type FormEvent } from "react";
import { useMutation } from "@apollo/client";
import { createInvoiceMutation } from "../graphql/operations";
import { dollarsToCents } from "../money";
import { errorMessage } from "../errors";
import type { Customer, Notify } from "../types";
import { Field } from "./Field";

export function InvoiceForm({ customers, onDone }: { customers: Customer[]; onDone: Notify }) {
  const [customerId, setCustomerId] = useState("");
  const [amount, setAmount] = useState("");
  const [description, setDescription] = useState("");
  const [createInvoice, { loading }] = useMutation(createInvoiceMutation);

  const selected = customers.some((c) => String(c.id) === customerId)
    ? customerId
    : String(customers[0]?.id ?? "");

  async function submit(event: FormEvent) {
    event.preventDefault();
    try {
      const result = await createInvoice({
        variables: { customerId: Number(selected), totalCents: dollarsToCents(amount), description },
      });
      setAmount("");
      setDescription("");
      onDone(`Invoice #${result.data?.createInvoice.id} created.`);
    } catch (error) {
      onDone(errorMessage(error), true);
    }
  }

  return (
    <section className="card" aria-labelledby="invoice-heading">
      <h2 id="invoice-heading">New invoice</h2>
      <form onSubmit={submit}>
        <Field id="invoiceCustomer" label="Customer">
          <select id="invoiceCustomer" value={selected} onChange={(e) => setCustomerId(e.target.value)} required>
            {customers.length === 0 ? (
              <option value="">Add a customer first</option>
            ) : (
              customers.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name} ({c.email})
                </option>
              ))
            )}
          </select>
        </Field>
        <Field id="invoiceAmount" label="Amount ($)">
          <input
            id="invoiceAmount"
            inputMode="decimal"
            placeholder="120.00"
            value={amount}
            onChange={(e) => setAmount(e.target.value)}
            required
          />
        </Field>
        <Field id="invoiceDescription" label="Description">
          <input
            id="invoiceDescription"
            maxLength={200}
            placeholder="Card terminal rental"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            required
          />
        </Field>
        <button type="submit" disabled={loading || customers.length === 0}>
          Create invoice
        </button>
      </form>
    </section>
  );
}

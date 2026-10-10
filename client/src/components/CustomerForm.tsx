import { useState, type FormEvent } from "react";
import { useMutation } from "@apollo/client";
import { createCustomerMutation } from "../graphql/operations";
import { errorMessage } from "../errors";
import type { Notify } from "../types";
import { Field } from "./Field";

export function CustomerForm({ onDone }: { onDone: Notify }) {
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [createCustomer, { loading }] = useMutation(createCustomerMutation);

  async function submit(event: FormEvent) {
    event.preventDefault();
    try {
      const result = await createCustomer({ variables: { name, email } });
      setName("");
      setEmail("");
      onDone(`Customer ${result.data?.createCustomer.name} added.`);
    } catch (error) {
      onDone(errorMessage(error), true);
    }
  }

  return (
    <section className="card" aria-labelledby="customer-heading">
      <h2 id="customer-heading">New customer</h2>
      <form onSubmit={submit}>
        <Field id="customerName" label="Name">
          <input id="customerName" maxLength={100} value={name} onChange={(e) => setName(e.target.value)} required />
        </Field>
        <Field id="customerEmail" label="Email">
          <input
            id="customerEmail"
            type="email"
            maxLength={200}
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
          />
        </Field>
        <button type="submit" disabled={loading}>
          Add customer
        </button>
      </form>
    </section>
  );
}

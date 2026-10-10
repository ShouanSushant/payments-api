import { describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MockedProvider, type MockedResponse } from "@apollo/client/testing";
import { recordPaymentMutation } from "../graphql/operations";
import type { Invoice } from "../types";
import { PaymentForm } from "./PaymentForm";

function invoice(overrides: Partial<Invoice>): Invoice {
  return {
    id: 1,
    customerId: 1,
    customerName: "Maple Leaf Coffee",
    description: "POS terminal rental",
    totalCents: 10000,
    paidCents: 0,
    outstandingCents: 10000,
    status: "OPEN",
    createdAt: "2026-10-01T12:00:00Z",
    payments: [],
    ...overrides,
  };
}

const paymentMock: MockedResponse = {
  request: {
    query: recordPaymentMutation,
    variables: { invoiceId: 1, amountCents: 2550, idempotencyKey: "test-key" },
  },
  result: {
    data: {
      recordPayment: { __typename: "Invoice", id: 1, status: "PARTIALLY_PAID", paidCents: 2550, outstandingCents: 7450 },
    },
  },
};

function renderForm(invoices: Invoice[], mocks: MockedResponse[] = []) {
  const onDone = vi.fn();
  render(
    <MockedProvider mocks={mocks}>
      <PaymentForm invoices={invoices} onDone={onDone} newIdempotencyKey={() => "test-key"} />
    </MockedProvider>,
  );
  return onDone;
}

describe("PaymentForm", () => {
  it("only offers invoices that still have money owing", () => {
    renderForm([
      invoice({ id: 1, status: "OPEN" }),
      invoice({ id: 2, status: "PAID", outstandingCents: 0 }),
      invoice({ id: 3, status: "VOID" }),
      invoice({ id: 4, status: "PARTIALLY_PAID", outstandingCents: 4000 }),
    ]);

    const options = screen.getAllByRole("option").map((o) => o.textContent);
    expect(options).toEqual([
      "#1 Maple Leaf Coffee: $100.00 owing",
      "#4 Maple Leaf Coffee: $40.00 owing",
    ]);
  });

  it("sends the amount in cents with an idempotency key", async () => {
    const onDone = renderForm([invoice({})], [paymentMock]);

    await userEvent.type(screen.getByLabelText("Amount ($)"), "25.50");
    await userEvent.click(screen.getByRole("button", { name: "Record payment" }));

    await waitFor(() => expect(onDone).toHaveBeenCalledWith("Payment of $25.50 recorded on invoice #1."));
  });

  it("shows the server's rule error instead of a success message", async () => {
    const overpay: MockedResponse = {
      request: {
        query: recordPaymentMutation,
        variables: { invoiceId: 1, amountCents: 20000, idempotencyKey: "test-key" },
      },
      result: {
        errors: [
          {
            message: "Payment of 20000 cents is more than the outstanding balance of 10000 cents.",
            extensions: { code: "PAYMENT_RULE" },
          },
        ],
      },
    };
    const onDone = renderForm([invoice({})], [overpay]);

    await userEvent.type(screen.getByLabelText("Amount ($)"), "200");
    await userEvent.click(screen.getByRole("button", { name: "Record payment" }));

    await waitFor(() =>
      expect(onDone).toHaveBeenCalledWith(
        "Payment of 20000 cents is more than the outstanding balance of 10000 cents.",
        true,
      ),
    );
  });

  it("rejects a badly formatted amount without calling the server", async () => {
    // No mocks: if the form sent a request, MockedProvider would return an error.
    const onDone = renderForm([invoice({})]);

    await userEvent.type(screen.getByLabelText("Amount ($)"), "12.345");
    await userEvent.click(screen.getByRole("button", { name: "Record payment" }));

    expect(onDone).toHaveBeenCalledWith("Enter an amount like 25 or 25.50", true);
  });

  it("disables the button when nothing can be paid", () => {
    renderForm([invoice({ status: "PAID", outstandingCents: 0 })]);

    expect(screen.getByRole("option")).toHaveTextContent("No invoices waiting for payment");
    expect(screen.getByRole("button", { name: "Record payment" })).toBeDisabled();
  });
});

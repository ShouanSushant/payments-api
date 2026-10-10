import { describe, expect, it, vi } from "vitest";
import { render, screen, within } from "@testing-library/react";
import { MockedProvider } from "@apollo/client/testing";
import type { Invoice } from "../types";
import { InvoiceTable } from "./InvoiceTable";

const base: Invoice = {
  id: 1,
  customerId: 1,
  customerName: "Lakeshore Bikes",
  description: "Online checkout setup",
  totalCents: 29900,
  paidCents: 0,
  outstandingCents: 29900,
  status: "OPEN",
  createdAt: "2026-10-01T12:00:00Z",
  payments: [],
};

function renderTable(invoices: Invoice[]) {
  render(
    <MockedProvider mocks={[]}>
      <InvoiceTable invoices={invoices} onDone={vi.fn()} />
    </MockedProvider>,
  );
}

describe("InvoiceTable", () => {
  it("shows money as dollars and a readable status", () => {
    renderTable([{ ...base, paidCents: 5000, outstandingCents: 24900, status: "PARTIALLY_PAID" }]);

    const row = screen.getAllByRole("row")[1];
    expect(within(row).getByText("$299.00")).toBeInTheDocument();
    expect(within(row).getByText("$50.00")).toBeInTheDocument();
    expect(within(row).getByText("$249.00")).toBeInTheDocument();
    expect(within(row).getByText("Partially paid")).toBeInTheDocument();
  });

  it("only lets open invoices be voided", () => {
    renderTable([
      { ...base, id: 1, status: "OPEN" },
      { ...base, id: 2, status: "PAID", paidCents: 29900, outstandingCents: 0 },
    ]);

    const [, openRow, paidRow] = screen.getAllByRole("row");
    expect(within(openRow).getByRole("button", { name: "Void" })).toBeInTheDocument();
    expect(within(paidRow).queryByRole("button", { name: "Void" })).not.toBeInTheDocument();
  });

  it("shows customer names as text, not HTML", () => {
    renderTable([{ ...base, customerName: "<img src=x onerror=alert(1)>" }]);

    expect(screen.getByText("<img src=x onerror=alert(1)>")).toBeInTheDocument();
    expect(document.querySelector("img")).toBeNull();
  });
});

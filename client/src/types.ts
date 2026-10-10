import type { DashboardQuery, InvoiceStatus } from "./gql/graphql";

// Shapes come straight from the generated query types, so they always match the schema.
export type Invoice = DashboardQuery["invoices"][number];
export type Customer = DashboardQuery["customers"][number];

// Reports the result of an action to the page: a success or an error message.
export type Notify = (message: string, isError?: boolean) => void;

export const statusLabels: Record<InvoiceStatus, string> = {
  OPEN: "Open",
  PARTIALLY_PAID: "Partially paid",
  PAID: "Paid",
  VOID: "Void",
};

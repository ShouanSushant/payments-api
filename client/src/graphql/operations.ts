import { graphql } from "../gql";

// Every operation the dashboard runs. `npm run codegen` checks each one against
// schema.graphql and generates its TypeScript types, so a typo in a field name
// is a build error instead of a runtime bug.

export const dashboardQuery = graphql(`
  query Dashboard {
    customers {
      id
      name
      email
    }
    invoices {
      id
      customerId
      customerName
      description
      totalCents
      paidCents
      outstandingCents
      status
      createdAt
      payments {
        id
        amountCents
        refundedCents
        createdAt
      }
    }
  }
`);

export const createCustomerMutation = graphql(`
  mutation CreateCustomer($name: String!, $email: String!) {
    createCustomer(name: $name, email: $email) {
      id
      name
    }
  }
`);

export const createInvoiceMutation = graphql(`
  mutation CreateInvoice($customerId: Int!, $totalCents: Long!, $description: String!) {
    createInvoice(customerId: $customerId, totalCents: $totalCents, description: $description) {
      id
    }
  }
`);

export const recordPaymentMutation = graphql(`
  mutation RecordPayment($invoiceId: Int!, $amountCents: Long!, $idempotencyKey: String) {
    recordPayment(invoiceId: $invoiceId, amountCents: $amountCents, idempotencyKey: $idempotencyKey) {
      id
      status
      paidCents
      outstandingCents
    }
  }
`);

export const refundPaymentMutation = graphql(`
  mutation RefundPayment($paymentId: Int!, $amountCents: Long!, $reason: String!) {
    refundPayment(paymentId: $paymentId, amountCents: $amountCents, reason: $reason) {
      id
      amountCents
    }
  }
`);

export const voidInvoiceMutation = graphql(`
  mutation VoidInvoice($invoiceId: Int!) {
    voidInvoice(invoiceId: $invoiceId) {
      id
      status
    }
  }
`);

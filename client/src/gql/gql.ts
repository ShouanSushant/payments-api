/* eslint-disable */
import * as types from './graphql';
import type { TypedDocumentNode as DocumentNode } from '@graphql-typed-document-node/core';

/**
 * Map of all GraphQL operations in the project.
 *
 * This map has several performance disadvantages:
 * 1. It is not tree-shakeable, so it will include all operations in the project.
 * 2. It is not minifiable, so the string of a GraphQL query will be multiple times inside the bundle.
 * 3. It does not support dead code elimination, so it will add unused operations.
 *
 * Therefore it is highly recommended to use the babel or swc plugin for production.
 * Learn more about it here: https://the-guild.dev/graphql/codegen/plugins/presets/preset-client#reducing-bundle-size
 */
type Documents = {
    "\n  query Dashboard {\n    customers {\n      id\n      name\n      email\n    }\n    invoices {\n      id\n      customerId\n      customerName\n      description\n      totalCents\n      paidCents\n      outstandingCents\n      status\n      createdAt\n      payments {\n        id\n        amountCents\n        refundedCents\n        createdAt\n      }\n    }\n  }\n": typeof types.DashboardDocument,
    "\n  mutation CreateCustomer($name: String!, $email: String!) {\n    createCustomer(name: $name, email: $email) {\n      id\n      name\n    }\n  }\n": typeof types.CreateCustomerDocument,
    "\n  mutation CreateInvoice($customerId: Int!, $totalCents: Long!, $description: String!) {\n    createInvoice(customerId: $customerId, totalCents: $totalCents, description: $description) {\n      id\n    }\n  }\n": typeof types.CreateInvoiceDocument,
    "\n  mutation RecordPayment($invoiceId: Int!, $amountCents: Long!, $idempotencyKey: String) {\n    recordPayment(invoiceId: $invoiceId, amountCents: $amountCents, idempotencyKey: $idempotencyKey) {\n      id\n      status\n      paidCents\n      outstandingCents\n    }\n  }\n": typeof types.RecordPaymentDocument,
    "\n  mutation RefundPayment($paymentId: Int!, $amountCents: Long!, $reason: String!) {\n    refundPayment(paymentId: $paymentId, amountCents: $amountCents, reason: $reason) {\n      id\n      amountCents\n    }\n  }\n": typeof types.RefundPaymentDocument,
    "\n  mutation VoidInvoice($invoiceId: Int!) {\n    voidInvoice(invoiceId: $invoiceId) {\n      id\n      status\n    }\n  }\n": typeof types.VoidInvoiceDocument,
};
const documents: Documents = {
    "\n  query Dashboard {\n    customers {\n      id\n      name\n      email\n    }\n    invoices {\n      id\n      customerId\n      customerName\n      description\n      totalCents\n      paidCents\n      outstandingCents\n      status\n      createdAt\n      payments {\n        id\n        amountCents\n        refundedCents\n        createdAt\n      }\n    }\n  }\n": types.DashboardDocument,
    "\n  mutation CreateCustomer($name: String!, $email: String!) {\n    createCustomer(name: $name, email: $email) {\n      id\n      name\n    }\n  }\n": types.CreateCustomerDocument,
    "\n  mutation CreateInvoice($customerId: Int!, $totalCents: Long!, $description: String!) {\n    createInvoice(customerId: $customerId, totalCents: $totalCents, description: $description) {\n      id\n    }\n  }\n": types.CreateInvoiceDocument,
    "\n  mutation RecordPayment($invoiceId: Int!, $amountCents: Long!, $idempotencyKey: String) {\n    recordPayment(invoiceId: $invoiceId, amountCents: $amountCents, idempotencyKey: $idempotencyKey) {\n      id\n      status\n      paidCents\n      outstandingCents\n    }\n  }\n": types.RecordPaymentDocument,
    "\n  mutation RefundPayment($paymentId: Int!, $amountCents: Long!, $reason: String!) {\n    refundPayment(paymentId: $paymentId, amountCents: $amountCents, reason: $reason) {\n      id\n      amountCents\n    }\n  }\n": types.RefundPaymentDocument,
    "\n  mutation VoidInvoice($invoiceId: Int!) {\n    voidInvoice(invoiceId: $invoiceId) {\n      id\n      status\n    }\n  }\n": types.VoidInvoiceDocument,
};

/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 *
 *
 * @example
 * ```ts
 * const query = graphql(`query GetUser($id: ID!) { user(id: $id) { name } }`);
 * ```
 *
 * The query argument is unknown!
 * Please regenerate the types.
 */
export function graphql(source: string): unknown;

/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(source: "\n  query Dashboard {\n    customers {\n      id\n      name\n      email\n    }\n    invoices {\n      id\n      customerId\n      customerName\n      description\n      totalCents\n      paidCents\n      outstandingCents\n      status\n      createdAt\n      payments {\n        id\n        amountCents\n        refundedCents\n        createdAt\n      }\n    }\n  }\n"): (typeof documents)["\n  query Dashboard {\n    customers {\n      id\n      name\n      email\n    }\n    invoices {\n      id\n      customerId\n      customerName\n      description\n      totalCents\n      paidCents\n      outstandingCents\n      status\n      createdAt\n      payments {\n        id\n        amountCents\n        refundedCents\n        createdAt\n      }\n    }\n  }\n"];
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(source: "\n  mutation CreateCustomer($name: String!, $email: String!) {\n    createCustomer(name: $name, email: $email) {\n      id\n      name\n    }\n  }\n"): (typeof documents)["\n  mutation CreateCustomer($name: String!, $email: String!) {\n    createCustomer(name: $name, email: $email) {\n      id\n      name\n    }\n  }\n"];
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(source: "\n  mutation CreateInvoice($customerId: Int!, $totalCents: Long!, $description: String!) {\n    createInvoice(customerId: $customerId, totalCents: $totalCents, description: $description) {\n      id\n    }\n  }\n"): (typeof documents)["\n  mutation CreateInvoice($customerId: Int!, $totalCents: Long!, $description: String!) {\n    createInvoice(customerId: $customerId, totalCents: $totalCents, description: $description) {\n      id\n    }\n  }\n"];
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(source: "\n  mutation RecordPayment($invoiceId: Int!, $amountCents: Long!, $idempotencyKey: String) {\n    recordPayment(invoiceId: $invoiceId, amountCents: $amountCents, idempotencyKey: $idempotencyKey) {\n      id\n      status\n      paidCents\n      outstandingCents\n    }\n  }\n"): (typeof documents)["\n  mutation RecordPayment($invoiceId: Int!, $amountCents: Long!, $idempotencyKey: String) {\n    recordPayment(invoiceId: $invoiceId, amountCents: $amountCents, idempotencyKey: $idempotencyKey) {\n      id\n      status\n      paidCents\n      outstandingCents\n    }\n  }\n"];
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(source: "\n  mutation RefundPayment($paymentId: Int!, $amountCents: Long!, $reason: String!) {\n    refundPayment(paymentId: $paymentId, amountCents: $amountCents, reason: $reason) {\n      id\n      amountCents\n    }\n  }\n"): (typeof documents)["\n  mutation RefundPayment($paymentId: Int!, $amountCents: Long!, $reason: String!) {\n    refundPayment(paymentId: $paymentId, amountCents: $amountCents, reason: $reason) {\n      id\n      amountCents\n    }\n  }\n"];
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(source: "\n  mutation VoidInvoice($invoiceId: Int!) {\n    voidInvoice(invoiceId: $invoiceId) {\n      id\n      status\n    }\n  }\n"): (typeof documents)["\n  mutation VoidInvoice($invoiceId: Int!) {\n    voidInvoice(invoiceId: $invoiceId) {\n      id\n      status\n    }\n  }\n"];

export function graphql(source: string) {
  return (documents as any)[source] ?? {};
}

export type DocumentType<TDocumentNode extends DocumentNode<any, any>> = TDocumentNode extends DocumentNode<  infer TType,  any>  ? TType  : never;
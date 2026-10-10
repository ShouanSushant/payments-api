import { expect, test, type Page } from "@playwright/test";

// These tests drive the real dashboard in a browser against the real .NET app
// and SQLite database, so each one makes its own uniquely named customer.

const API_KEY = "dev-key-change-me";

async function connect(page: Page) {
  await page.goto("/");
  await page.getByLabel("API key").fill(API_KEY);
  await page.getByRole("button", { name: "Connect" }).click();
  // The first GraphQL request after the app starts is slow (Hot Chocolate builds the
  // schema and EF Core builds its model on first use), so allow extra time here.
  await expect(page.getByRole("region", { name: "Invoices" }).getByRole("table")).toBeVisible({ timeout: 30_000 });
}

// Creates a customer and an invoice through the UI, returns the invoice's row and id.
async function createInvoice(page: Page, amount: string) {
  const stamp = `${Date.now()}-${Math.floor(Math.random() * 1000)}`;
  const name = `E2E Customer ${stamp}`;
  const email = `e2e-${stamp}@example.com`;

  const customerForm = page.getByRole("region", { name: "New customer" });
  await customerForm.getByLabel("Name").fill(name);
  await customerForm.getByLabel("Email").fill(email);
  await customerForm.getByRole("button", { name: "Add customer" }).click();
  await expect(page.getByRole("status")).toHaveText(`Customer ${name} added.`);

  const invoiceForm = page.getByRole("region", { name: "New invoice" });
  await invoiceForm.getByLabel("Customer").selectOption({ label: `${name} (${email})` });
  await invoiceForm.getByLabel("Amount ($)").fill(amount);
  await invoiceForm.getByLabel("Description").fill("E2E test invoice");
  await invoiceForm.getByRole("button", { name: "Create invoice" }).click();
  await expect(page.getByRole("status")).toContainText("created");

  const row = page.getByRole("row").filter({ hasText: name });
  await expect(row).toContainText("Open");
  const invoiceId = (await row.getByRole("cell").first().textContent())!.trim();
  return { row, invoiceId };
}

test("a customer pays an invoice in full", async ({ page }) => {
  await connect(page);
  const { row, invoiceId } = await createInvoice(page, "50.00");

  const paymentForm = page.getByRole("region", { name: "Record a payment" });
  await paymentForm.getByLabel("Invoice").selectOption(invoiceId);
  await paymentForm.getByLabel("Amount ($)").fill("50");
  await paymentForm.getByRole("button", { name: "Record payment" }).click();

  await expect(page.getByRole("status")).toHaveText(`Payment of $50.00 recorded on invoice #${invoiceId}.`);
  await expect(row.getByText("Paid", { exact: true })).toBeVisible();
  await expect(row.getByRole("cell").nth(5)).toHaveText("$0.00"); // Outstanding
});

test("paying more than is owed is refused", async ({ page }) => {
  await connect(page);
  const { row, invoiceId } = await createInvoice(page, "10.00");

  const paymentForm = page.getByRole("region", { name: "Record a payment" });
  await paymentForm.getByLabel("Invoice").selectOption(invoiceId);
  await paymentForm.getByLabel("Amount ($)").fill("20");
  await paymentForm.getByRole("button", { name: "Record payment" }).click();

  await expect(page.getByRole("status")).toContainText("more than the outstanding balance");
  await expect(row).toContainText("Open");
});

test("a wrong API key shows a clear error", async ({ page }) => {
  await page.goto("/");
  await page.getByLabel("API key").fill("not-the-key");
  await page.getByRole("button", { name: "Connect" }).click();

  await expect(page.getByRole("status")).toHaveText("That API key didn't work. Check it in the box at the top right.");
});

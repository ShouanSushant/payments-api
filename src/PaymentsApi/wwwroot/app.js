// Plain JavaScript, no framework. Talks to the API with fetch().

const $ = (id) => document.getElementById(id);

// ---------- Money helpers ----------
// Dollars are parsed as text into whole cents, never through floating point
// ("0.29" * 100 in JS gives 28.999999999999996).
function dollarsToCents(text) {
  const match = /^\s*(\d{1,7})(?:\.(\d{1,2}))?\s*$/.exec(text);
  if (!match) {
    throw new Error("Enter an amount like 25 or 25.50");
  }
  const whole = Number(match[1]);
  const fraction = Number((match[2] || "").padEnd(2, "0"));
  return whole * 100 + fraction;
}

function centsToDollars(cents) {
  return (cents / 100).toLocaleString("en-CA", { style: "currency", currency: "CAD" });
}

// ---------- API calls ----------
async function api(path, options = {}) {
  if (!$("apiKey").value.trim()) {
    throw new Error("Enter your API key in the box at the top right, then press Enter.");
  }

  const response = await fetch(path, {
    ...options,
    headers: {
      "Content-Type": "application/json",
      "X-Api-Key": $("apiKey").value,
      ...(options.headers || {})
    }
  });

  if (response.status === 401) {
    throw new Error("That API key didn't work. Check it in the box at the top right and press Enter.");
  }

  if (!response.ok) {
    let detail = `Request failed (${response.status})`;
    try {
      const problem = await response.json();
      if (problem.errors) {
        // Validation errors: { errors: { Field: ["message"] } }
        detail = Object.values(problem.errors).flat().join(" ");
      } else if (problem.detail) {
        detail = problem.detail;
      }
    } catch {
      // Response had no JSON body; keep the generic message.
    }
    throw new Error(detail);
  }

  return response.status === 204 ? null : response.json();
}

// ---------- Rendering ----------
// Everything user-supplied is written with textContent, never innerHTML,
// so a customer named "<script>..." is shown as text instead of running.
function cell(text, className) {
  const td = document.createElement("td");
  td.textContent = text;
  if (className) td.className = className;
  return td;
}

// Fills a dropdown, or shows a disabled hint when there's nothing to pick.
function fillSelect(select, options, emptyText) {
  if (options.length === 0) {
    const hint = option("", emptyText);
    hint.disabled = true;
    hint.selected = true;
    select.replaceChildren(hint);
  } else {
    select.replaceChildren(...options);
  }
}

function option(value, text) {
  const opt = document.createElement("option");
  opt.value = value;
  opt.textContent = text;
  return opt;
}

function showMessage(text, isError) {
  const box = $("message");
  box.textContent = text;
  box.className = isError ? "message error" : "message ok";
  box.hidden = false;
}

function renderInvoices(invoices) {
  const rows = $("invoiceRows");
  rows.replaceChildren();

  for (const inv of invoices) {
    const tr = document.createElement("tr");
    tr.append(
      cell(inv.id),
      cell(inv.customerName),
      cell(inv.description),
      cell(centsToDollars(inv.totalCents), "num"),
      cell(centsToDollars(inv.paidCents), "num"),
      cell(centsToDollars(inv.outstandingCents), "num")
    );

    const statusCell = document.createElement("td");
    const badge = document.createElement("span");
    badge.className = `badge ${inv.status.toLowerCase()}`;
    badge.textContent = inv.status;
    statusCell.append(badge);
    tr.append(statusCell);

    const actionCell = document.createElement("td");
    if (inv.status === "Open") {
      const voidButton = document.createElement("button");
      voidButton.textContent = "Void";
      voidButton.className = "small";
      voidButton.addEventListener("click", () => run(async () => {
        await api(`/api/invoices/${inv.id}/void`, { method: "POST" });
        return `Invoice #${inv.id} voided.`;
      }));
      actionCell.append(voidButton);
    }
    tr.append(actionCell);
    rows.append(tr);
  }

  const payable = invoices.filter(i => i.status === "Open" || i.status === "PartiallyPaid");
  fillSelect($("payInvoice"), payable.map(i =>
    option(i.id, `#${i.id} ${i.customerName}: ${centsToDollars(i.outstandingCents)} owing`)),
    "No invoices waiting for payment");

  const refundable = invoices.flatMap(i => i.payments
    .filter(p => p.amountCents > p.refundedCents)
    .map(p => ({ invoice: i, payment: p })));
  fillSelect($("refundPayment"), refundable.map(({ invoice, payment }) =>
    option(payment.id,
      `Payment #${payment.id} on invoice #${invoice.id}: ${centsToDollars(payment.amountCents - payment.refundedCents)} refundable`)),
    "No payments to refund");
}

function renderCustomers(customers) {
  fillSelect($("invoiceCustomer"), customers.map(c => option(c.id, `${c.name} (${c.email})`)),
    "Add a customer first");
}

async function refresh() {
  const [invoices, customers] = await Promise.all([api("/api/invoices"), api("/api/customers")]);
  renderInvoices(invoices);
  renderCustomers(customers);
}

// Runs an action, shows the result, reloads the data, and reports errors.
async function run(action) {
  try {
    const message = await action();
    await refresh();
    showMessage(message, false);
  } catch (err) {
    showMessage(err.message, true);
  }
}

// ---------- Forms ----------
function handle(formId, action) {
  const form = $(formId);
  form.addEventListener("submit", (event) => {
    event.preventDefault();
    const button = form.querySelector("button");
    button.disabled = true; // stops double submits
    run(async () => {
      const message = await action();
      form.reset();
      return message;
    }).finally(() => { button.disabled = false; });
  });
}

handle("paymentForm", async () => {
  const invoiceId = $("payInvoice").value;
  const amountCents = dollarsToCents($("payAmount").value);
  await api(`/api/invoices/${invoiceId}/payments`, {
    method: "POST",
    headers: { "Idempotency-Key": crypto.randomUUID() },
    body: JSON.stringify({ amountCents })
  });
  return `Payment of ${centsToDollars(amountCents)} recorded on invoice #${invoiceId}.`;
});

handle("refundForm", async () => {
  const paymentId = $("refundPayment").value;
  const amountCents = dollarsToCents($("refundAmount").value);
  await api(`/api/payments/${paymentId}/refunds`, {
    method: "POST",
    body: JSON.stringify({ amountCents, reason: $("refundReason").value })
  });
  return `Refunded ${centsToDollars(amountCents)} on payment #${paymentId}.`;
});

handle("invoiceForm", async () => {
  const created = await api("/api/invoices", {
    method: "POST",
    body: JSON.stringify({
      customerId: Number($("invoiceCustomer").value),
      totalCents: dollarsToCents($("invoiceAmount").value),
      description: $("invoiceDescription").value
    })
  });
  return `Invoice #${created.id} created.`;
});

handle("customerForm", async () => {
  const created = await api("/api/customers", {
    method: "POST",
    body: JSON.stringify({ name: $("customerName").value, email: $("customerEmail").value })
  });
  return `Customer ${created.name} added.`;
});

$("apiKey").addEventListener("change", () => run(async () => "API key accepted. Data loaded."));

refresh().catch(err => showMessage(err.message, true));

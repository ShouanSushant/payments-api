import { useState, type FormEvent } from "react";
import { useQuery } from "@apollo/client";
import { setApiKey } from "./apollo";
import { dashboardQuery } from "./graphql/operations";
import { errorMessage } from "./errors";
import type { Notify } from "./types";
import { InvoiceTable } from "./components/InvoiceTable";
import { PaymentForm } from "./components/PaymentForm";
import { RefundForm } from "./components/RefundForm";
import { InvoiceForm } from "./components/InvoiceForm";
import { CustomerForm } from "./components/CustomerForm";

const KEY_STORAGE = "payments-api-key";

// Remembers the key for this browser tab only (sessionStorage), so it isn't
// left behind after the tab is closed.
function loadSavedKey(): string {
  try {
    return sessionStorage.getItem(KEY_STORAGE) ?? "";
  } catch {
    return "";
  }
}

export function App() {
  const [apiKey, setKey] = useState(() => {
    const saved = loadSavedKey();
    setApiKey(saved);
    return saved;
  });
  const [keyInput, setKeyInput] = useState(apiKey);
  const [message, setMessage] = useState<{ text: string; isError: boolean } | null>(null);

  // One GraphQL request loads everything the page needs.
  const { data, error, refetch } = useQuery(dashboardQuery, { skip: !apiKey });

  const invoices = data?.invoices ?? [];
  const customers = data?.customers ?? [];

  const notify: Notify = (text, isError = false) => {
    setMessage({ text, isError });
    if (!isError) {
      // Reload so every table and dropdown reflects the change.
      refetch().catch(() => {});
    }
  };

  function connect(event: FormEvent) {
    event.preventDefault();
    const key = keyInput.trim();
    setApiKey(key);
    try {
      sessionStorage.setItem(KEY_STORAGE, key);
    } catch {
      // Storage can be blocked (private mode); the key still works for this page.
    }
    setMessage(null);
    if (key === apiKey) {
      refetch().catch(() => {});
    } else {
      setKey(key);
    }
  }

  const banner = message ?? (error ? { text: errorMessage(error), isError: true } : null);

  return (
    <>
      <header className="topbar">
        <h1>Payments Dashboard</h1>
        <form className="key" onSubmit={connect}>
          <label htmlFor="apiKey">API key</label>
          <input
            id="apiKey"
            type="password"
            autoComplete="off"
            value={keyInput}
            onChange={(e) => setKeyInput(e.target.value)}
          />
          <button type="submit">Connect</button>
        </form>
      </header>

      <main>
        {banner && (
          <div className={banner.isError ? "message error" : "message ok"} role="status">
            {banner.text}
          </div>
        )}
        {!apiKey && <div className="message">Enter your API key at the top right to load the dashboard.</div>}

        <InvoiceTable invoices={invoices} onDone={notify} />

        <div className="grid">
          <PaymentForm invoices={invoices} onDone={notify} />
          <RefundForm invoices={invoices} onDone={notify} />
          <InvoiceForm customers={customers} onDone={notify} />
          <CustomerForm onDone={notify} />
        </div>

        <p className="footer">
          GraphQL editor: <a href="/graphql">/graphql</a> · REST docs: <a href="/swagger">/swagger</a>
        </p>
      </main>
    </>
  );
}

import type { ReactNode } from "react";

// A label above its input. Kept as siblings (not nesting the input inside the
// label) so screen readers and tests see the label text on its own.
export function Field({ id, label, children }: { id: string; label: string; children: ReactNode }) {
  return (
    <div className="field">
      <label htmlFor={id}>{label}</label>
      {children}
    </div>
  );
}

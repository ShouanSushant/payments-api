// Dollars are parsed as text into whole cents, never through floating point
// ("0.29" * 100 in JavaScript gives 28.999999999999996).
export function dollarsToCents(text: string): number {
  const match = /^\s*(\d{1,7})(?:\.(\d{1,2}))?\s*$/.exec(text);
  if (!match) {
    throw new Error("Enter an amount like 25 or 25.50");
  }
  const whole = Number(match[1]);
  const fraction = Number((match[2] ?? "").padEnd(2, "0"));
  return whole * 100 + fraction;
}

export function centsToDollars(cents: number): string {
  return (cents / 100).toLocaleString("en-CA", { style: "currency", currency: "CAD" });
}

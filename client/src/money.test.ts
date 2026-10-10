import { describe, expect, it } from "vitest";
import { centsToDollars, dollarsToCents } from "./money";

describe("dollarsToCents", () => {
  it.each([
    ["25", 2500],
    ["25.5", 2550],
    ["25.50", 2550],
    ["0.29", 29], // 0.29 * 100 is 28.999... in floating point
    [" 1000.01 ", 100001],
  ])("turns %j into %i cents", (text, cents) => {
    expect(dollarsToCents(text)).toBe(cents);
  });

  it.each(["", "abc", "-5", "1.234", "1,000", "12345678"])("rejects %j", (text) => {
    expect(() => dollarsToCents(text)).toThrow("Enter an amount like 25 or 25.50");
  });
});

describe("centsToDollars", () => {
  it("formats cents as Canadian dollars", () => {
    expect(centsToDollars(12550)).toBe("$125.50");
  });
});

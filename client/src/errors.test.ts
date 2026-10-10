import { describe, expect, it } from "vitest";
import { ApolloError } from "@apollo/client";
import { GraphQLError } from "graphql";
import { errorMessage } from "./errors";

// Builds the kind of error Apollo produces for a non-2xx HTTP response.
function httpError(statusCode: number, result: unknown) {
  return new ApolloError({
    networkError: Object.assign(new Error(`Response not successful: Received status code ${statusCode}`), {
      name: "ServerError",
      statusCode,
      result,
      response: {} as Response,
    }),
  });
}

describe("errorMessage", () => {
  it("explains a rejected API key", () => {
    const error = httpError(401, { title: "Unauthorized", detail: "Missing or invalid API key." });
    expect(errorMessage(error)).toBe("That API key didn't work. Check it in the box at the top right.");
  });

  it("shows GraphQL errors returned with a 200", () => {
    const error = new ApolloError({ graphQLErrors: [new GraphQLError("This invoice is already void.")] });
    expect(errorMessage(error)).toBe("This invoice is already void.");
  });

  it("shows GraphQL errors returned with a 500 (how Hot Chocolate reports a failed mutation)", () => {
    const error = httpError(500, {
      errors: [{ message: "Payment of 2000 cents is more than the outstanding balance of 1000 cents." }],
      data: null,
    });
    expect(errorMessage(error)).toBe("Payment of 2000 cents is more than the outstanding balance of 1000 cents.");
  });

  it("says the server is down when there's no response at all", () => {
    const error = new ApolloError({ networkError: new TypeError("Failed to fetch") });
    expect(errorMessage(error)).toBe("Can't reach the server. Is the .NET app running?");
  });

  it("passes through plain errors like a bad amount", () => {
    expect(errorMessage(new Error("Enter an amount like 25 or 25.50"))).toBe("Enter an amount like 25 or 25.50");
  });
});

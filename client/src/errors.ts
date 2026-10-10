import { ApolloError, type ServerError } from "@apollo/client";

// Turns whatever went wrong into one sentence for the message banner.
export function errorMessage(error: unknown): string {
  if (error instanceof ApolloError) {
    const status = (error.networkError as ServerError | null)?.statusCode;
    if (status === 401) {
      return "That API key didn't work. Check it in the box at the top right.";
    }
    // Business rule errors from the server (overpaying, refunding too much, ...)
    // arrive as GraphQL errors with a readable message.
    if (error.graphQLErrors.length > 0) {
      return error.graphQLErrors.map((e) => e.message).join(" ");
    }
    if (error.networkError) {
      return "Can't reach the server. Is the .NET app running?";
    }
  }
  return error instanceof Error ? error.message : "Something went wrong.";
}

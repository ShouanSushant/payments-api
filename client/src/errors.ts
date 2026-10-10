import { ApolloError, type ServerError } from "@apollo/client";

type GraphQLErrorBody = { errors?: { message: string }[] };

// Turns whatever went wrong into one sentence for the message banner.
export function errorMessage(error: unknown): string {
  if (error instanceof ApolloError) {
    const network = error.networkError as Partial<ServerError> | null;

    if (network?.statusCode === 401) {
      return "That API key didn't work. Check it in the box at the top right.";
    }

    // Business rule errors from the server (overpaying, refunding too much, ...)
    // arrive as GraphQL errors with a readable message.
    if (error.graphQLErrors.length > 0) {
      return error.graphQLErrors.map((e) => e.message).join(" ");
    }

    // Hot Chocolate follows the GraphQL-over-HTTP spec: when a mutation fails and
    // there's no data to return, it answers with HTTP 500 and the errors in the body.
    // Apollo treats any non-2xx as a network error, so the real message is in the body.
    const bodyErrors = (network?.result as GraphQLErrorBody | undefined)?.errors;
    if (bodyErrors && bodyErrors.length > 0) {
      return bodyErrors.map((e) => e.message).join(" ");
    }

    if (network) {
      return "Can't reach the server. Is the .NET app running?";
    }
  }
  return error instanceof Error ? error.message : "Something went wrong.";
}

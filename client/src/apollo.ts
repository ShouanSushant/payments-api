import { ApolloClient, HttpLink, InMemoryCache, from } from "@apollo/client";
import { setContext } from "@apollo/client/link/context";

// The key lives in memory for this tab only. It's read on every request,
// so changing it takes effect on the next query.
let apiKey = "";

export function setApiKey(key: string) {
  apiKey = key;
}

const withApiKey = setContext((_, { headers }) => ({
  headers: { ...headers, "X-Api-Key": apiKey },
}));

export const client = new ApolloClient({
  link: from([withApiKey, new HttpLink({ uri: "/graphql" })]),
  // Invoices are cached by id, so when recordPayment returns the updated
  // invoice, the table row updates without another request.
  cache: new InMemoryCache(),
});

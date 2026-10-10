import type { CodegenConfig } from "@graphql-codegen/cli";

// Reads schema.graphql (exported from the .NET app) and every graphql(`...`)
// call in src, then writes TypeScript types for each query and mutation into
// src/gql. Run `npm run codegen` after the schema or an operation changes.
const config: CodegenConfig = {
  schema: "../schema.graphql",
  documents: ["src/**/*.{ts,tsx}", "!src/gql/**/*"],
  generates: {
    "./src/gql/": {
      preset: "client",
      presetConfig: { fragmentMasking: false },
      config: {
        scalars: { Long: "number", DateTime: "string" },
        enumsAsTypes: true,
        useTypeImports: true,
      },
    },
  },
};

export default config;

/// <reference types="vitest/config" />
import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig({
  plugins: [react()],
  build: {
    // ASP.NET Core serves whatever is in wwwroot, so `npm run build`
    // puts the dashboard where `dotnet run` will pick it up.
    outDir: "../src/PaymentsApi/wwwroot",
    emptyOutDir: true,
  },
  server: {
    port: 5173,
    // During `npm run dev`, API calls go to the .NET app on port 5080.
    proxy: {
      "/graphql": "http://localhost:5080",
      "/api": "http://localhost:5080",
      "/swagger": "http://localhost:5080",
    },
  },
  test: {
    environment: "jsdom",
    setupFiles: "./src/test/setup.ts",
    include: ["src/**/*.test.{ts,tsx}"],
  },
});

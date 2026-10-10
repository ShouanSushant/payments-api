import { defineConfig, devices } from "@playwright/test";

// Set E2E_BASE_URL to test an app that's already running somewhere else.
const externalServer = process.env.E2E_BASE_URL;

export default defineConfig({
  testDir: "./e2e",
  timeout: 30_000,
  reporter: process.env.CI ? [["github"], ["html", { open: "never" }]] : "list",
  use: {
    baseURL: externalServer ?? "http://localhost:5080",
    trace: "retain-on-failure",
  },
  projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }],
  // Starts the real .NET app (with the dashboard `npm run e2e` just built)
  // and waits for it, unless one is already running on port 5080.
  webServer: externalServer
    ? undefined
    : {
        command: "dotnet run",
        cwd: "../src/PaymentsApi",
        url: "http://localhost:5080",
        reuseExistingServer: !process.env.CI,
        timeout: 180_000,
      },
});

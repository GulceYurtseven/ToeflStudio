import { defineConfig, devices } from "@playwright/test";
export default defineConfig({
    testDir: "./tests",
    outputDir: "./real-test-results",
    testMatch: "**/*.real.spec.ts",
    timeout: 90000,
    workers: 1,
    use: {
        ...devices["Desktop Edge"],
        channel: "msedge",
        baseURL: "http://127.0.0.1:5187",
        screenshot: "only-on-failure",
        trace: "retain-on-failure",
    },
    reporter: [["list"]],
});

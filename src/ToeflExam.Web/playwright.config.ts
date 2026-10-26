import { defineConfig, devices } from "@playwright/test";
export default defineConfig({
    testDir: "./tests",
    testIgnore: "**/*.real.spec.ts",
    timeout: 30000,
    fullyParallel: true,
    workers: 2,
    reporter: [["list"]],
    use: {
        baseURL: "http://127.0.0.1:5173",
        channel: "msedge",
        screenshot: "only-on-failure",
        trace: "retain-on-failure",
    },
    projects: [
        {
            name: "desktop",
            use: {
                ...devices["Desktop Edge"],
                channel: "msedge",
                viewport: { width: 1440, height: 1000 },
            },
        },
        {
            name: "mobile",
            use: {
                ...devices["Pixel 7"],
                defaultBrowserType: "chromium",
                channel: "msedge",
            },
        },
    ],
    webServer: {
        command: "npm run dev -- --port 5173 --strictPort",
        url: "http://127.0.0.1:5173",
        reuseExistingServer: false,
    },
});

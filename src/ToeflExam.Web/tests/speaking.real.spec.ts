import {
    test,
    expect,
    type APIRequestContext,
    type APIResponse,
} from "@playwright/test";

// The complete suite creates accounts from one IP. Respect the production
// 20/min auth window instead of relaxing its limit for browser tests.
async function authPost(
    request: APIRequestContext,
    path: string,
    data: object,
) {
    let response: APIResponse;
    await expect
        .poll(
            async () => {
                response = await request.post(path, { data });
                if (response.status() === 429) return false;
                expect(response.ok(), await response.text()).toBeTruthy();
                return true;
            },
            { timeout: 65000, intervals: [5000] },
        )
        .toBeTruthy();
    return response!;
}
test.use({
    launchOptions: {
        args: [
            "--use-fake-device-for-media-stream",
            "--use-fake-ui-for-media-stream",
        ],
    },
    permissions: ["microphone"],
});
for (const mobile of [false, true]) {
    test(`Speaking real recording, expanded paths and persistent progress ${mobile ? "mobile" : "desktop"}`, async ({
        page,
        request,
    }) => {
        if (mobile) await page.setViewportSize({ width: 393, height: 851 });
        const credentials = {
        username: "student_" + crypto.randomUUID().replaceAll("-", "").slice(0, 20),
            email: `speaker-${crypto.randomUUID()}@example.test`,
            password: "Speaking!124578",
        };
        await authPost(request, "/api/auth/register", credentials);
        const tokens = await (
            await authPost(
                request,
                "/api/auth/login?useCookies=false",
                credentials,
            )
        ).json();
        await page.addInitScript(
            (data) =>
                sessionStorage.setItem("toefl.session", JSON.stringify(data)),
            { ...tokens, email: credentials.email },
        );
        await page.goto("/#/speaking");
        await expect(page.locator(".module-grid .module-card")).toHaveCount(2);
        await page.screenshot({
            path: `test-results/speaking-dashboard-${mobile ? "mobile" : "desktop"}.png`,
            fullPage: true,
        });
        for (const index of [0, 1]) {
            if (index) await page.goto("/#/speaking");
            await page
                .locator(".module-grid .module-card")
                .nth(index)
                .getByRole("button")
                .click();
            await expect(
                page.getByRole("button", { name: "Activity 2", exact: true }),
            ).toBeDisabled();
            if (index === 0) {
                await page
                    .getByRole("button", { name: "Listen and record" })
                    .click();
                await expect(
                    page.getByText("Speak now — recording", { exact: false }),
                ).toBeVisible({ timeout: 15000 });
                await expect(
                    page.getByText("Your recording is ready", { exact: false }),
                ).toBeVisible({ timeout: 15000 });
                await expect(
                    page.getByRole("button", { name: "Assess recording" }),
                ).toBeDisabled();
                await expect(page.locator("a[download]")).toHaveCount(0);
                await page.screenshot({
                    path: `test-results/speaking-recording-${mobile ? "mobile" : "desktop"}.png`,
                    fullPage: true,
                });
            }
            for (let n = 1; n <= (index ? 4 : 7); n++) {
                if (n > 1)
                    await page
                        .getByRole("button", {
                            name: "Next activity",
                            exact: true,
                        })
                        .click();
                await page
                    .getByRole("button", {
                        name: "Skip without answering",
                        exact: true,
                    })
                    .click();
                await page
                    .getByRole("dialog")
                    .getByRole("button", {
                        name: "Skip without answering",
                        exact: true,
                    })
                    .click();
                await expect(
                    page.getByRole("heading", {
                        name: "Your speaking feedback",
                    }),
                ).toBeVisible();
            }
            await expect(
                page.getByRole("progressbar", {
                    name: "Speaking run completion",
                }),
            ).toHaveAttribute("aria-valuenow", index ? "10" : "33.33");
            // The original scene is complete, but the expanded edition has more activities.
            // Full restart compatibility is covered by SQL tests pinned to the legacy edition.
            await expect(
                page.getByRole("button", {
                    name: "Next activity",
                    exact: true,
                }),
            ).toBeVisible();
        }
        await page.goto("/#/speaking");
        await expect(
            page.getByRole("heading", { name: "Speaking Studio" }),
        ).toBeVisible();
        await expect(page.getByRole("link", { name: /history/i })).toHaveCount(
            0,
        );
        expect(
            await page.evaluate(
                () => document.documentElement.scrollWidth <= innerWidth,
            ),
        ).toBeTruthy();
    });
}

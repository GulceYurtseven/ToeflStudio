import { test, expect } from "@playwright/test";

for (const mobile of [false, true]) {
    test(`Listening real audio, answers, completion and restart ${mobile ? "mobile" : "desktop"}`, async ({
        page,
        request,
    }) => {
        if (mobile) await page.setViewportSize({ width: 393, height: 851 });
        const credentials = {
        username: "student_" + crypto.randomUUID().replaceAll("-", "").slice(0, 20),
            email: `listening-${crypto.randomUUID()}@example.test`,
            password: "Listen!12345678",
        };
        expect(
            (
                await request.post("/api/auth/register", { data: credentials })
            ).ok(),
        ).toBeTruthy();
        const login = await request.post("/api/auth/login?useCookies=false", {
            data: credentials,
        });
        expect(login.ok()).toBeTruthy();
        const tokens = await login.json();
        await page.addInitScript(
            (data) =>
                sessionStorage.setItem("toefl.session", JSON.stringify(data)),
            { ...tokens, email: credentials.email },
        );
        await page.goto("/#/listening");
        await expect(page.locator(".module-grid .module-card")).toHaveCount(4);
        await page
            .locator(".module-grid .module-card")
            .first()
            .getByText("Task instructions")
            .click();
        await expect(
            page.getByText("The spoken prompt is not displayed on screen", {
                exact: false,
            }),
        ).toBeVisible();
        await page.locator(".format-guide summary").click();
        await expect(page.locator(".format-guide")).toContainText("29 minutes");
        await expect(page.locator(".format-guide")).toContainText(
            "cannot return to a previous question",
        );
        await expect(
            page.locator(".module-grid .module-card").getByRole("button"),
        ).toHaveCount(4);
        expect(
            await page.evaluate(
                () => document.documentElement.scrollWidth <= innerWidth,
            ),
        ).toBeTruthy();
        await page.screenshot({
            path: `test-results/listening-foundation-${mobile ? "mobile" : "desktop"}.png`,
            fullPage: true,
        });
        await expect(page.locator(".module-grid .module-card")).toHaveCount(4);
        await page
            .locator(".module-grid .module-card")
            .first()
            .getByRole("button", { name: "Start", exact: true })
            .click();
        await expect(page.getByRole("radio")).toHaveCount(0);
        await page
            .locator(".bank-audio audio")
            .evaluate((audio: HTMLAudioElement) => audio.play());
        await page
            .getByRole("radio", { name: "Of course. I will keep it open." })
            .check();
        await page.getByRole("button", { name: "Submit and continue" }).click();
        await expect(
            page.getByRole("heading", { name: "1 / 1" }),
        ).toBeVisible();
        await page.getByText("Transcript", { exact: true }).click();
        await expect(
            page.getByText(
                "Could you hold the door while I carry these boxes?",
                { exact: true },
            ),
        ).toBeVisible();
        for (let i = 2; i <= 20; i++) {
            await page.getByRole("button", { name: "Next activity" }).click();
            await page
                .getByRole("button", { name: "Leave unanswered" })
                .click();
            await expect(
                page.getByRole("heading", { name: "0 / 1" }),
            ).toBeVisible();
        }
        await page.getByRole("button", { name: "Next activity" }).click();
        await expect(
            page.getByRole("heading", { name: "Path complete" }),
        ).toBeVisible();
        await page.getByRole("button", { name: "Start a new run" }).click();
        await expect(
            page.getByRole("heading", { name: "Everyday exchange 1" }),
        ).toBeVisible();
        await expect(
            page.getByRole("progressbar", { name: "Session completion" }),
        ).toHaveAttribute("value", "0");
    });
}

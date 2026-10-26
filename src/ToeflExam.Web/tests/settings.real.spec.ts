import { test, expect } from "@playwright/test";

test("Real profile persists after reload and all Studios share an English layout", async ({
    page,
    request,
}) => {
    const credentials = {
        username: "student_" + crypto.randomUUID().replaceAll("-", "").slice(0, 20),
        email: `settings-ui-${crypto.randomUUID()}@example.test`,
        password: "ProfilePractice!9831",
    };
    const registered = await request.post("/api/auth/register", {
        data: credentials,
    });
    expect(registered.ok(), await registered.text()).toBe(true);
    const response = await request.post("/api/auth/login?useCookies=false", {
        data: credentials,
    });
    expect(response.ok()).toBe(true);
    const tokens = await response.json();
    await page.addInitScript(
        (data) => sessionStorage.setItem("toefl.session", JSON.stringify(data)),
        { ...tokens, email: credentials.email },
    );
    await page.goto("/#/settings");
    await page.getByLabel("Display name").fill("Alex Learner");
    await page.getByLabel("Daily practice goal (minutes)").fill("45");
    await page.getByRole("button", { name: "Save profile" }).click();
    await expect(page.getByRole("status")).toContainText("saved");
    await page.reload();
    await expect(page.getByLabel("Display name")).toHaveValue("Alex Learner");
    await expect(page.getByLabel("Daily practice goal (minutes)")).toHaveValue(
        "45",
    );
    await page.screenshot({
        path: "test-results/settings-real-desktop.png",
        fullPage: true,
    });
    await page.setViewportSize({ width: 393, height: 851 });
    await page.screenshot({
        path: "test-results/settings-real-mobile.png",
        fullPage: true,
    });
    expect(
        await page.evaluate(
            () => document.documentElement.scrollWidth <= innerWidth,
        ),
    ).toBe(true);
    for (const section of ["writing", "reading", "listening", "speaking"]) {
        await page.goto("/#/" + section);
        await expect(
            page.getByRole("heading", {
                name: section[0].toUpperCase() + section.slice(1) + " Studio",
                exact: true,
            }),
        ).toBeVisible();
        await expect(page.locator(".studio-overview")).toBeVisible();
        await expect(
            page.locator(".module-grid .module-card").first(),
        ).toBeVisible();
        expect(await page.locator("main").innerText()).not.toMatch(
            /[çğıöşüÇĞİÖŞÜ]/,
        );
        await expect(page.getByRole("link", { name: /history/i })).toHaveCount(
            0,
        );
        await expect(
            page.getByRole("button", { name: /PDF|JSON|download/i }),
        ).toHaveCount(0);
        await expect(page.locator("iframe")).toHaveCount(0);
        expect(
            await page.evaluate(
                () => document.documentElement.scrollWidth <= innerWidth,
            ),
        ).toBe(true);
        await page.screenshot({
            path: `test-results/studio-${section}-mobile.png`,
            fullPage: true,
        });
    }
});

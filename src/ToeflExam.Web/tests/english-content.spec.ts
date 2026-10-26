import { test, expect } from "@playwright/test";
import { mockApi } from "./fixtures";

test("login and registration explain TOEFL preparation in English", async ({
    page,
}, info) => {
    await page.goto("/");
    const auth = page.locator(".auth-layout");
    await expect(auth).toHaveAttribute("lang", "en");
    await expect(auth).toContainText("PREPARE FOR TOEFL iBT");
    expect((await auth.innerText()).replace("Made by Berşan Gülce Yurtseven", "")).not.toMatch(/[çğıöşüÇĞİÖŞÜ]/);
    await page.screenshot({
        path: `test-results/login-en-${info.project.name}.png`,
        fullPage: true,
    });
    await page.route("**/api/auth/login*", (r) =>
        r.fulfill({ status: 401, json: {} }),
    );
    await page.getByLabel("Email or username").fill("learner@example.test");
    await page.getByLabel("Password", { exact: true }).fill("wrong-password");
    await page.getByRole("button", { name: "Sign in to TOEFL Studio" }).click();
    await expect(page.getByRole("alert")).toHaveText(
        "Incorrect email, username or password.",
    );
    await page
        .getByRole("button", { name: "Create account", exact: true })
        .click();
    await expect(page.getByLabel("Confirm password")).toBeVisible();
    expect((await auth.innerText()).replace("Made by Berşan Gülce Yurtseven", "")).not.toMatch(/[çğıöşüÇĞİÖŞÜ]/);
    expect(
        await page.evaluate(
            () => document.documentElement.scrollWidth <= innerWidth,
        ),
    ).toBe(true);
});

test("Listening offers seven study channels without an embedded player", async ({
    page,
}, info) => {
    await mockApi(page);
    await page.goto("/#/listening");
    await expect(
        page.getByRole("heading", { name: "Recommended Study Channels" }),
    ).toBeVisible();
    const links = page.locator(".study-channels a");
    await expect(links).toHaveCount(7);
    for (const handle of [
        "TOEFLofficial",
        "TSTPrep",
        "TOEFLTUTOR",
        "gabbleai",
        "InspireSpeakEasy",
        "linguamarina",
        "LearnEnglishWithTVSeries",
    ])
        await expect(
            page
                .locator(".study-channels")
                .getByRole("link", { name: "@" + handle, exact: false }),
        ).toHaveAttribute("href", "https://www.youtube.com/@" + handle);
    await expect(page.locator("iframe")).toHaveCount(0);
    await expect(page.locator(".sidebar-note")).toHaveCount(0);
    expect(await page.locator("main").innerText()).not.toMatch(
        /[çğıöşüÇĞİÖŞÜ]/,
    );
    await page.screenshot({
        path: "test-results/listening-en-" + info.project.name + ".png",
        fullPage: true,
    });
    expect(
        await page.evaluate(
            () => document.documentElement.scrollWidth <= innerWidth,
        ),
    ).toBe(true);
});

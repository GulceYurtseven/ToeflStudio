import { test, expect } from "@playwright/test";

test("real ASP.NET and SQL: register, login, complete sentence path, restart, write and resume drafts", async ({
    page,
}) => {
    const email = "ui-" + crypto.randomUUID() + "@example.test";
    const password = "UiPractice!9842";
    await page.goto("/");
    await expect(
        page.getByRole("heading", { name: "Continue your preparation." }),
    ).toBeVisible();
    await page
        .getByRole("button", { name: "Create account", exact: true })
        .click();
    await page.getByLabel("Username", { exact: true }).fill("student_" + crypto.randomUUID().slice(0, 16));
    await page.getByLabel("Display name").fill("Journey Learner");
    await page.getByLabel("Email address").fill(email);
    await page.getByLabel("Password", { exact: true }).fill(password);
    await page.getByLabel("Confirm password").fill(password);
    await page.getByRole("button", { name: "Create my account" }).click();
    // First Identity request also warms up EF on a newly migrated database.
    await expect(page.getByRole("status")).toContainText(
        "Your account is ready",
        { timeout: 20000 },
    );
    await page.getByLabel("Password", { exact: true }).fill(password);
    await page.getByRole("button", { name: "Sign in to TOEFL Studio" }).click();
    await expect(page.locator(".studio-card")).toHaveCount(6);
    await page
        .getByRole("link", { name: "Writing Studio", exact: true })
        .click();
    await expect(page.locator(".module-card")).toHaveCount(3);
    await expect(page.locator(".studio-overview")).toContainText(
        "70 activities",
    );
    await page.screenshot({
        path: "test-results/real-dashboard.png",
        fullPage: true,
    });
    await page.locator(".module-card").first().getByRole("button").click();
    for (const word of ["visits", "the", "library"])
        await page
            .locator(".word-tile")
            .filter({ hasText: new RegExp("^" + word) })
            .click();
    await page.getByRole("button", { name: "Check answer" }).click();
    await expect(
        page.getByRole("heading", { name: "You built a correct sentence." }),
    ).toBeVisible();
    for (let i = 2; i <= 30; i++) {
        await page.getByRole("button", { name: "Next activity" }).click();
        await page.getByRole("button", { name: "Check answer" }).click();
        await page
            .getByRole("dialog")
            .getByRole("button", { name: "Submit anyway" })
            .click();
        await expect(
            page.getByRole("heading", {
                name: "Review the correct structure.",
            }),
        ).toBeVisible();
    }
    await expect(
        page.getByRole("heading", { name: "Run complete." }),
    ).toBeVisible();
    await expect(
        page.getByRole("progressbar", { name: "Run completion" }),
    ).toHaveAttribute("aria-valuenow", "100");
    await page.getByRole("button", { name: "Start new run" }).click();
    await page
        .getByRole("dialog")
        .getByRole("button", { name: "Start new run" })
        .click();
    await expect(
        page.getByRole("progressbar", { name: "Run completion" }),
    ).toHaveAttribute("aria-valuenow", "0");
    await expect(page.getByRole("link", { name: /history/i })).toHaveCount(0);
    for (const module of ["email", "academic-discussion"]) {
        await page
            .getByRole("link", { name: "Writing Studio", exact: true })
            .click();
        await page
            .locator(".module-card")
            .nth(module === "email" ? 1 : 2)
            .getByRole("button")
            .click();
        const draft =
            module === "email"
                ? "Dear Ms. Chen, I am writing to reserve a room for our study group."
                : "In my opinion, lecture recordings help students learn at their own pace.";
        await page.getByLabel("Your response in English").fill(draft);
        await page.reload();
        await expect(page.getByLabel("Your response in English")).toHaveValue(
            draft,
        );
        await expect(
            page.getByRole("button", { name: "Assess my response" }),
        ).toBeDisabled();
        await page.screenshot({
            path: "test-results/real-" + module + ".png",
            fullPage: true,
        });
    }
    await page.getByLabel("Account menu").click();
    await page.getByRole("button", { name: "Sign out" }).click();
    await expect(
        page.getByRole("heading", { name: "Continue your preparation." }),
    ).toBeVisible();
});

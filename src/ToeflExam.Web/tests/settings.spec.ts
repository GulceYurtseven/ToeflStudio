import { test, expect } from "@playwright/test";
import { mockApi } from "./fixtures";

test("Settings saves profile goals, shows free membership and validates credential changes", async ({
    page,
}, info) => {
    await mockApi(page);
    let saved: any;
    await page.route("**/api/settings**", (route) => {
        if (route.request().url().endsWith("/credentials"))
            return route.fulfill({
                status: 400,
                json: { detail: "The current password is incorrect." },
            });
        if (route.request().method() === "POST") {
            saved = route.request().postDataJSON();
            return route.fulfill({ status: 204 });
        }
        return route.fulfill({
            json: {
                email: "learner@example.test",
                displayName: "Learner",
                dailyPracticeMinutes: 20,
                targetTestDate: null,
                membership: "Free",
                membershipDescription:
                    "Access to all four Studios and three Practice Exams.",
            },
        });
    });
    await page.goto("/#/settings");
    await expect(
        page.getByRole("heading", { name: "Settings", exact: true }),
    ).toBeVisible();
    await expect(
        page.getByText("Free membership", { exact: true }),
    ).toBeVisible();
    await page.getByLabel("Display name").fill("Alex");
    await page.getByLabel("Daily practice goal (minutes)").fill("35");
    await page.getByLabel("Target test date (optional)").fill("2027-01-20");
    await page.getByRole("button", { name: "Save profile" }).click();
    await expect(page.getByRole("status")).toContainText("saved");
    expect(saved.displayName).toBe("Alex");
    expect(saved.dailyPracticeMinutes).toBe(35);
    expect(saved.targetTestDate).toBe("2027-01-20");
    await page
        .getByLabel("Current password", { exact: true })
        .fill("Incorrect!123");
    await page
        .getByLabel("New password (optional)", { exact: true })
        .fill("Updated!Test123");
    await page
        .getByLabel("Confirm new password", { exact: true })
        .fill("Different!Test123");
    await page.getByRole("button", { name: "Update sign-in details" }).click();
    await expect(page.getByRole("alert")).toContainText("do not match");
    await page
        .getByLabel("Confirm new password", { exact: true })
        .fill("Updated!Test123");
    await page.getByRole("button", { name: "Update sign-in details" }).click();
    await expect(page.getByRole("alert")).toContainText(
        "current password is incorrect",
    );
    expect(await page.locator("main").innerText()).not.toMatch(
        /[çğıöşüÇĞİÖŞÜ]/,
    );
    expect(
        await page.evaluate(
            () => document.documentElement.scrollWidth <= innerWidth,
        ),
    ).toBe(true);
    await page.screenshot({
        path: `test-results/settings-${info.project.name}.png`,
        fullPage: true,
    });
});

test("Settings signs out after a successful credential update", async ({
    page,
}) => {
    await mockApi(page);
    await page.route("**/api/settings**", (route) =>
        route.fulfill({
            json:
                route.request().method() === "POST"
                    ? { requiresSignIn: true }
                    : {
                          email: "learner@example.test",
                          displayName: "Alex",
                          dailyPracticeMinutes: 20,
                          targetTestDate: null,
                          membership: "Free",
                          membershipDescription: "All Studios included.",
                      },
        }),
    );
    await page.goto("/#/settings");
    await page
        .getByLabel("Current password", { exact: true })
        .fill("Current!123");
    await page
        .getByLabel("New email address (optional)")
        .fill("changed@example.test");
    await page.getByRole("button", { name: "Update sign-in details" }).click();
    await expect(
        page.getByRole("heading", { name: "Continue your preparation." }),
    ).toBeVisible();
    expect(
        await page.evaluate(() => sessionStorage.getItem("toefl.session")),
    ).toBeNull();
});

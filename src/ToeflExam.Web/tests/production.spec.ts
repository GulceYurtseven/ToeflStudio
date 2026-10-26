import { test, expect } from "@playwright/test";
import { mockApi } from "./fixtures";

test("completed exam actions align and the saved report stays accessible during a new attempt", async ({ page }, info) => {
    await mockApi(page);
    const savedId = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
    let inProgress = false;
    await page.route("**/api/sessions/catalog", route => route.fulfill({ json: {
        listening: [], limitations: [],
        exams: [1, 2, 3].map(n => ({ id: `mock-${n}`, title: `TOEFL Practice Exam ${n}`, questionsPerPath: 93, available: true })),
    } }));
    await page.route("**/api/sessions/exam/*/history", route => route.fulfill({ json:
        route.request().url().includes("mock-1") ? [
            ...(inProgress ? [{ id: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb", completed: false, completionPercent: 20 }] : []),
            { id: savedId, completed: true, completionPercent: 100, hasSavedReport: true },
        ] : [],
    }));
    await page.goto("/#/exams");
    const card = page.locator(".bank-cards .module-card").first();
    const report = card.getByRole("button", { name: "View latest report" });
    await expect(report).toBeEnabled();
    await page.getByRole("checkbox").check();
    const restart = card.getByRole("button", { name: "Start again" });
    const first = (await report.boundingBox())!;
    const second = (await restart.boundingBox())!;
    expect(Math.abs(first.height - second.height)).toBeLessThan(1);
    if (info.project.name === "desktop") expect(Math.abs(first.y - second.y)).toBeLessThan(1);
    expect(await report.evaluate(el => getComputedStyle(el).marginTop)).toBe("0px");
    await page.screenshot({ path: `test-results/exam-catalog-${info.project.name}.png`, fullPage: true, animations: "disabled" });
    inProgress = true;
    await page.reload();
    await expect(card.getByRole("link", { name: "View latest completed report" })).toHaveAttribute("href", `#/session/${savedId}`);
    await expect(card.getByRole("button", { name: "Continue" })).toBeDisabled();
});

test("all Studios share a notebook format heading, purple Studio and study channels", async ({
    page,
}, info) => {
    await mockApi(page);
    for (const section of ["writing", "reading", "listening", "speaking"]) {
        await page.goto("/#/" + section);
        await expect(page.locator("h1 em")).toHaveText("Studio");
        const format = page.locator(".format-guide summary");
        await expect(format).toContainText("2026 Test Format");
        await expect(format.locator(".notebook-mini")).toBeVisible();
        await expect(page.locator(".study-channels a")).toHaveCount(7);
        await expect(
            page.locator("nav").getByText("Settings", { exact: true }),
        ).toHaveCount(0);
        await expect(
            page.getByText(
                "50 original questions across 32 ordered activities.",
                { exact: false },
            ),
        ).toHaveCount(0);
        await format.click();
        expect(
            await page.evaluate(
                () => document.documentElement.scrollWidth <= innerWidth,
            ),
        ).toBe(true);
        if (section === "writing")
            await page.screenshot({
                path: `test-results/production-studio-${info.project.name}.png`,
                fullPage: true,
            });
    }
    await page.goto("/#/");
    await expect(page.locator(".hero-notebook .auth-paper")).toHaveCount(1);
    await expect(page.locator(".study-channels a")).toHaveCount(7);
});

test("profile dropdown uses the display name and submits feedback with CSRF, preserving failed drafts", async ({
    page,
}, info) => {
    await mockApi(page);
    await page.route("**/api/auth/manage/info", (route) =>
        route.fulfill({
            json: {
                email: "learner@example.test",
                username: "alex",
                displayName: "Alex Learner",
            },
        }),
    );
    let delivered: any;
    let attempts = 0;
    await page.route("**/api/contact", (route) => {
        // An unavailable metadata check must not block the form; delivery is decided by POST.
        if (route.request().method() === "GET")
            return route.fulfill({ json: { configured: false } });
        expect(route.request().headers()["x-csrf-token"]).toBe("csrf-test");
        delivered = route.request().postDataJSON();
        attempts++;
        return attempts === 1
            ? route.fulfill({
                  status: 503,
                  json: {
                      detail: "Your message could not be sent. Please try later.",
                  },
              })
            : route.fulfill({ json: { message: "Sent" } });
    });
    await page.goto("/#/");
    await expect(page.locator(".account-email")).toHaveText("Alex Learner");
    await page.getByLabel("Account menu").click();
    await expect(page.locator(".profile-dropdown a").first()).toHaveText(
        "Settings",
    );
    await page.getByRole("button", { name: "Feedback & Suggestions" }).click();
    await expect(page.getByRole("dialog")).toBeVisible();
    await expect(page.getByText("The contact form is not available yet.", { exact: false })).toHaveCount(0);
    await expect(page.getByLabel("Reply email")).toHaveValue(
        "learner@example.test",
    );
    await page
        .getByLabel("Subject", { exact: true })
        .fill("More practice please");
    await page
        .getByLabel("Message", { exact: true })
        .fill("Please add more academic reading activities.");
    await page.getByRole("button", { name: "Send message" }).click();
    await expect(page.getByRole("alert")).toContainText("could not be sent");
    await expect(page.getByLabel("Message", { exact: true })).toHaveValue(
        "Please add more academic reading activities.",
    );
    await page.screenshot({
        path: `test-results/production-contact-${info.project.name}.png`,
        fullPage: true,
    });
    await page.getByRole("button", { name: "Send message" }).click();
    await expect(page.getByRole("dialog").getByRole("status")).toContainText(
        "Your message has been sent",
    );
    expect(delivered.subject).toBe("More practice please");
    await page.getByRole("button", { name: "Done", exact: true }).click();
    await expect(page.getByLabel("Account menu")).toBeFocused();
    await page.getByLabel("Account menu").click();
    await page.keyboard.press("Escape");
    await expect(page.locator(".profile-menu")).not.toHaveAttribute("open", "");
    await expect(
        page.getByRole("link", { name: "Made by Berşan Gülce Yurtseven" }),
    ).toHaveAttribute("href", /linkedin.com/);
    await page.getByRole("button", { name: "Contact", exact: true }).click();
    await expect(page.getByRole("dialog").getByRole("heading")).toHaveText(
        "Contact",
    );
});

test("login accepts usernames, renders unsafe display names as text and keeps Contact public", async ({
    page,
}) => {
    await page.route("**/api/auth/providers", (route) =>
        route.fulfill({ json: { google: true } }),
    );
    await page.route("**/api/auth/login*", (route) => {
        expect(route.request().postDataJSON().email).toBe("alex_learner");
        return route.fulfill({
            json: { accessToken: "test-token", refreshToken: "test-refresh" },
        });
    });
    const name = '<img src=x onerror="window.injected=true">';
    await page.route("**/api/auth/manage/info", (route) =>
        route.fulfill({
            json: {
                email: "alex@example.test",
                username: "alex_learner",
                displayName: name,
            },
        }),
    );
    await page.goto("/");
    await expect(
        page.getByRole("button", { name: "Contact", exact: true }),
    ).toBeVisible();
    await expect(
        page.getByRole("button", { name: "Sign in with Google" }),
    ).toHaveCount(0);
    await page.getByLabel("Email or username").fill("alex_learner");
    await page.getByLabel("Password", { exact: true }).fill("TestPassword!123");
    await page.getByRole("button", { name: "Sign in to TOEFL Studio" }).click();
    await expect(page.locator(".account-email")).toHaveText(name);
    await expect(page.locator(".profile-menu img")).toHaveCount(0);
    expect(await page.evaluate(() => (window as any).injected)).toBeUndefined();
    expect(
        await page.evaluate(
            () => JSON.parse(sessionStorage.getItem("toefl.session")!).email,
        ),
    ).toBe("alex@example.test");
});

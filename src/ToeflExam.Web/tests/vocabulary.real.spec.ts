import { test, expect } from "@playwright/test";

/** Exercises real registration, authorized catalog loading and a flashcard through the built SPA. */
test("Vocabulary Studio serves the complete bank through the real authenticated API", async ({
    page,
    request,
}) => {
    const anonymous = await request.get("/api/vocabulary");
    expect(anonymous.status()).toBe(401);
    const credentials = {
        username:
            "vocab_" + crypto.randomUUID().replaceAll("-", "").slice(0, 20),
        email: `vocab-ui-${crypto.randomUUID()}@example.test`,
        password: "VocabularyPractice!9831",
    };
    const registration = await request.post("/api/auth/register", {
        data: credentials,
    });
    expect(registration.ok(), await registration.text()).toBe(true);
    const login = await request.post("/api/auth/login?useCookies=false", {
        data: credentials,
    });
    expect(login.ok()).toBe(true);
    const tokens = await login.json();
    await page.addInitScript(
        (data) => sessionStorage.setItem("toefl.session", JSON.stringify(data)),
        { ...tokens, email: credentials.email },
    );
    await page.goto("/");
    await expect(page.locator(".studio-card")).toHaveCount(6);
    await page.locator(".studio-vocabulary").click();
    await expect(
        page.getByRole("heading", { name: "Vocabulary Studio", exact: true }),
    ).toBeVisible();
    await expect(page.locator(".vocabulary-word-card")).toHaveCount(378);
    await page.getByRole("button", { name: "Get Random Word" }).click();
    await expect(page.locator("#vocabulary-answer dl")).toHaveCount(0);
    await page.getByRole("button", { name: "Show Answer" }).click();
    await expect(
        page.locator("#vocabulary-answer .vocabulary-example"),
    ).toBeVisible();
    await page.reload();
    await expect(page.locator(".vocabulary-word-card")).toHaveCount(378);
    await expect(page.getByRole("button", { name: "Show Answer" })).toHaveCount(
        0,
    );
});

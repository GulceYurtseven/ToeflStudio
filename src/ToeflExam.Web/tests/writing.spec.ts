import { test, expect } from "@playwright/test";
import { mockApi, runId } from "./fixtures";

test("dashboard has ordered modules and fits the viewport", async ({
    page,
}, info) => {
    await mockApi(page);
    await page.goto("/#/writing");
    await expect(
        page.getByRole("heading", { name: "Writing Studio" }),
    ).toBeVisible();
    await expect(page.locator(".module-card")).toHaveCount(3);
    await expect(page.locator(".module-card").first()).toContainText(
        "Build a Sentence",
    );
    expect(
        await page.evaluate(
            () => document.documentElement.scrollWidth <= innerWidth,
        ),
    ).toBeTruthy();
    await page.screenshot({
        path: "test-results/dashboard-" + info.project.name + ".png",
        fullPage: true,
    });
});

test("sentence tiles, completion, next step, restart and history", async ({
    page,
}) => {
    await mockApi(page);
    await page.goto("/#/practice/build-sentence/" + runId + "/1");
    await expect(
        page.getByRole("button", { name: /Activity 02/ }),
    ).toBeDisabled();
    for (const word of ["visits", "the", "library"]) {
        const tile = page
            .locator(".word-tile")
            .filter({ hasText: new RegExp("^" + word) });
        await tile.focus();
        await tile.press("Space");
    }
    await expect(page.locator(".sentence-slot.filled")).toHaveCount(3);
    await page.reload();
    await expect(page.locator(".sentence-slot.filled")).toHaveCount(3);
    await page.getByRole("button", { name: "Check answer" }).click();
    await expect(
        page.getByRole("heading", { name: "You built a correct sentence." }),
    ).toBeVisible();
    await expect(
        page.getByRole("progressbar", { name: "Run completion" }),
    ).toHaveAttribute("aria-valuenow", "50");
    await page.getByRole("button", { name: "Next activity" }).click();
    for (const word of ["visits", "the", "library"])
        await page
            .locator(".word-tile")
            .filter({ hasText: new RegExp("^" + word) })
            .click();
    await page.getByRole("button", { name: "Check answer" }).click();
    await expect(
        page.getByRole("heading", { name: "Run complete." }),
    ).toBeVisible();
    await page.getByRole("button", { name: "Start new run" }).click();
    await expect(page.getByRole("dialog")).toBeVisible();
    await page.keyboard.press("Escape");
    await expect(page.getByRole("dialog")).toHaveCount(0);
    await page.getByRole("button", { name: "Start new run" }).click();
    await page
        .getByRole("dialog")
        .getByRole("button", { name: "Start new run" })
        .click();
    await expect(
        page.getByRole("progressbar", { name: "Run completion" }),
    ).toHaveAttribute("aria-valuenow", "0");
    await expect(page.getByRole("link", { name: /history/i })).toHaveCount(0);
});

for (const module of ["email", "academic-discussion"]) {
    test(
        module +
            " draft survives reload and provider failure, feedback downloads safely",
        async ({ page }, info) => {
            const state = await mockApi(page, { failOnce: true });
            await page.goto("/#/practice/" + module + "/" + runId + "/1");
            if (module === "academic-discussion")
                await expect(page.locator(".discussion-post")).toHaveCount(2);
            const answer =
                "Dear Ms. Chen, I would like a study room. <script>window.bad=true</script>";
            await page.getByLabel("Your response in English").fill(answer);
            await page.reload();
            await expect(
                page.getByLabel("Your response in English"),
            ).toHaveValue(answer);
            await page
                .getByRole("button", { name: "Assess my response" })
                .click();
            await expect(page.getByRole("alert")).toContainText(
                "Gemini zaman aşımına uğradı",
            );
            await expect(
                page.getByLabel("Your response in English"),
            ).toHaveValue(answer);
            await page
                .getByRole("button", { name: "Assess my response" })
                .click();
            await expect(
                page.getByRole("heading", { name: "Your feedback is ready." }),
            ).toBeVisible();
            expect(state.attempts).toBe(2);
            expect(state.submissions[0].submissionId).toBe(
                state.submissions[1].submissionId,
            );
            await page
                .getByText("Your submitted response", { exact: false })
                .click();
            await expect(
                page.locator(".response-details").first(),
            ).toContainText(answer);
            expect(
                await page.evaluate(() => (window as any).bad),
            ).toBeUndefined();
            await expect(
                page.getByRole("button", { name: /JSON|PDF|Download|Print/i }),
            ).toHaveCount(0);
            await page.evaluate(() => scrollTo(0, 0));
            await page.screenshot({
                path:
                    "test-results/" +
                    module +
                    "-feedback-" +
                    info.project.name +
                    ".png",
                fullPage: true,
            });
        },
    );
}

test("disabled evaluator permits drafting and never submits", async ({
    page,
}) => {
    const state = await mockApi(page, { configured: false });
    await page.goto("/#/practice/email/" + runId + "/1");
    await page
        .getByLabel("Your response in English")
        .fill("My practice draft.");
    await expect(
        page.getByRole("button", { name: "Assess my response" }),
    ).toBeDisabled();
    expect(state.attempts).toBe(0);
});

test("lost response retries with the same submission id and original payload", async ({
    page,
}) => {
    const state = await mockApi(page);
    let first = true;
    await page.route("**/steps/1/submissions", async (route) => {
        if (first) {
            first = false;
            await route.abort("failed");
        } else await route.fallback();
    });
    await page.goto("/#/practice/email/" + runId + "/1");
    await page
        .getByLabel("Your response in English")
        .fill("A draft to preserve after a network error.");
    await page.getByRole("button", { name: "Assess my response" }).click();
    await expect(page.getByRole("alert")).toContainText(
        "Unable to reach the server",
    );
    await expect(page.getByLabel("Your response in English")).toHaveAttribute(
        "readonly",
        "",
    );
    await page.reload();
    await page.getByRole("button", { name: "Retry same response" }).click();
    await expect(
        page.getByRole("heading", { name: "Your feedback is ready." }),
    ).toBeVisible();
    expect(state.attempts).toBe(1);
});

test("expired session opens login without destroying the draft", async ({
    page,
}) => {
    await mockApi(page, { unauthorized: true });
    await page.addInitScript(() =>
        sessionStorage.setItem("toefl.draft.saved", "keep-me"),
    );
    await page.goto("/#/writing");
    await expect(
        page.getByRole("heading", { name: "Continue your preparation." }),
    ).toBeVisible();
    expect(
        await page.evaluate(() => sessionStorage.getItem("toefl.draft.saved")),
    ).toBe("keep-me");
    await page.getByLabel("Email or username").fill("learner@example.test");
    await page.getByLabel("Password", { exact: true }).fill("Practice!123");
    await page.getByRole("button", { name: "Sign in to TOEFL Studio" }).click();
    await expect(page.locator(".studio-card")).toHaveCount(6);
});

test("empty answer requires confirmation and logout removes drafts", async ({
    page,
}) => {
    await mockApi(page);
    await page.goto("/#/practice/email/" + runId + "/1");
    await page.getByRole("button", { name: "Assess my response" }).click();
    await expect(page.getByRole("dialog")).toContainText(
        "An empty response receives 0 points",
    );
    await page.getByRole("button", { name: "Cancel" }).click();
    await page.getByLabel("Your response in English").fill("Private draft");
    await page.getByLabel("Account menu").click();
    await page.getByRole("button", { name: "Sign out" }).click();
    await expect(
        page.getByRole("heading", { name: "Continue your preparation." }),
    ).toBeVisible();
    expect(
        await page.evaluate(
            () =>
                Object.keys(sessionStorage).filter((k) =>
                    k.startsWith("toefl.draft."),
                ).length,
        ),
    ).toBe(0);
});

import { test, expect, type Page } from "@playwright/test";
import { readFileSync } from "node:fs";
import { mockApi } from "./fixtures";

const catalog = JSON.parse(
    readFileSync(
        new URL(
            "../../ToeflExam.Infrastructure/Vocabulary/catalog.json",
            import.meta.url,
        ),
        "utf8",
    ),
);

/** Serves the real curated bank through the mock API so rendering tests cover every entry. */
async function openVocabulary(page: Page) {
    await mockApi(page);
    await page.route("**/api/vocabulary", (route) =>
        route.fulfill({ json: catalog }),
    );
    await page.goto("/#/vocabulary");
    await expect(page.locator(".vocabulary-word-card")).toHaveCount(378);
}

test("full searchable bank and same-page answer reveal work without immediate repeats", async ({
    page,
}, info) => {
    await openVocabulary(page);
    const flashcard = page.locator(".vocabulary-flashcard");
    await expect(
        flashcard.getByRole("button", { name: "Show Answer" }),
    ).toHaveCount(0);
    await expect(page.locator(".vocabulary-word-card").first()).toContainText(
        "Meaning",
    );
    await expect(page.locator(".vocabulary-word-card").first()).toContainText(
        "Example Sentence",
    );
    const url = page.url();
    // Control the draw so this test verifies answer resetting instead of relying on chance.
    await page.evaluate(() => {
        Math.random = () => 0;
    });
    await flashcard.getByRole("button", { name: "Get Random Word" }).click();
    await expect(flashcard.locator("h3")).toHaveText(catalog.words[0].word);
    await expect(flashcard.locator("dl")).toHaveCount(0);
    await flashcard.getByRole("button", { name: "Show Answer" }).click();
    await expect(
        flashcard.getByRole("region", { name: "Flashcard answer" }),
    ).toContainText(catalog.words[0].meaning);
    await expect(flashcard.locator(".vocabulary-example")).toHaveText(
        catalog.words[0].exampleSentence,
    );
    await flashcard.getByRole("button", { name: "Get Random Word" }).click();
    await expect(flashcard.locator("h3")).toHaveText(catalog.words[1].word);
    await expect(flashcard.locator("dl")).toHaveCount(0);
    expect(page.url()).toBe(url);
    await expect(page.locator(".vocabulary-word-card")).toHaveCount(378);
    await flashcard.getByRole("button", { name: "Show Answer" }).click();
    expect(
        await page.evaluate(
            () => document.documentElement.scrollWidth <= innerWidth,
        ),
    ).toBe(true);
    await page.screenshot({
        path: `test-results/vocabulary-${info.project.name}.png`,
        animations: "disabled",
    });
    await page
        .getByLabel("Search words, meanings, or examples")
        .fill("  CRITERION  ");
    await expect(page.locator(".vocabulary-word-card")).toHaveCount(1);
    await expect(page.locator(".vocabulary-word-card h3")).toHaveText(
        "criteria",
    );
    await page.screenshot({
        path: `test-results/vocabulary-search-${info.project.name}.png`,
        animations: "disabled",
        fullPage: true,
    });
    await page
        .getByLabel("Search words, meanings, or examples")
        .fill("nothing-matches-xyz");
    await expect(
        page.getByRole("heading", { name: "No matching words" }),
    ).toBeVisible();
    // An empty search result must not shrink or disable the flashcard pool.
    await flashcard.getByRole("button", { name: "Get Random Word" }).click();
    await expect(flashcard.locator("h3")).toHaveText(catalog.words[0].word);
    await page.getByRole("button", { name: "Clear search" }).click();
    await expect(page.locator(".vocabulary-word-card")).toHaveCount(378);
    await expect(
        page.getByRole("button", { name: /PDF|JSON|download/i }),
    ).toHaveCount(0);
});

test("failed catalog load can be retried and content stays text with reduced motion", async ({
    page,
}) => {
    await mockApi(page);
    await page.emulateMedia({ reducedMotion: "reduce" });
    let available = false;
    const safeCatalog = {
        ...catalog,
        words: [
            {
                ...catalog.words[0],
                meaning: '<img src=x onerror="alert(1)"> Text stays text.',
            },
        ],
    };
    await page.route("**/api/vocabulary", (route) => {
        if (!available) {
            return route.fulfill({
                status: 503,
                json: { detail: "The word bank is temporarily unavailable." },
            });
        }
        return route.fulfill({ json: safeCatalog });
    });
    await page.goto("/#/vocabulary");
    await expect(
        page.getByRole("button", { name: "Get Random Word" }),
    ).toBeDisabled();
    await expect(page.getByRole("alert")).toBeVisible();
    available = true;
    await page.getByRole("button", { name: /try again/i }).click();
    await expect(page.locator(".vocabulary-word-card")).toHaveCount(1);
    await page.getByRole("button", { name: "Get Random Word" }).click();
    await page.getByRole("button", { name: "Show Answer" }).click();
    await expect(page.locator("#vocabulary-answer")).toContainText(
        safeCatalog.words[0].meaning,
    );
    await expect(page.locator(".vocabulary-studio img")).toHaveCount(0);
    expect(
        await page
            .locator(".vocabulary-answer-content")
            .evaluate((el) => getComputedStyle(el).animationName),
    ).toBe("none");
    await page.getByRole("button", { name: "Get Random Word" }).click();
    await expect(page.locator("#vocabulary-answer dl")).toHaveCount(0);
});

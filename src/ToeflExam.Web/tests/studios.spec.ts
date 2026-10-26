import { test, expect } from "@playwright/test";
import { mockApi } from "./fixtures";

test("home offers five studios and practice exams without removed copy", async ({
    page,
}, info) => {
    await mockApi(page);
    await page.goto("/");
    await expect(page.locator(".studio-card")).toHaveCount(6);
    for (const title of [
        "Reading Studio",
        "Listening Studio",
        "Writing Studio",
        "Speaking Studio",
        "Vocabulary Studio",
    ])
        await expect(
            page.getByRole("heading", { name: title, exact: true }),
        ).toBeVisible();
    expect(
        await page.evaluate(
            () => document.documentElement.scrollWidth <= innerWidth,
        ),
    ).toBeTruthy();
    await page.screenshot({
        path: "test-results/studios-" + info.project.name + ".png",
        fullPage: true,
    });
    await page
        .getByRole("link", { name: "Listening Studio", exact: true })
        .click();
    await expect(
        page.getByRole("heading", { name: "Listening Studio", exact: true }),
    ).toBeVisible();
    await page
        .getByRole("link", { name: "Writing Studio", exact: true })
        .click();
    for (const text of [
        "Orderyla, sağlam adımlarla",
        "Bitirmek yeni bir başlangıç",
        "BİR SONRAKİ CÜMLEN BURADA BAŞLIYOR",
        "KÜÇÜK ADIMLAR, GÖRÜNÜR İLERLEME",
    ])
        await expect(page.getByText(text, { exact: false })).toHaveCount(0);
});

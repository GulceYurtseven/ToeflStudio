import { test, expect } from "@playwright/test";

for (const mobile of [false, true])
    for (const module of ["complete-words", "daily-life", "academic-passage"]) {
        test(`real Reading ${module} ${mobile ? "mobile" : "desktop"}: answers, review, progress, restart, history`, async ({
            page,
            request,
        }) => {
            if (mobile) await page.setViewportSize({ width: 393, height: 851 });
            const credentials = {
        username: "student_" + crypto.randomUUID().replaceAll("-", "").slice(0, 20),
                email: "reader-ui-" + crypto.randomUUID() + "@example.test",
                password: "Reader!9842-Test",
            };
            expect(
                (
                    await request.post("/api/auth/register", {
                        data: credentials,
                    })
                ).ok(),
            ).toBeTruthy();
            const login = await request.post(
                "/api/auth/login?useCookies=false",
                { data: credentials },
            );
            expect(login.ok()).toBeTruthy();
            const tokens = await login.json();
            await page.addInitScript(
                ({ tokens, email }) => {
                    if (!sessionStorage.getItem("test.auth")) {
                        sessionStorage.setItem(
                            "toefl.session",
                            JSON.stringify({ ...tokens, email }),
                        );
                        sessionStorage.setItem("test.auth", "yes");
                    }
                },
                { tokens, email: credentials.email },
            );
            await page.goto("/#/reading");
            await expect(page.locator(".module-card")).toHaveCount(3);
            await page.screenshot({
                path: `test-results/reading-dashboard-${mobile ? "mobile" : "desktop"}.png`,
                fullPage: true,
            });
            const index = [
                "complete-words",
                "daily-life",
                "academic-passage",
            ].indexOf(module);
            await page
                .locator(".module-card")
                .nth(index)
                .getByRole("button")
                .click();
            await expect(
                page.getByRole("button", {
                    name: "Activity 2, locked",
                    exact: true,
                }),
            ).toBeDisabled();
            if (module === "complete-words") {
                const guides = page.locator(".missing-letters-guide");
                await expect(guides).toHaveCount(10);
                await expect(guides.first()).toHaveText("___");
                await page.getByRole("textbox", { name: /^Gap 1,/ }).fill("v");
                await expect(guides.first()).toHaveText("__");
                // Fill the exact missing half shown by the authored content.
                const words = [
                    "leaves",
                    "shade",
                    "summer",
                    "water",
                    "from",
                    "openings",
                    "cools",
                    "surrounding",
                    "throughout",
                    "day",
                ];
                for (let i = 0; i < words.length; i++)
                    await page
                        .getByRole("textbox", {
                            name: new RegExp("^Gap " + (i + 1) + ","),
                        })
                        .fill(words[i].slice(Math.floor(words[i].length / 2)));
            } else {
                const choices =
                    module === "daily-life" ? [0, 2] : [1, 2, 3, 0, 2];
                for (let i = 0; i < choices.length; i++)
                    await page
                        .locator(".reading-question")
                        .nth(i)
                        .getByRole("radio")
                        .nth(choices[i])
                        .check();
            }
            await page.reload();
            if (module === "complete-words")
                await expect(
                    page.getByRole("textbox", { name: /^Gap 1,/ }),
                ).toHaveValue("ves");
            else
                await expect(
                    page
                        .locator(".reading-question")
                        .first()
                        .getByRole("radio")
                        .nth(module === "daily-life" ? 0 : 1),
                ).toBeChecked();
            expect(
                await page.evaluate(
                    () => document.documentElement.scrollWidth <= innerWidth,
                ),
            ).toBeTruthy();
            await page.screenshot({
                path: `test-results/reading-${module}-${mobile ? "mobile" : "desktop"}.png`,
                fullPage: true,
            });
            const droppedBodies: string[] = [];
            const loseResponse = module === "daily-life" && !mobile;
            if (loseResponse)
                await page.route(
                    "**/api/reading/daily-life/runs/*/steps/1/submissions",
                    async (route) => {
                        droppedBodies.push(route.request().postData()!);
                        const response = await route.fetch();
                        if (droppedBodies.length === 1)
                            await route.abort("failed");
                        else await route.fulfill({ response });
                    },
                );
            await page.getByRole("button", { name: "Check answers" }).click();
            if (loseResponse) {
                await expect(
                    page.getByText("Unable to reach the server.", {
                        exact: false,
                    }),
                ).toBeVisible();
                await expect(
                    page
                        .locator(".reading-question")
                        .first()
                        .getByRole("radio")
                        .first(),
                ).toBeDisabled();
                await page
                    .getByRole("button", {
                        name: "Retry same responses",
                    })
                    .click();
                await expect(
                    page.getByRole("heading", { name: "Activity complete" }),
                ).toBeVisible();
                expect(droppedBodies).toHaveLength(2);
                expect(droppedBodies[1]).toBe(droppedBodies[0]);
            }
            await expect(
                page.getByRole("heading", { name: "Activity complete" }),
            ).toBeVisible();
            await expect(page.locator(".reading-result.incorrect")).toHaveCount(
                0,
            );
            await expect(
                page.getByRole("button", { name: /download|PDF|JSON|print/i }),
            ).toHaveCount(0);
            await page.reload();
            await expect(
                page.getByRole("heading", { name: "Activity complete" }),
            ).toBeVisible();
            await expect(page.locator(".reading-result.incorrect")).toHaveCount(
                0,
            );
            await page.screenshot({
                path: `test-results/reading-feedback-${module}-${mobile ? "mobile" : "desktop"}.png`,
                fullPage: true,
            });
            for (
                let n = 2;
                n <=
                (module === "complete-words"
                    ? 7
                    : module === "daily-life"
                      ? 10
                      : 9);
                n++
            ) {
                await page
                    .getByRole("button", {
                        name: "Next activity",
                        exact: true,
                    })
                    .click();
                if (module === "academic-passage" && n === 3) {
                    await expect(
                        page.locator(".reading-passage"),
                    ).toContainText("[B]");
                    const insertion = page.locator(".reading-question").nth(4);
                    await expect(insertion).toContainText(
                        "Neither kind of evidence",
                    );
                    await insertion.getByRole("radio").nth(1).check();
                    await page.screenshot({
                        path: `test-results/reading-insertion-${mobile ? "mobile" : "desktop"}.png`,
                        fullPage: true,
                    });
                }
                await page
                    .getByRole("button", { name: "Check answers" })
                    .click();
                await expect(page.getByRole("dialog")).toBeVisible();
                await page
                    .getByRole("dialog")
                    .getByRole("button", { name: "Submit anyway" })
                    .click();
                await expect(
                    page.getByRole("heading", { name: "Activity complete" }),
                ).toBeVisible();
            }
            await expect(
                page.getByRole("progressbar", {
                    name: "Reading run completion",
                }),
            ).toHaveAttribute("aria-valuenow", "100");
            await page.getByRole("button", { name: "Start new run" }).click();
            await page
                .getByRole("dialog")
                .getByRole("button", { name: "Start new run" })
                .click();
            await expect(
                page.getByRole("progressbar", {
                    name: "Reading run completion",
                }),
            ).toHaveAttribute("aria-valuenow", "0");
            await page.goto("/#/reading");
            await expect(
                page.getByRole("heading", { name: "Reading Studio" }),
            ).toBeVisible();
            await expect(
                page.getByRole("link", { name: /history/i }),
            ).toHaveCount(0);
            await page.getByLabel("Account menu").click();
    await page.getByRole("button", { name: "Sign out" }).click();
            expect(
                await page.evaluate(
                    () =>
                        Object.keys(sessionStorage).filter((k) =>
                            k.startsWith("toefl.draft."),
                        ).length,
                ),
            ).toBe(0);
        });
    }

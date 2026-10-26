import { test, expect } from "@playwright/test";
test.use({
    launchOptions: {
        args: [
            "--use-fake-device-for-media-stream",
            "--use-fake-ui-for-media-stream",
        ],
    },
    permissions: ["microphone"],
});
for (const mobile of [false, true])
    test(`Real exam catalog, isolated questions, module review, drafts and durable deadline ${mobile ? "mobile" : "desktop"}`, async ({
        page,
        request,
    }) => {
        if (mobile) await page.setViewportSize({ width: 393, height: 851 });
        const credentials = {
        username: "student_" + crypto.randomUUID().replaceAll("-", "").slice(0, 20),
            email: `exam-ui-${crypto.randomUUID()}@example.test`,
            password: "ExamPractice!57891",
        };
        async function authPost(path: string) {
            let response;
            await expect
                .poll(
                    async () => {
                        response = await request.post(path, {
                            data: credentials,
                        });
                        if (response.status() === 429) return false;
                        expect(
                            response.ok(),
                            await response.text(),
                        ).toBeTruthy();
                        return true;
                    },
                    { timeout: 65000, intervals: [5000] },
                )
                .toBe(true);
            return response!;
        }
        await authPost("/api/auth/register");
        const tokens = await (
            await authPost("/api/auth/login?useCookies=false")
        ).json();
        await page.addInitScript(
            (data) =>
                sessionStorage.setItem("toefl.session", JSON.stringify(data)),
            { ...tokens, email: credentials.email },
        );
        await page.goto("/#/exams");
        await expect(page.locator(".bank-cards .module-card")).toHaveCount(3);
        await expect(
            page
                .locator(".bank-cards .module-card")
                .first()
                .getByRole("button", { name: "Start", exact: true }),
        ).toBeDisabled();
        await page
            .getByLabel(
                "I have headphones and a microphone, and have read the practice conditions.",
            )
            .check();
        await page
            .locator(".bank-cards .module-card")
            .first()
            .getByRole("button", { name: "Start", exact: true })
            .click();
        await expect(
            page.getByRole("heading", { name: "Complete the Words", exact: true }),
        ).toBeVisible();
        const id = page.url().split("/session/")[1];
        const headers = { Authorization: `Bearer ${tokens.accessToken}` };
        const before = await (
            await request.get(`/api/sessions/${id}`, { headers })
        ).json();
        const content = await (
            await request.get(`/api/sessions/${id}/steps/1`, { headers })
        ).text();
        expect(content).not.toContain('"answer":');
        expect(content).not.toContain("explanation");
        expect(
            (
                await request.get(`/api/sessions/${id}/report`, { headers })
            ).status(),
        ).toBe(409);
        const guides = page.locator(".missing-letters-guide");
        await expect(guides).toHaveCount(10);
        await expect(guides.first()).toHaveText("___");
        const gap = page.getByRole("textbox", { name: "Missing letters 1", exact: true });
        await gap.fill("o");
        await expect(guides.first()).toHaveText("__");
        await gap.fill("");
        await expect(guides.first()).toHaveText("___");
        expect((await request.get("/favicon.svg")).status()).toBe(200);
        await page
            .getByRole("textbox", { name: "Missing letters 1", exact: true })
            .fill("ord");
        expect(before.canReview).toBe(true);
        expect(before.accessiblePositions).toHaveLength(11);
        await expect(page.getByRole("textbox", { name: /Missing letters/ })).toHaveCount(10);
        expect((await request.get(`/api/sessions/${id}/steps/2`, { headers })).status()).toBe(200);
        expect((await request.get(`/api/sessions/${id}/steps/12`, { headers })).status()).toBe(409);
        await page.getByRole("button", { name: "Next", exact: true }).click();
        await page.getByRole("button", { name: "Back", exact: true }).click();
        await expect(page.getByRole("textbox", { name: "Missing letters 1", exact: true })).toHaveValue("ord");
        expect(
            await page.evaluate(
                () => document.documentElement.scrollWidth <= innerWidth,
            ),
        ).toBe(true);
        await page.screenshot({
            path: `test-results/exam-reading-${mobile ? "mobile" : "desktop"}.png`,
            fullPage: true,
        });
        page.on("dialog", (dialog) => dialog.accept());
        await page.reload();
        await expect(
            page.getByRole("heading", { name: "Complete the Words", exact: true }),
        ).toBeVisible();
        const after = await (
            await request.get(`/api/sessions/${id}`, { headers })
        ).json();
        expect(after.deadline).toBe(before.deadline);
        await expect(page.getByRole("textbox", { name: "Missing letters 1", exact: true })).toHaveValue("ord");
        await page.getByRole("button", { name: "Finish module", exact: true }).click();
        await expect(page.getByRole("dialog")).toBeVisible();
        await page.getByRole("button", { name: "Submit module", exact: true }).click();
        await expect.poll(async () => (await (await request.get(`/api/sessions/${id}`, { headers })).json()).phase).not.toBe(before.phase);
        expect(
            (
                await request.get(`/api/sessions/${id}/steps/1`, { headers })
            ).status(),
        ).toBe(409);
    });

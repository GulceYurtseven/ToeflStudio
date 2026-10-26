import { test, expect } from "@playwright/test";
import { readFileSync } from "node:fs";
import { mockApi, runId } from "./fixtures";

test.use({
    launchOptions: {
        args: [
            "--use-fake-device-for-media-stream",
            "--use-fake-ui-for-media-stream",
        ],
    },
    permissions: ["microphone"],
});
test.setTimeout(60000);

async function speaking(page: import("@playwright/test").Page, fail: boolean) {
    await mockApi(page);
    const run = {
        id: runId,
        module: "listen-repeat",
        isCompleted: false,
        completedSteps: 0,
        totalSteps: 7,
        completionPercent: 0,
        nextPosition: 1,
        steps: Array.from({ length: 7 }, (_, i) => ({
            position: i + 1,
            status: i ? "locked" : "available",
            score: null as number | null,
        })),
    };
    const report = {
        title: "Practice",
        prompt: "Welcome to the learning center.",
        position: 1,
        module: "listen-repeat",
        durationMilliseconds: 8000,
        model: "test-double",
        rubricVersion: "test-v1",
        expiresAt: new Date(Date.now() + 1800000).toISOString(),
        feedback: {
            score: 4,
            transcript: "Welcome to the learning center.",
            rationale: "Anlaşılır bir tekrar.",
            strengths: ["Net konuşma."],
            issues: [],
            nextSteps: ["Vurgulara dikkat et."],
        },
    };
    const bodies: string[] = [];
    await page.route("**/api/speaking", (r) =>
        r.fulfill({
            json: {
                configured: true,
                modules: [
                    {
                        module: "listen-repeat",
                        totalSteps: 7,
                        completedSteps: 0,
                        completionPercent: 0,
                    },
                    {
                        module: "interview",
                        totalSteps: 4,
                        completedSteps: 0,
                        completionPercent: 0,
                    },
                ],
            },
        }),
    );
    await page.route("**/api/practice/speaking/**", async (route) => {
        const url = new URL(route.request().url()).pathname;
        if (url.endsWith("/audio"))
            return route.fulfill({
                contentType: "audio/wav",
                body: readFileSync(
                    "../ToeflExam.Api/Content/Speaking/1-1-v1.wav",
                ),
            });
        if (url.endsWith("/submissions")) {
            bodies.push(route.request().postData()!);
            if (fail && bodies.length === 1) return route.abort("failed");
            const payload = JSON.parse(bodies.at(-1)!);
            expect(payload.audioBase64).toMatch(/^UklGR/);
            const wave = Buffer.from(payload.audioBase64, "base64");
            expect(wave.readUInt32LE(4)).toBe(wave.length - 8);
            expect(wave.readUInt32LE(24)).toBe(16000);
            expect(wave.readUInt16LE(22)).toBe(1);
            expect(wave.readUInt16LE(34)).toBe(16);
            expect(wave.readUInt32LE(40)).toBe(wave.length - 44);
            expect(wave.length).toBeGreaterThan(44 + 6 * 32000);
            expect(wave.length).toBeLessThanOrEqual(44 + 8 * 32000);
            expect(wave.subarray(44).some((value) => value !== 0)).toBeTruthy();
            expect(payload.skip).toBe(false);
            run.completedSteps = 1;
            run.completionPercent = 14.29;
            run.nextPosition = 2;
            run.steps[0].score = 4;
            run.steps[0].status = "completed";
            return route.fulfill({
                json: { progress: run, report, reportUnavailable: false },
            });
        }
        if (url.endsWith("/steps/1"))
            return route.fulfill({
                json: {
                    position: 1,
                    question: {
                        title: "Learning center",
                        context: "Campus",
                        instructions: "Listen and repeat.",
                        responseSeconds: 8,
                        scenePosition: 1,
                    },
                    report: null,
                },
            });
        return route.fulfill({ json: run });
    });
    return bodies;
}

test("recording stops on deadline, retry preserves audio/id and feedback exports", async ({
    page,
}, info) => {
    const bodies = await speaking(page, true);
    await page.addInitScript(() => {
        BaseAudioContext.prototype.decodeAudioData = () =>
            Promise.reject(
                new DOMException(
                    "Unable to decode audio data",
                    "EncodingError",
                ),
            );
        Object.defineProperty(window, "MediaRecorder", { value: undefined });
    });
    await page.goto(`/#/speaking/listen-repeat/${runId}/1`);
    await page.getByRole("button", { name: "Listen and record" }).click();
    await expect(
        page.getByText("Speak now — recording", { exact: false }),
    ).toBeVisible({ timeout: 15000 });
    await expect(
        page.getByText("Your recording is ready", { exact: false }),
    ).toBeVisible({
        timeout: 15000,
    });
    expect(await page.locator("audio").getAttribute("src")).toMatch(/^blob:/);
    await page.getByRole("button", { name: "Assess recording" }).click();
    await expect(
        page.getByRole("button", { name: "Record again" }),
    ).toBeDisabled();
    await page
        .getByRole("button", { name: "Resubmit the same recording" })
        .click();
    await expect(
        page.getByRole("heading", { name: "Your speaking feedback" }),
    ).toBeVisible();
    expect(bodies).toHaveLength(2);
    expect(bodies[0]).toBe(bodies[1]);
    await expect(
        page.getByRole("button", { name: /download|PDF|JSON|print/i }),
    ).toHaveCount(0);
    expect(
        await page.evaluate(
            () => document.documentElement.scrollWidth <= innerWidth,
        ),
    ).toBeTruthy();
    await page.screenshot({
        path: `test-results/speaking-feedback-${info.project.name}.png`,
        fullPage: true,
    });
    await page.emulateMedia({ media: "print" });
    await expect(page.locator(".speaking-feedback")).toBeVisible();
    await expect(page.locator(".sidebar")).toBeHidden();
    await expect(
        page.getByRole("button", { name: "Download report" }),
    ).toBeHidden();
});

test("cancel and navigation release the microphone without sending audio", async ({
    page,
}) => {
    const bodies = await speaking(page, false);
    await page.addInitScript(() => {
        const get = navigator.mediaDevices.getUserMedia.bind(
            navigator.mediaDevices,
        );
        (window as any).capturedStreams = [];
        navigator.mediaDevices.getUserMedia = async (options) => {
            const stream = await get(options);
            (window as any).capturedStreams.push(stream);
            return stream;
        };
    });
    await page.goto(`/#/speaking/listen-repeat/${runId}/1`);
    await page.getByRole("button", { name: "Listen and record" }).click();
    await expect(
        page.getByText("Speak now — recording", { exact: false }),
    ).toBeVisible({ timeout: 15000 });
    await page.getByRole("link", { name: "Back to Speaking Studio" }).click();
    await expect
        .poll(() =>
            page.evaluate(() =>
                (window as any).capturedStreams.every((s: MediaStream) =>
                    s.getTracks().every((t) => t.readyState === "ended"),
                ),
            ),
        )
        .toBe(true);
    expect(bodies).toHaveLength(0);
});

test("microphone denial is recoverable and never submits", async ({ page }) => {
    const bodies = await speaking(page, false);
    await page.addInitScript(() => {
        navigator.mediaDevices.getUserMedia = async () => {
            throw new DOMException("denied", "NotAllowedError");
        };
    });
    await page.goto(`/#/speaking/listen-repeat/${runId}/1`);
    await page.getByRole("button", { name: "Listen and record" }).click();
    await expect(
        page.getByText("Microphone or audio playback permission was denied.", {
            exact: false,
        }),
    ).toBeVisible();
    await expect(
        page.getByRole("button", { name: "Listen and record" }),
    ).toBeEnabled();
    expect(bodies).toHaveLength(0);
});

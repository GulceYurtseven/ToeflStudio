import { Page } from "@playwright/test";
export const runId = "11111111-1111-4111-8111-111111111111";
export const modules = ["build-sentence", "email", "academic-discussion"];
export async function mockApi(
    page: Page,
    options: {
        configured?: boolean;
        failOnce?: boolean;
        expired?: boolean;
        unauthorized?: boolean;
    } = {},
) {
    await page.addInitScript(() => {
        if (!sessionStorage.getItem("test.initialized")) {
            sessionStorage.setItem("test.initialized", "yes");
            sessionStorage.setItem(
                "toefl.session",
                JSON.stringify({
                    accessToken: "ui-test-token",
                    refreshToken: "ui-refresh-token",
                    email: "learner@example.test",
                }),
            );
        }
    });
    const run: any = {
        id: runId,
        totalSteps: 2,
        completedSteps: 0,
        completionPercent: 0,
        correctAnswers: 0,
        averageRawScore: null,
        isCompleted: false,
        nextPosition: 1,
        steps: [
            { position: 1, status: "available" },
            { position: 2, status: "locked" },
        ],
    };
    const reports: Record<number, any> = {};
    const submissions: any[] = [];
    let failed = false;
    let attempts = 0;
    const sentence = {
        id: "q1",
        prompt: "What does Maya do before class?",
        tiles: [
            { id: 16, text: "library" },
            { id: 95, text: "visit" },
            { id: 42, text: "visits" },
            { id: 87, text: "the" },
        ],
        responseParts: [
            { isSlot: false, text: "Before class, Maya " },
            ...Array(3).fill({ isSlot: true, text: null }),
            { isSlot: false, text: "." },
        ],
        slotCount: 3,
    };
    const question = (module: string) =>
        module === "build-sentence"
            ? sentence
            : {
                  id: "q1",
                  title:
                      module === "email"
                          ? "Request a study room"
                          : "Recorded university lectures",
                  context:
                      module === "email"
                          ? "You organize a study group of six students. Write to the library coordinator, Ms. Chen."
                          : "Professor Allen: Should recordings be available to all students? Explain your position.",
                  instructions: "Write your response in English.",
                  requirements: [
                      "State your purpose.",
                      "Give supporting details.",
                      "Suggest a solution.",
                  ],
                  posts:
                      module === "email"
                          ? []
                          : [
                                {
                                    name: "Maya",
                                    text: "Recordings help students review difficult topics.",
                                },
                                {
                                    name: "Leo",
                                    text: "Participation may decrease.",
                                },
                            ],
              };
    const feedback = {
        rawScore: 1,
        maxRawScore: 1,
        explanation: "Maya üçüncü tekil şahıs olduğu için visits kullanılır.",
        correctSentences: ["Before class, Maya visits the library."],
        acceptedOrders: [[42, 87, 16]],
    };
    await page.route("**/api/**", async (route) => {
        const req = route.request();
        const path = new URL(req.url()).pathname;
        const method = req.method();
        const respond = (body: any, status = 200) =>
            route.fulfill({
                status,
                contentType: "application/json",
                body: JSON.stringify(body),
            });
        if (path === "/api/auth/providers") return respond({ google: false });
        if (path === "/api/auth/csrf") return respond({ token: "csrf-test" });
        if (path === "/api/contact") return respond({ configured: true });
        if (path === "/api/auth/manage/info") return respond({ email: "learner@example.test", username: "learner", displayName: "Learner" });
        if (path === "/api/listening") return respond({ modules: [] });
        if (path === "/api/sessions/catalog")
            return respond({ listening: [], exams: [], limitations: [] });
        if (path.startsWith("/api/test-format/"))
            return respond({
                approximateBaseMinutes: path.endsWith("writing") ? 23 : 30,
                scoredItemCount: 35,
                possibleUnscoredItems: 15,
                maxRawScore: 35,
                source: "https://rr.ets.org/index.php/etsrr/article/download/28/17/34",
            });
        if (path === "/api/auth/login")
            return respond({
                accessToken: "renewed-token",
                refreshToken: "renewed-refresh",
            });
        if (path === "/api/auth/register")
            return route.fulfill({ status: 200, body: "" });
        if (path === "/api/auth/refresh")
            return options.unauthorized
                ? respond({}, 401)
                : respond({
                      accessToken: "renewed-token",
                      refreshToken: "renewed-refresh",
                  });
        if (
            options.unauthorized &&
            !req.headers().authorization?.includes("renewed-token")
        )
            return respond({}, 401);
        if (path === "/api/writing/configuration")
            return respond({
                configured: options.configured !== false,
                model: "test-model",
            });
        if (path === "/api/writing")
            return respond({
                modules: modules.map((module) => ({
                    module,
                    title: module,
                    totalSteps: 2,
                    completedSteps: run.completedSteps,
                    completionPercent: run.completionPercent,
                    runId: run.id,
                })),
            });
        const module = path.includes("build-sentence")
            ? "build-sentence"
            : path.includes("academic-discussion")
              ? "academic-discussion"
              : "email";
        if (path.endsWith("/path")) return respond({ totalSteps: 2 });
        if (path.endsWith("/restart")) {
            run.id = "22222222-2222-4222-8222-222222222222";
            run.completedSteps = 0;
            run.completionPercent = 0;
            run.isCompleted = false;
            run.nextPosition = 1;
            run.steps = [
                { position: 1, status: "available" },
                { position: 2, status: "locked" },
            ];
            for (const key of Object.keys(reports)) delete reports[Number(key)];
            return respond(run);
        }
        if (path.endsWith("/runs"))
            return respond(method === "POST" ? run : [run]);
        if (/\/runs\/[^/]+$/.test(path)) return respond(run);
        const position = Number(path.match(/steps\/(\d+)/)?.[1]);
        if (position) {
            if (method === "POST") {
                attempts++;
                const body = req.postDataJSON();
                submissions.push(body);
                if (options.failOnce && !failed) {
                    failed = true;
                    return respond(
                        {
                            detail: "Gemini zaman aşımına uğradı. Cevabını koruyup yeniden dene.",
                        },
                        503,
                    );
                }
                if (!reports[position]) {
                    if (module === "build-sentence")
                        reports[position] = { feedback };
                    else
                        reports[position] = {
                            report: {
                                runId: run.id,
                                position,
                                submissionId: body.submissionId,
                                module,
                                question: question(module),
                                answer: body.answer,
                                wordCount: body.answer.split(/\s+/).length,
                                feedback: {
                                    score: 4,
                                    rationale:
                                        "Amacın açık, ancak çözüm önerin daha ayrıntılı olabilir.",
                                    strengths: ["Uygun bir üslup kullandın."],
                                    issues: [
                                        {
                                            category: "task",
                                            evidence: "",
                                            explanation:
                                                "Somut bir tarih belirtilmemiş.",
                                            suggestion: "Give a specific date.",
                                        },
                                    ],
                                    nextSteps: ["Somut bir örnek ekle."],
                                    improvedResponse:
                                        "Dear Ms. Chen, I would like to reserve a room next Thursday.",
                                },
                                model: "test-model",
                                rubricVersion: "v1",
                                expiresAt: new Date(
                                    Date.now() + 1800000,
                                ).toISOString(),
                                isOfficialScore: false,
                                maxRawScore: 5,
                            },
                        };
                    run.completedSteps++;
                    run.completionPercent = run.completedSteps * 50;
                    run.steps[position - 1] = {
                        position,
                        status: "completed",
                        score: 4,
                        rawScore: 1,
                        submissionId: body.submissionId,
                    };
                    run.isCompleted = run.completedSteps === 2;
                    run.nextPosition = run.isCompleted
                        ? null
                        : run.completedSteps + 1;
                    if (!run.isCompleted)
                        run.steps[run.completedSteps].status = "available";
                }
                return respond({ progress: run, ...reports[position] });
            }
            return respond({
                position,
                question: question(module),
                ...(options.expired ? {} : reports[position] || {}),
            });
        }
        return respond({}, 404);
    });
    return {
        run,
        submissions,
        get attempts() {
            return attempts;
        },
    };
}

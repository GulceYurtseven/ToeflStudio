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
const id = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
const base = {
    id,
    mode: "exam",
    form: "mock-1",
    completed: false,
    completedActivities: 0,
    totalActivities: 2,
    completionPercent: 0,
    nextPosition: 1,
    deadline: null,
    pendingEvaluations: 0,
    accessiblePositions: [1],
};
const question = {
    id: "new-content",
    section: "reading",
    kind: "daily-life",
    title: "Exam notice",
    text: "The workshop opens at nine. Bring a notebook.",
    context: "",
    instructions: "Read the notice.",
    prompt: "",
    prefix: "",
    suffix: "",
    tiles: [],
    questions: [
        {
            id: 1,
            prompt: "When does the workshop open?",
            prefix: "",
            missingLetters: 0,
            options: [
                { id: "A", text: "At nine" },
                { id: "B", text: "At ten" },
                { id: "C", text: "At noon" },
                { id: "D", text: "In the evening" },
            ],
        },
    ],
    requirements: [],
    posts: [],
    responseSeconds: 0,
    scenePosition: 0,
};
async function auth(page: import("@playwright/test").Page) {
    await page.addInitScript(() =>
        sessionStorage.setItem(
            "toefl.session",
            JSON.stringify({
                accessToken: "test-token",
                refreshToken: "test-refresh",
                email: "bank@example.test",
                displayName: "Bank Learner",
            }),
        ),
    );
}
function wave() {
    const b = Buffer.alloc(44 + 6400);
    b.write("RIFF");
    b.writeUInt32LE(b.length - 8, 4);
    b.write("WAVEfmt ", 8);
    b.writeUInt32LE(16, 16);
    b.writeUInt16LE(1, 20);
    b.writeUInt16LE(1, 22);
    b.writeUInt32LE(16000, 24);
    b.writeUInt32LE(32000, 28);
    b.writeUInt16LE(2, 32);
    b.writeUInt16LE(16, 34);
    b.write("data", 36);
    b.writeUInt32LE(6400, 40);
    return b;
}

test("A forward-only task retries the exact payload after a lost submission", async ({
    page,
}) => {
    await auth(page);
    const bodies: string[] = [];
    let accepted = false;
    await page.route("**/api/sessions/**", async (route) => {
        const path = new URL(route.request().url()).pathname;
        if (route.request().method() === "POST") {
            bodies.push(route.request().postData()!);
            if (bodies.length === 1) return route.abort("failed");
            accepted = true;
            return route.fulfill({ json: { progress: base, feedback: null } });
        }
        if (path.includes("/steps/"))
            return route.fulfill({
                json: {
                    position: Number(path.split("/").at(-1)),
                    question: {
                        ...question,
                        title: path.endsWith("/2")
                            ? "Second notice"
                            : "Exam notice",
                    },
                    feedback: null,
                },
            });
        return route.fulfill({
            json: accepted
                ? {
                      ...base,
                      accessiblePositions: [3],
                      nextPosition: 3,
                      completionPercent: 50,
                  }
                : base,
        });
    });
    await page.goto(`/#/session/${id}`);
    await expect(
        page.getByRole("button", { name: "Submit and continue" }),
    ).toBeDisabled();
    await page.getByRole("radio", { name: "At nine", exact: true }).check();
    await expect(
        page.getByRole("button", { name: /Passage|Previous|Leave unanswered/ }),
    ).toHaveCount(0);
    await page.getByRole("button", { name: "Submit and continue" }).click();
    await expect(page.getByRole("alert")).toContainText("Connection lost");
    await page
        .getByRole("button", { name: "Retry submission", exact: true })
        .first()
        .click();
    await expect.poll(() => bodies.length).toBe(2);
    expect(bodies[0]).toBe(bodies[1]);
    expect(JSON.parse(bodies[0]).answers).toEqual({
        "1": "A",
    });
});

test("deadline automatically submits the current answer and opens only the next question", async ({
    page,
}) => {
    await auth(page);
    await page.clock.install();
    let submitted: any;
    let position = 1;
    await page.route("**/api/sessions/**", (route) => {
        const path = new URL(route.request().url()).pathname;
        const progress = {
            ...base,
            accessiblePositions: [position],
            nextPosition: position,
            deadline: new Date(Date.now() + position * 60000).toISOString(),
        };
        if (route.request().method() === "POST") {
            submitted = route.request().postDataJSON();
            position = 2;
            return route.fulfill({
                json: {
                    progress: {
                        ...progress,
                        nextPosition: 2,
                        accessiblePositions: [2],
                    },
                    feedback: null,
                },
            });
        }
        if (path.includes("/steps/"))
            return route.fulfill({
                json: {
                    position,
                    question: {
                        ...question,
                        title: position === 1 ? "Exam notice" : "Next question",
                    },
                    feedback: null,
                },
            });
        return route.fulfill({ json: progress });
    });
    await page.goto(`/#/session/${id}`);
    await page.getByRole("radio", { name: "At nine", exact: true }).check();
    await expect(
        page.getByRole("button", { name: /Passage|Previous|Leave unanswered/ }),
    ).toHaveCount(0);
    await page.clock.fastForward(61000);
    await expect(
        page.getByText("Next question", { exact: true }),
    ).toBeVisible();
    expect(submitted.answers).toEqual({ "1": "A" });
    expect(submitted.skip).toBe(false);
});

test("Listening hides transcript, advances questions forward and shows feedback before final completion", async ({
    page,
}) => {
    await auth(page);
    let completed = false;
    let submitted: any;
    const progress = {
        ...base,
        mode: "listening",
        form: "conversation",
        accessiblePositions: [1],
        totalActivities: 1,
    };
    await page.route("**/api/sessions/**", async (route) => {
        const path = new URL(route.request().url()).pathname;
        if (path.endsWith("/audio"))
            return route.fulfill({ contentType: "audio/wav", body: wave() });
        if (route.request().method() === "POST") {
            submitted = route.request().postDataJSON();
            completed = true;
            return route.fulfill({
                json: {
                    progress: {
                        ...progress,
                        completed: true,
                        completionPercent: 100,
                        nextPosition: null,
                        accessiblePositions: [],
                    },
                    feedback: {
                        transcript: "The private transcript is now available.",
                        score: 2,
                        maxScore: 2,
                        items: [
                            {
                                id: 1,
                                score: 1,
                                correctAnswer: "At nine",
                                explanation: "The speaker gives this time.",
                            },
                            {
                                id: 2,
                                score: 1,
                                correctAnswer: "At ten",
                                explanation: "The second event begins then.",
                            },
                        ],
                    },
                },
            });
        }
        if (path.includes("/steps/"))
            return route.fulfill({
                json: {
                    position: 1,
                    question: {
                        ...question,
                        section: "listening",
                        kind: "conversation",
                        text: "",
                        questions: [
                            question.questions[0],
                            {
                                ...question.questions[0],
                                id: 2,
                                prompt: "When is the second event?",
                            },
                        ],
                    },
                    feedback: null,
                },
            });
        return route.fulfill({
            json: completed
                ? {
                      ...progress,
                      completed: true,
                      completionPercent: 100,
                      accessiblePositions: [],
                  }
                : progress,
        });
    });
    await page.goto(`/#/session/${id}`);
    await expect(page.getByRole("radio")).toHaveCount(0);
    await expect(page.getByText("The private transcript")).toHaveCount(0);
    await page.locator("audio").evaluate((el: HTMLAudioElement) => el.play());
    await page.getByRole("radio", { name: "At nine", exact: true }).check();
    await page.getByRole("button", { name: "Next question" }).click();
    await expect(
        page.getByText("When does the workshop open?", { exact: false }),
    ).toHaveCount(0);
    await page.getByRole("radio", { name: "At ten", exact: true }).check();
    await page.getByRole("button", { name: "Submit and continue" }).click();
    await expect(page.getByRole("heading", { name: "2 / 2" })).toBeVisible();
    await page.getByText("Transcript", { exact: true }).click();
    await expect(
        page.getByText("The private transcript is now available."),
    ).toBeVisible();
    expect(submitted.answers).toEqual({ "1": "A", "2": "B" });
    await page.getByRole("button", { name: "Next activity" }).click();
    await expect(
        page.getByRole("heading", { name: "Path complete" }),
    ).toBeVisible();
});

test("Exam report labels content-only scoring and prints all final feedback", async ({
    page,
}) => {
    await auth(page);
    let printCalled = false;
    await page.exposeFunction("testPrint", () => {
        printCalled = true;
    });
    await page.addInitScript(() => {
        window.print = () => {
            void (window as any).testPrint();
        };
    });
    const report = {
        id,
        form: "mock-1",
        expiresAt: new Date(Date.now() + 1800000).toISOString(),
        sections: ["reading", "listening", "writing", "speaking"].map(
            (section) => ({ section, score: 4, maximum: 5, pending: 0 }),
        ),
        detailsUnavailable: false,
        limitations: [
            "Transcript content estimate; pronunciation and fluency are not assessed.",
        ],
        details: [
            {position:3, section:"reading", kind:"complete-words", title:"A field record", passage:"Researchers record their results.", questions:[{id:1,prompt:"Complete the missing letters.",prefix:"rec",missingLetters:3,options:[]},{id:2,prompt:"Complete the missing letters.",prefix:"res",missingLetters:4,options:[]}],answer:{answers:{"1":"ord"}},score:1,maximum:2,pending:false,feedback:{items:[{id:1,correctAnswer:"record",score:1,explanation:"Use the base form."},{id:2,correctAnswer:"results",score:0,explanation:"Use a plural noun."}]}},
            {position:2,section:"reading",kind:"daily-life",title:"Exam notice",prompt:"",passage:question.text,instructions:question.instructions,questions:question.questions,requirements:[],posts:[],timedOut:false,answer:{answers:{"1":"At nine"}},score:1,maximum:1,pending:false,feedback:{explanation:"The notice gives this time."}},
            {
                position: 1,
                section: "speaking",
                kind: "interview",
                title: "An interview response",
                prompt: "Explain your view.",
                passage: "",
                timedOut: false,
                answer: { text: "My recognized response." },
                score: 4,
                maximum: 5,
                pending: false,
                feedback: {
                    rationale: "A relevant example supports the opinion.",
                    issues: [
                        {
                            category: "language",
                            explanation: "A verb agreement error.",
                            suggestion: "Use the singular verb.",
                        },
                    ],
                    nextSteps: ["Develop the example further."],
                },
            },
        ],
    };
    await page.route("**/api/sessions/**", (route) =>
        route.fulfill({
            json: route.request().url().endsWith("/study-plan")
                ? { sections: ["reading", "listening", "writing", "speaking"].map(section => ({section, summary: "Review your observed errors.", priorities: [{skill: "Accuracy", evidence: "Check the relevant detail.", positions: [1], exercise: "Practice five similar questions.", successCheck: "Explain four correct answers."}]})) }
                : route.request().url().endsWith("/report")
                ? report
                : {
                      ...base,
                      completed: true,
                      accessiblePositions: [],
                      nextPosition: null,
                  },
        }),
    );
    await page.goto(`/#/session/${id}`);
    await expect(
        page.getByRole("heading", { name: "Your exam report" }),
    ).toBeVisible();
    await expect(page.getByText("My recognized response.")).toBeVisible();
    await expect(page.getByText("Complete the missing letters.", {exact:true})).toHaveCount(0);
    const completion = page.locator(".bank-report-item").filter({has:page.getByRole("heading", {name:"A field record"})});
    await expect(completion).toContainText("Your response: record");
    await expect(completion).toContainText("Your response: No response");
    await expect(completion).toContainText("Correct answer: results");
    expect(await page.locator(".bank-report-item .bank-passage").evaluateAll(nodes => nodes.every(n => !!n.textContent?.trim()))).toBe(true);

    await expect(page.getByText("What to study next · reading")).toBeVisible();
    await expect(page.getByRole("region", { name: "Estimated TOEFL score" })).toContainText("5.0 / 6");
    await expect(page.getByText("Estimated band: 5.0 / 6", { exact: true })).toHaveCount(4);
    await expect(page.getByText(/AI can make mistakes/)).toBeVisible();

    await expect(page.getByText(/Download before/)).toHaveCount(0);
    await expect(page.getByText(/Your responses and detailed feedback are not/)).toHaveCount(0);
    await expect(page.getByText("Raw points: 1 per correct answer; 0 per incorrect or unanswered item.")).toHaveCount(2);
    await expect(page.getByText(/Raw points: Build a Sentence/)).toBeVisible();

    await expect(page.getByRole("heading", {name:"1. When does the workshop open?"})).toBeVisible();
    await expect(page.getByText("A. At nine", {exact:true})).toBeVisible();
    await expect(page.getByRole("button", {name:/JSON/i})).toHaveCount(0);
    await expect(
        page.getByText("Transcript content estimate; delivery unassessed"),
    ).toBeVisible();
    await page.getByRole("button", { name: "Save as PDF" }).click();
    await expect.poll(() => printCalled).toBe(true);
    await page.emulateMedia({ media: "print" });
    await expect(page.getByText("Use the singular verb.")).toBeVisible();
    await expect(page.getByText(/AI can make mistakes/)).toBeVisible();
    await expect(page.getByRole("region", { name: "Estimated TOEFL score" })).toContainText("5.0 / 6");

    await expect(page.locator(".bank-actions")).toBeHidden();
    if(test.info().project.name === "desktop") await page.pdf({path:"test-results/exam-report.pdf",format:"A4",printBackground:true});
    expect(
        await page.evaluate(
            () => document.documentElement.scrollWidth <= innerWidth,
        ),
    ).toBeTruthy();
    await page.screenshot({
        path: `test-results/exam-report-${test.info().project.name}.png`,
        fullPage: true,
    });
});

test.describe("Speaking in a continuous exam", () => {
    test("records PCM after the prompt and submits automatically without an assessment pause", async ({
        page,
    }) => {
        await auth(page);
        let submitted: any;
        const progress = {
            ...base,
            accessiblePositions: [1],
            deadline: new Date(Date.now() + 25000).toISOString(),
        };
        await page.route("**/api/sessions/**", async (route) => {
            const path = new URL(route.request().url()).pathname;
            if (path.endsWith("/audio"))
                return route.fulfill({
                    contentType: "audio/wav",
                    body: wave(),
                });
            if (route.request().method() === "POST") {
                submitted = route.request().postDataJSON();
                return route.fulfill({ json: { progress, feedback: null } });
            }
            if (path.includes("/steps/"))
                return route.fulfill({
                    json: {
                        position: 1,
                        question: {
                            ...question,
                            section: submitted ? "writing" : "speaking",
                            kind: submitted ? "email" : "listen-repeat",
                            title: submitted
                                ? "Next exam task"
                                : "Repeat the recording",
                            text: "",
                            prompt: "",
                            questions: [],
                            responseSeconds: 1,
                            scenePosition: 1,
                        },
                        feedback: null,
                    },
                });
            return route.fulfill({ json: progress });
        });
        await page.goto(`/#/session/${id}`);
        await expect.poll(() => !!submitted, { timeout: 12000 }).toBe(true);
        expect(
            Buffer.from(submitted.audioBase64, "base64")
                .subarray(0, 4)
                .toString(),
        ).toBe("RIFF");
        await expect(
            page.getByText("Next exam task", { exact: true }),
        ).toBeVisible();
    });
});

test("vocabulary targets are bold and module completion uses an accessible dialog", async ({page}) => {
    await auth(page);
    let finishes = 0;
    await page.route("**/api/sessions/**", async route => {
        const path = new URL(route.request().url()).pathname;
        if (path.endsWith("/finish-phase")) { finishes++; return route.fulfill({json:{progress:{...base,canReview:true},feedback:null}}); }
        if (path.includes("/steps/")) return route.fulfill({json:{position:1,question:{...question,text:"The remainder drains away slowly.",questions:[{...question.questions[0],prompt:'What does "remainder" mean here?'}]},feedback:null}});
        return route.fulfill({json:{...base,canReview:true,phase:"reading-router"}});
    });
    await page.goto(`/#/session/${id}`);
    await expect(page.getByRole("heading", {name:"Read in Daily Life",exact:true})).toBeVisible();
    await expect(page.locator(".bank-passage strong.vocabulary-target")).toHaveText("remainder");
    await page.getByRole("button", {name:"Finish module",exact:true}).click();
    await expect(page.getByRole("dialog",{name:"Finish this module?"})).toBeVisible();
    expect(finishes).toBe(0);
    await page.screenshot({path:`test-results/module-confirm-${test.info().project.name}.png`,fullPage:true});
    await page.getByRole("button",{name:"Keep reviewing"}).click();
    await expect(page.getByRole("dialog")).toHaveCount(0);
    await page.getByRole("button", {name:"Finish module",exact:true}).click();
    await page.getByRole("button", {name:"Submit module",exact:true}).click();
    await expect.poll(() => finishes).toBe(1);
});

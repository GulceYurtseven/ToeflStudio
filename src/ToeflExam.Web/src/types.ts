export type Module = "build-sentence" | "email" | "academic-discussion";
export const modules: Record<
    Module,
    {
        title: string;
        short: string;
        caption: string;
        description: string;
        icon: string;
        tag: string;
    }
> = {
    "build-sentence": {
        title: "Build a Sentence",
        short: "Sentence building",
        caption: "FROM WORDS TO SENTENCES",
        description:
            "Arrange words and strengthen your understanding of English sentence structure.",
        icon: "tiles",
        tag: "Grammar",
    },
    email: {
        title: "Write an Email",
        short: "Email writing",
        caption: "WRITE WITH A PURPOSE",
        description:
            "Write clear, purposeful emails for everyday and academic situations.",
        icon: "mail",
        tag: "Communication",
    },
    "academic-discussion": {
        title: "Academic Discussion",
        short: "Academic discussion",
        caption: "DEVELOP YOUR IDEAS",
        description:
            "Form an opinion, support it with examples, and add your contribution to the discussion.",
        icon: "chat",
        tag: "Idea development",
    },
};
export interface ModuleProgress {
    module: Module;
    title: string;
    totalSteps: number;
    completedSteps: number;
    completionPercent: number;
    runId: string | null;
}
export interface Run {
    id: string;
    isCompleted: boolean;
    completedSteps: number;
    totalSteps: number;
    completionPercent: number;
    nextPosition: number | null;
    correctAnswers?: number;
    averageRawScore?: number | null;
    steps: {
        position: number;
        status: string;
        rawScore?: number | null;
        score?: number | null;
        wordCount?: number | null;
        submissionId?: string | null;
    }[];
}
export interface Question {
    id: string;
    title?: string;
    prompt?: string;
    instructions?: string;
    context?: string;
    requirements?: string[];
    posts?: { name: string; text: string }[];
    tiles?: { id: number; text: string }[];
    responseParts?: { isSlot: boolean; text: string | null }[];
    slotCount?: number;
}
export interface SentenceFeedback {
    rawScore: number;
    maxRawScore: number;
    explanation: string;
    correctSentences: string[];
}
export interface Feedback {
    score: number;
    rationale: string;
    strengths: string[];
    issues: {
        category: string;
        evidence: string;
        explanation: string;
        suggestion: string;
    }[];
    nextSteps: string[];
    improvedResponse: string;
}
export interface Report {
    runId: string;
    position: number;
    submissionId: string;
    module: string;
    question: Question;
    answer: string;
    wordCount: number;
    feedback: Feedback;
    model: string;
    rubricVersion: string;
    createdAt: string;
    expiresAt: string;
    maxRawScore: number;
    isOfficialScore: boolean;
}
export interface Step {
    position: number;
    question: Question;
    feedback?: SentenceFeedback | null;
    report?: Report | null;
}
export interface Submission {
    progress: Run;
    feedback?: SentenceFeedback;
    report?: Report | null;
    reportUnavailable?: boolean;
}
export interface Configuration {
    configured: boolean;
    model: string;
}
/** Builds the API root for a validated task module. */
export const base = (module: Module) =>
    module === "build-sentence"
        ? "/api/practice/build-sentence"
        : "/api/practice/writing/" + module;
/** Narrows a route value to a supported Writing module name. */
export const isModule = (value: string): value is Module =>
    Object.hasOwn(modules, value);

import { test, expect } from "@playwright/test";
import { estimateSection, estimateOverall } from "../src/examEstimate";

/** Exercises the disclosed practice scale, including rounding and invalid grading states. */
test("practice estimate respects boundaries and never treats pending work as zero", () => {
    const section = { section: "reading", score: 0, maximum: 20, pending: 0 };
    expect(estimateSection(section)).toBe(1);
    expect(estimateSection({ ...section, score: 20 })).toBe(6);
    expect(estimateSection({ ...section, score: 1 })).toBe(1.5);
    expect(estimateSection({ ...section, pending: 1 })).toBeNull();
    expect(estimateSection({ ...section, maximum: 0 })).toBeNull();
    expect(estimateSection({ ...section, score: -1 })).toBeNull();
    expect(estimateSection({ ...section, score: 21 })).toBeNull();
    expect(estimateSection({ ...section, score: NaN })).toBeNull();
});

/** Each skill carries equal weight, regardless of its raw maximum or number of items. */
test("overall estimate requires four unique completed skills and uses equal weights", () => {
    const sections = [
        { section: "reading", score: 35, maximum: 35, pending: 0 },
        { section: "listening", score: 35, maximum: 35, pending: 0 },
        { section: "writing", score: 10, maximum: 20, pending: 0 },
        { section: "speaking", score: 22, maximum: 55, pending: 0 },
    ];
    // 6, 6, 3.5, 3 => 4.625 => 4.5, not a percentage of pooled raw points.
    expect(estimateOverall(sections)).toBe(4.5);
    expect(estimateOverall(sections.slice(0, 3))).toBeNull();
    expect(estimateOverall([...sections.slice(0, 3), sections[0]])).toBeNull();
    expect(estimateOverall(sections.map(s => ({ ...s, pending: 1 })))).toBeNull();
});

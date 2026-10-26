export type ScoredSection = {
    section: string;
    score: number;
    maximum: number;
    pending: number;
};

/** Maps completed practice points to a transparent, uncalibrated 1–6 learning estimate. */
export function estimateSection(section: ScoredSection): number | null {
    if (section.pending !== 0 || !Number.isFinite(section.score) ||
        !Number.isFinite(section.maximum) || section.maximum <= 0 ||
        section.score < 0 || section.score > section.maximum) return null;
    return Math.round((1 + 5 * section.score / section.maximum) * 2) / 2;
}

/** Requires all four distinct sections and averages their estimates with equal weight. */
export function estimateOverall(sections: ScoredSection[]): number | null {
    const required = ["reading", "listening", "writing", "speaking"];
    if (sections.length !== required.length) return null;
    const values = required.map(name => {
        const matches = sections.filter(s => s.section === name);
        return matches.length === 1 ? estimateSection(matches[0]) : null;
    });
    if (values.some(value => value === null)) return null;
    return Math.round(values.reduce<number>((sum, value) => sum + value!, 0) / 4 * 2) / 2;
}

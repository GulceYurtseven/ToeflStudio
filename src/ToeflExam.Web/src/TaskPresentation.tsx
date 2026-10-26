/** Uses official task names instead of internal content-bank labels. */
export const taskNames: Record<string, string> = {
    "complete-words": "Complete the Words", "daily-life": "Read in Daily Life",
    "academic-passage": "Read an Academic Passage", "build-sentence": "Build a Sentence",
    email: "Write an Email", "academic-discussion": "Write for an Academic Discussion",
    "listen-repeat": "Listen and Repeat", interview: "Take an Interview",
    "choose-response": "Listen and Choose a Response", conversation: "Listen to a Conversation",
    announcement: "Listen to an Announcement", "academic-talk": "Listen to an Academic Talk",
};

/** Extracts explicitly quoted vocabulary targets, never the correct option, from authored prompts. */
export function vocabularyTargets(items: { prompt: string }[]): string[] {
    return items.filter(i => /mean|meaning|closest|refer|word|phrase/i.test(i.prompt))
        .flatMap(i => [...i.prompt.matchAll(/["“]([^"”]+)["”]/g)].map(m => m[1]));
}

/** Highlights literal whole words as safe React text, including repeated occurrences. */
export function PassageText({ text, targets }: { text: string; targets: string[] }) {
    const words = [...new Set(targets)].filter(Boolean).sort((a, b) => b.length - a.length);
    if (!words.length) return <>{text}</>;
    const pattern = words.map(w => w.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")).join("|");
    const matcher = new RegExp(`\\b(${pattern})\\b`, "gi");
    const nodes = []; let start = 0;
    for (const match of text.matchAll(matcher)) {
        nodes.push(text.slice(start, match.index));
        nodes.push(<strong className="vocabulary-target" key={match.index}>{match[0]}</strong>);
        start = match.index! + match[0].length;
    }
    nodes.push(text.slice(start));
    return <>{nodes}</>;
}

import "./missingLetters.css";

/** Shows one underscore per unfilled letter while retaining native editing and selection. */
export function MissingLetters({ count, value, label, disabled, onChange }: {
    count: number;
    value: string;
    label: string;
    disabled: boolean;
    onChange: (value: string) => void;
}) {
    return <span className="missing-letters" style={{ width: `${count * 1.35 + 0.3}ch` }}>
        <span className="missing-letters-guide" aria-hidden="true">
            {"\u00a0".repeat(value.length) + "_".repeat(Math.max(0, count - value.length))}
        </span>
        <input
            aria-label={label}
            title={`${count} missing letters`}
            maxLength={count}
            value={value}
            disabled={disabled}
            autoComplete="off"
            autoCapitalize="none"
            spellCheck={false}
            onChange={e => onChange(e.target.value.replace(/[^a-z]/gi, "").slice(0, count))}
        />
    </span>;
}

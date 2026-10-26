namespace ToeflExam.Application.Assessment;

// Reference data, not an operational exam timer or a calibrated routing algorithm.
public static class TestFormat2026
{
    public const string Version = "ets-rr25-12-2026.1";
    public const string Manual = "https://rr.ets.org/index.php/etsrr/article/download/28/17/34";
    public const string TimingSource = "https://www.ets.org/toefl/test-takers/ibt/about/content.html";
    public const string PracticeSource = "https://www.ets.org/content/dam/ets-org/pdfs/toefl/toefl-ibt-teachers-resources-practice-test-1.pdf";

    /// <summary>Looks up the documented 2026 format metadata for a supported section.</summary>
    public static SectionFormat? Section(string section) => section switch
    {
        "reading" => new(section, true, 30, 35, 15, 35,
            [new("complete-words", 10, 10, 10), new("daily-life", 5, 5, 0), new("academic-passage", 5, 0, 5)], []),
        "listening" => new(section, true, 29, 35, 12, 35,
            [new("choose-response", 8, 7, 3), new("conversation", 4, 4, 4), new("announcement", 4, 4, 0), new("academic-talk", 4, 0, 8)], []),
        "writing" => new(section, false, 23, 12, 0, 20, [],
            [new("build-sentence", 10, 1, null), new("email", 1, 5, 420), new("academic-discussion", 1, 5, 600)]),
        "speaking" => new(section, false, 8, 11, 0, 55, [],
            [new("listen-repeat", 7, 5, null), new("interview", 4, 5, 45)]),
        _ => null
    };
}

public sealed record AdaptiveTask(string Module, int Router, int Lower, int Upper);
public sealed record LinearTask(string Module, int ItemCount, int MaxRawScorePerItem, int? ConfirmedTimeSeconds);
public sealed record SectionFormat(string Section, bool IsAdaptive, int ApproximateBaseMinutes,
    int ScoredItemCount, int PossibleUnscoredItems, int MaxRawScore,
    AdaptiveTask[] AdaptiveTasks, LinearTask[] LinearTasks)
{
    public string Version => TestFormat2026.Version;
    public string Source => TestFormat2026.Manual;
    public string TimingSource => TestFormat2026.TimingSource;
    public string TaskTimingSource => Section == "speaking" ? "https://www.toefl-ibt.jp/test_takers/toefl_ibt/202601/" : TestFormat2026.PracticeSource;
    public bool IsExamSimulationAvailable => false;
    // Null deliberately: the manual does not publish operational clocks or routing thresholds.
    public int? RouterTimeSeconds => null;
    public int? SecondStageTimeSeconds => null;
    public int? RoutingCutoff => null;
    public bool HasOfficialScoreConversion => false;
}

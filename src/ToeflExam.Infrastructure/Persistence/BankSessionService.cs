using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System.Text.Json;
using ToeflExam.Infrastructure.Persistence.Entities;
using ToeflExam.Application.ContentBank;
using ToeflExam.Application.Learning.BuildSentence;
using ToeflExam.Application.Reading;
using ToeflExam.Application.Speaking;
using ToeflExam.Application.Writing;
using ToeflExam.Domain.Learning;
using ToeflExam.Domain.Speaking;
using ToeflExam.Domain.Writing;
using ToeflExam.Infrastructure.Assessment;

namespace ToeflExam.Infrastructure.Persistence;

public sealed class BankSessionService(ToeflExamDbContext db, IContentBank bank, TimeProvider clock, VolatileExamStore memory,
    IWritingScorer writing, ISpeakingScorer speaking, QuestionAudioFiles audioFiles, IExamCoach coach) : IBankSessionService
{
    public static readonly string[] ListeningKinds = ["choose-response", "conversation", "announcement", "academic-talk"];
    public static readonly string[] Limitations = [
        "Independent practice simulation, not an official ETS test. No official 1–6 band conversion is available.",
        "The two-stage item blueprint follows ETS RR-25-12 Table 1. Routing at 12/20 is a local practice rule, not ETS calibration.",
        "Reading permits review within the current module; Complete the Words displays all ten gaps together. Finishing a module locks it. Listening and Speaking are forward-only. Sentence-building responses can be reviewed until that set is finished.",
        "Email: 7 minutes; Academic Discussion: 10 minutes. Speaking: 8–12-second repetitions and 45-second interviews; the per-sentence repetition allocation is a practice setting.",
        "Speaking uses transcription followed by text assessment. Pronunciation, intelligibility, fluency, rhythm and intonation are not assessed; this is not a full Speaking rubric score.",
        "Original synthetic audio is used. The latest completed report for each exam is saved privately, replacing the previous report only when a new run is completed. Pending recordings are retained for AI analysis and removed from the saved report after transcription and assessment."
    ];
    /// <summary>Resumes an existing run or creates an ordered run for the selected content.</summary>
    public async Task<BankProgress> StartAsync(string user, string mode, string form, CancellationToken ct)
    {
        var content = Content(mode, form);
        var old = await db.BankSessions.Include(x => x.Steps).Where(x => x.UserId == user && x.Mode == mode && x.Form == form).OrderByDescending(x => x.StartedAt).FirstOrDefaultAsync(ct);
        // Retain legacy score records; a new format starts a separate run instead of rewriting answers or positions.
        if (old is { Mode: "exam", Edition: < 3, CompletedAt: null }) return await Create(user, mode, form, content, old.Id, ct);
        if (old is not null) return await ProgressAsync(user, old.Id, ct);
        return await Create(user, mode, form, content, null, ct);
    }
    /// <summary>Loads student-owned run summaries with the requested scope.</summary>
    public async Task<BankProgress[]> HistoryAsync(string user, string mode, string form, CancellationToken ct)
    {
        _ = Content(mode, form);
        var savedId = mode == "exam" ? await db.LatestExamReports.Where(x => x.UserId == user && x.Form == form).Select(x => (Guid?)x.SessionId).SingleOrDefaultAsync(ct) : null;
        return (await db.BankSessions.AsNoTracking().Include(x => x.Steps).Where(x => x.UserId == user && x.Mode == mode && x.Form == form)
            .OrderByDescending(x => x.StartedAt).Take(20).ToArrayAsync(ct)).Select(x => Progress(x) with { HasSavedReport = x.Id == savedId }).ToArray();
    }
    /// <summary>Resolves or creates a new run linked to a completed source run.</summary>
    public async Task<BankProgress> RestartAsync(string user, Guid id, CancellationToken ct)
    {
        var source = await Require(user, id, ct);
        if (source.CompletedAt is null) throw new PracticeConflictException("Complete this run before restarting.");
        var existing = await db.BankSessions.Include(x => x.Steps).SingleOrDefaultAsync(x => x.UserId == user && x.RestartedFromId == id, ct);
        if (existing is not null) return Progress(existing);
        if (await db.BankSessions.AnyAsync(x => x.UserId == user && x.Mode == source.Mode && x.Form == source.Form && x.StartedAt > source.StartedAt, ct)) throw new PracticeConflictException("Restart your latest run.");
        return await Create(user, source.Mode, source.Form, Content(source.Mode, source.Form), id, ct);
    }
    /// <summary>Loads content for the selected module or exam edition.</summary>
    private BankEntry[] Content(string mode, string form)
    {
        var rows = mode switch
        {
            "listening" when ListeningKinds.Contains(form) => bank.Practice.Where(x => x.Section == "listening" && x.Kind == form).ToArray(),
            "exam" when new[] { "mock-1", "mock-2", "mock-3" }.Contains(form) => bank.Exams.Where(x => x.ExamId == form).ToArray(),
            _ => throw new PracticeNotFoundException()
        };
        if (rows.Length == 0) throw new PracticeNotFoundException(); return rows;
    }
    /// <summary>Creates the requested practice state using the selected content edition.</summary>
    private async Task<BankProgress> Create(string user, string mode, string form, BankEntry[] entries, Guid? source, CancellationToken ct)
    {
        if (entries.Where(x => x.AudioFile.Length > 0).Any(e => !File.Exists(audioFiles.Resolve("Bank", e.AudioFile))))
            throw new SpeakingScoringException("The question audio package is incomplete. Please try again after it has been restored.", true);
        var run = new BankSession { Id = Guid.NewGuid(), UserId = user, Mode = mode, Form = form, Edition = 3, StartedAt = clock.GetUtcNow(), RestartedFromId = source };
        if (mode == "exam") memory.Create(user, run.Id);
        var rows = entries.SelectMany(e => mode == "exam" && e.Questions.Length > 0 && e.Kind != "complete-words"
            ? e.Questions.Select((_, index) => (Entry: e, Item: index)) : new[] { (Entry: e, Item: -1) }).ToArray();
        run.Steps = rows.Select((row, i) => new BankStep { SessionId = run.Id, Position = i + 1,
            ContentId = row.Entry.Id, ItemIndex = row.Item, Section = row.Entry.Section, Stage = row.Entry.Stage, Kind = row.Entry.Kind,
            Maximum = row.Item >= 0 ? 1 : row.Entry.Questions.Length > 0 ? row.Entry.Questions.Length : row.Entry.Kind == "build-sentence" ? 1 : 5 }).ToList();
        if (mode == "exam") run.Deadline = clock.GetUtcNow().AddSeconds(QuestionSeconds(run, run.Steps[0]));
        db.BankSessions.Add(run);
        try { await Save(ct); }
        catch { if (mode == "exam") memory.Remove(user, run.Id); throw; }
        return Progress(run);
    }
    /// <summary>Resolves the complete question material for a pinned exam step.</summary>
    private BankEntry FullEntry(BankSession run, BankStep step) => (run.Mode == "exam" ? bank.Exams : bank.Practice).Single(x => x.Id == step.ContentId);
    /// <summary>Resolves the content exposed at the current practice position.</summary>
    private BankEntry Entry(BankSession run, BankStep step)
    {
        var entry = FullEntry(run, step);
        if(step.ItemIndex < 0) return entry;
        var item = entry.Questions[step.ItemIndex];
        var text = entry.Text;
        if(entry.Kind == "complete-words") foreach(var other in entry.Questions.Where(q => q.Id != item.Id))
            text = text.Replace("{{" + other.Id + "}}", other.Prefix + new string('_', other.MissingLetters), StringComparison.Ordinal);
        return entry with { Questions = [item], Text = text };
    }
    /// <summary>Calculates the custom practice-question deadline from its task type and audio duration.</summary>
    private double QuestionSeconds(BankSession run, BankStep step)
    {
        var entry = Entry(run, step);
        return step.Section switch {
            "reading" => step.Stage == "router" ? 1080 : 720,
            "listening" => (step.Kind == "choose-response" ? 20 : 35) + (step.ItemIndex <= 0 ? AudioSeconds(entry) : 0),
            "writing" => step.Kind == "build-sentence" ? 360 : step.Kind == "email" ? 420 : 600,
            "speaking" => entry.ResponseSeconds + AudioSeconds(entry) + 2,
            _ => throw new InvalidOperationException("Invalid exam section.")
        };
    }
    /// <summary>Selects the first unanswered, included question in the ordered run.</summary>
    private static BankStep? Current(BankSession run) => run.Steps.Where(x => !x.Excluded && x.SubmissionId is null).OrderBy(x => x.Position).FirstOrDefault();
    /// <summary>Builds a stable phase key for the current section and question position.</summary>
    private static string Phase(BankStep s) => s.Section == "reading" ? "reading-" + s.Stage
        : s.Kind == "build-sentence" ? "writing-build-sentence" : s.Section + "-" + s.Position;
    /// <summary>Allows review only while the current Reading module or sentence-building set remains open.</summary>
    private static bool Reviewable(BankStep step) => step.Section == "reading" || step.Kind == "build-sentence";
    private static BankStep[] PhaseSteps(BankSession run, BankStep anchor) => run.Steps
        .Where(s => !s.Excluded && Phase(s) == Phase(anchor)).OrderBy(s => s.Position).ToArray();
    private static int ItemCount(BankStep step) => step.Section is "reading" or "listening" ? step.Maximum : 1;
    /// <summary>Rejects an unavailable or unauthorized practice resource.</summary>
    private async Task<BankSession> Require(string user, Guid id, CancellationToken ct)
        => await db.BankSessions.Include(x => x.Steps).SingleOrDefaultAsync(x => x.Id == id && x.UserId == user, ct) ?? throw new PracticeNotFoundException();
    /// <summary>Serializes operations on a student-owned exam workspace.</summary>
    private async Task<T> Locked<T>(string user, Guid id, Func<Task<T>> action, CancellationToken ct, bool wait = false)
    {
        var state = memory.Get(user, id);
        if (state is null) return await action();
        if (wait) await state.Gate.WaitAsync(ct);
        else if (!await state.Gate.WaitAsync(0, ct)) throw new PracticeConflictException("An exam operation is already in progress. Retry shortly.");
        try { return await action(); } finally { state.Gate.Release(); }
    }
    /// <summary>Loads the student-owned run and returns its current progress.</summary>
    public Task<BankProgress> ProgressAsync(string user, Guid id, CancellationToken ct) => Locked(user, id, () => ProgressCore(user, id, ct), ct, wait: true);
    /// <summary>Updates expiration state before returning current run progress.</summary>
    private async Task<BankProgress> ProgressCore(string user, Guid id, CancellationToken ct)
    { var run = await Require(user, id, ct); RequireCurrentEdition(run); await Expire(run, ct); return Progress(run); }
    /// <summary>Rejects unsupported legacy exam sessions with a recoverable conflict.</summary>
    private static void RequireCurrentEdition(BankSession run)
    {
        if (run is { Mode: "exam", Edition: < 3, CompletedAt: null })
            throw new PracticeConflictException("This exam uses an earlier format. Open Practice Exams and start the updated version. Previous score records are preserved.");
    }
    /// <summary>Loads an accessible practice step without disclosing future answer keys.</summary>
    public Task<BankStepDto> StepAsync(string user, Guid id, int position, CancellationToken ct) => Locked(user, id, () => StepCore(user, id, position, ct), ct, wait: true);
    /// <summary>Validates the current session and returns the permitted question.</summary>
    private async Task<BankStepDto> StepCore(string user, Guid id, int position, CancellationToken ct)
    {
        var run = await Require(user, id, ct); await Expire(run, ct); var step = Accessible(run, position); var e = Entry(run, step);
        return new(position, Dto(e), run.Mode == "listening" && step.SubmissionId.HasValue ? Feedback(e, step.Scores) : null, step.ItemIndex <= 0,
            run.Mode == "exam" && Reviewable(step) ? memory.Get(user, id)?.Answers.GetValueOrDefault(position) : null);
    }
    /// <summary>Builds a student-safe question DTO without answer keys.</summary>
    private static BankQuestionDto Dto(BankEntry e) => new(e.Id, e.Section, e.Kind, e.Title, e.Section == "listening" ? "" : e.Text, e.Context, e.Instructions,
        e.Section == "speaking" ? "" : e.Prompt, e.Prefix, e.Suffix, e.Tiles, e.Questions.Select(q => new ReadingItemDto(q.Id, q.Prompt, q.Prefix, q.MissingLetters, q.Options)).ToArray(), e.Requirements, e.Posts, e.ResponseSeconds, e.ScenePosition);
    /// <summary>Rejects locked or out-of-range practice steps.</summary>
    private static BankStep Accessible(BankSession run, int position)
    {
        RequireCurrentEdition(run);
        var step = run.Steps.SingleOrDefault(x => x.Position == position && !x.Excluded) ?? throw new PracticeNotFoundException();
        var current = Current(run);
        if (run.Mode == "exam")
        {
            if (current is null || step.SubmissionId.HasValue || (Reviewable(current) ? Phase(step) != Phase(current) : step != current)) throw new PracticeConflictException("This exam item is locked.");
        }
        else if (step != current && !step.SubmissionId.HasValue) throw new PracticeConflictException("Complete the preceding activity first.");
        return step;
    }
    /// <summary>Serializes access before resolving an accessible question's audio.</summary>
    public Task<string> AudioAsync(string user, Guid id, int position, CancellationToken ct) => Locked(user, id, () => AudioCore(user, id, position, ct), ct, wait: true);
    /// <summary>Checks step access before returning the audio asset name.</summary>
    private async Task<string> AudioCore(string user, Guid id, int position, CancellationToken ct)
    { var run = await Require(user, id, ct); await Expire(run, ct); var step = Accessible(run, position); if(run.Mode == "exam" && step.ItemIndex > 0) throw new PracticeConflictException("The audio has already been played for this material."); var entry = Entry(run, step); if (entry.AudioFile.Length == 0) throw new PracticeNotFoundException(); return entry.AudioFile; }
    /// <summary>Checks submission identity and run ownership before recording scores and progress.</summary>
    public Task<BankResult> SubmitAsync(string user, Guid id, int position, BankAnswer answer, CancellationToken ct) => Locked(user, id, () => SubmitCore(user, id, position, answer, ct), ct);
    /// <summary>Applies the current response once and advances the ordered exam session.</summary>
    private async Task<BankResult> SubmitCore(string user, Guid id, int position, BankAnswer answer, CancellationToken ct)
    {
        Validate(answer);
        var run = await Require(user, id, ct);
        var old = run.Steps.SingleOrDefault(x => x.Position == position && !x.Excluded) ?? throw new PracticeNotFoundException();
        if (old.SubmissionId == answer.SubmissionId) return new(Progress(run), run.Mode == "listening" ? Feedback(Entry(run, old), old.Scores) : null);
        await Expire(run, ct); var step = Accessible(run, position);
        if (run.Mode == "exam" && Reviewable(step)) throw new PracticeConflictException("Save this response as a draft, then finish the module to submit it.");

        if (run.Steps.Any(x => x.SubmissionId == answer.SubmissionId)) throw new ArgumentException("Submission ID was already used.");
        var state = run.Mode == "exam" ? memory.Get(user, id) ?? throw new WritingReportExpiredException() : null;
        var retained = Apply(run, step, answer);
        using var pending = state is null ? null : memory.Stage(state, new Dictionary<int, BankAnswer> { { position, retained } });
        Advance(run, Phase(step), clock.GetUtcNow()); await Save(ct); pending?.Commit();
        return new(Progress(run), run.Mode == "listening" ? Feedback(Entry(run, step), step.Scores) : null);
    }
    /// <summary>Saves a revisable response in the bounded memory workspace without revealing a score.</summary>
    public Task<BankProgress> SaveDraftAsync(string user, Guid id, int position, BankAnswer answer, CancellationToken ct) => Locked(user, id, async () =>
    {
        var run = await Require(user, id, ct); await Expire(run, ct);
        var step = Accessible(run, position);
        if (run.Mode != "exam" || !Reviewable(step)) throw new PracticeConflictException("This task does not allow draft review.");
        ValidateDraft(Entry(run, step), answer);
        var state = memory.Get(user, id) ?? throw new WritingReportExpiredException();
        memory.Put(state, position, answer with { Answers = answer.Answers is null ? null : new(answer.Answers), Order = answer.Order?.ToArray() });
        return Progress(run);
    }, ct, wait: true);

    /// <summary>Checks every draft against its question before changing any scores or retained data.</summary>
    private static void ValidateDraft(BankEntry entry, BankAnswer answer)
    {
        Validate(answer);
        if (answer.AudioBase64 is not null || answer.Text is not null || answer.Skip)
            throw new ArgumentException("Only objective answers belong in this module.");
        if (entry.Kind == "build-sentence")
        {
            if (answer.Answers is { Count: > 0 } || answer.Order?.Any(n => !entry.Tiles.Any(t => t.Id == n)) == true || answer.Order?.Distinct().Count() != answer.Order?.Length)
                throw new ArgumentException("Invalid sentence draft.");
        }
        else if (answer.Order is { Length: > 0 } || answer.Answers?.Any(pair =>
            !entry.Questions.Any(q => q.Id == pair.Key && (q.Options.Length == 0 || pair.Value.Length == 0 || q.Options.Any(o => o.Id == pair.Value)))) == true)
            throw new ArgumentException("Invalid reading draft.");
    }

    /// <summary>Atomically closes an entire reviewable phase; an anchor prevents a retry from closing the next phase.</summary>
    public Task<BankResult> FinishPhaseAsync(string user, Guid id, BankPhaseSubmission submission, CancellationToken ct) => Locked<BankResult>(user, id, async () =>
    {
        if (submission.Responses is null || submission.Responses.Count > 35) throw new ArgumentException("Invalid module submission.");
        var run = await Require(user, id, ct); await Expire(run, ct);
        var anchor = run.Steps.SingleOrDefault(s => s.Position == submission.AnchorPosition && !s.Excluded) ?? throw new PracticeNotFoundException();
        if (run.Mode != "exam" || !Reviewable(anchor)) throw new PracticeConflictException("This task is submitted one question at a time.");
        if (anchor.SubmissionId.HasValue) return new(Progress(run), null);
        _ = Accessible(run, anchor.Position);
        var phase = PhaseSteps(run, anchor);
        if (submission.Responses.Keys.Any(p => !phase.Any(s => s.Position == p))) throw new ArgumentException("A response belongs to another module.");
        var state = memory.Get(user, id) ?? throw new WritingReportExpiredException();
        var drafts = phase.ToDictionary(s => s.Position, s => submission.Responses.GetValueOrDefault(s.Position)
            ?? state.Answers.GetValueOrDefault(s.Position) ?? new BankAnswer(Guid.NewGuid(), null, null, null, null));
        foreach (var s in phase) ValidateDraft(Entry(run, s), drafts[s.Position]);
        var retained = phase.ToDictionary(s => s.Position, s => Apply(run, s, drafts[s.Position]));
        using var pending = memory.Stage(state, retained);
        Advance(run, Phase(anchor), clock.GetUtcNow()); await Save(ct); pending.Commit();
        return new(Progress(run), null);
    }, ct, wait: true);

    /// <summary>Checks sizes, required values and scoring invariants before accepting external data.</summary>
    private static void Validate(BankAnswer answer)
    {
        if (answer.SubmissionId == Guid.Empty || (answer.Text?.Length ?? 0) > 20000 || (answer.AudioBase64?.Length ?? 0) > 2000000 ||
            (answer.Order?.Length ?? 0) > 12 || answer.Answers is { Count: > 10 } || answer.Answers?.Any(x => x.Value is null || x.Value.Length > 80) == true)
            throw new ArgumentException("Invalid answer size or submission ID.");
    }
    /// <summary>Scores objective responses or marks written and spoken responses for later assessment.</summary>
    private BankAnswer Apply(BankSession run, BankStep step, BankAnswer answer)
    {
        var e = Entry(run, step); var scores = new int[e.QuestionCount]; var subjective = e.Section == "speaking" || e.Kind is "email" or "academic-discussion";
        if (!answer.Skip)
        {
            if (e.Questions.Length > 0)
            {
                var values = answer.Answers ?? [];
                if (values.Keys.Any(k => !e.Questions.Any(q => q.Id == k))) throw new ArgumentException("Unknown item ID.");
                scores = e.Questions.Select(q =>
                {
                    var text = values.GetValueOrDefault(q.Id, "").Trim();
                    if (q.Options.Length > 0 && text.Length > 0 && !q.Options.Any(o => o.Id == text)) throw new ArgumentException("Unknown option.");
                    return string.Equals(text, q.Answer, StringComparison.OrdinalIgnoreCase) ? 1 : 0;
                }).ToArray();
            }
            else if (e.Kind == "build-sentence")
            {
                if (answer.Order?.Any(n => !e.Tiles.Any(t => t.Id == n)) == true || answer.Order?.Distinct().Count() != answer.Order?.Length) throw new ArgumentException("Invalid tile selection.");
                scores = [(answer.Order ?? []).SequenceEqual(e.Order) ? 1 : 0];
            }
            else if (e.Section == "speaking" && !string.IsNullOrWhiteSpace(answer.AudioBase64)) _ = SpeakingAudio.Parse(answer.AudioBase64, e.ResponseSeconds);
        }
        var hasResponse = !answer.Skip && (e.Section == "speaking" ? !string.IsNullOrWhiteSpace(answer.AudioBase64) : !string.IsNullOrWhiteSpace(answer.Text));
        var retained = answer.Skip ? new BankAnswer(answer.SubmissionId, null, null, null, null, true) :
            e.Questions.Length > 0 ? answer with { Answers = answer.Answers is null ? [] : new(answer.Answers), Text = null, Order = null, AudioBase64 = null } :
            e.Kind == "build-sentence" ? answer with { Answers = null, Text = null, Order = answer.Order?.ToArray(), AudioBase64 = null } :
            e.Section == "speaking" ? answer with { Answers = null, Text = null, Order = null } : answer with { Answers = null, Order = null, AudioBase64 = null };
        step.Scores = subjective && hasResponse ? [] : scores; step.NeedsEvaluation = subjective && hasResponse;
        step.SubmissionId = answer.SubmissionId; step.SubmittedAt = clock.GetUtcNow(); run.Revision++;
        return retained;
    }
    /// <summary>Moves the run forward and assigns the next server-controlled deadline.</summary>
    private void Advance(BankSession run, string previousPhase, DateTimeOffset start)
    {
        if (run.Mode == "exam")
        {
            foreach (var section in new[] { "reading", "listening" })
            {
                var router = run.Steps.Where(x => x.Section == section && x.Stage == "router").ToArray();
                if (router.Length > 0 && router.All(x => x.SubmissionId.HasValue))
                {
                    var selected = router.Sum(x => x.Scores.Sum()) >= 12 ? "upper" : "lower";
                    foreach (var step in run.Steps.Where(x => x.Section == section && x.Stage != "router")) step.Excluded = step.Stage != selected;
                }
            }
        }
        var next = Current(run);
        if (next is null)
        { run.CompletedAt = start; run.Deadline = null; if (memory.Get(run.UserId, run.Id) is { } state) memory.Complete(state); return; }
        if (run.Mode == "exam" && Phase(next) != previousPhase)
        {
            var seconds = QuestionSeconds(run, next);
            run.Deadline = start.AddSeconds(seconds);
        }
    }
    /// <summary>Reads the authored audio duration used by the practice timer.</summary>
    private double AudioSeconds(BankEntry entry)
    {
        var path = audioFiles.Resolve("Bank", entry.AudioFile);
        using var reader = new BinaryReader(File.OpenRead(path));
        if (new string(reader.ReadChars(4)) != "RIFF") throw new InvalidOperationException("Invalid question audio.");
        reader.ReadInt32(); reader.ReadChars(4); int rate = 32000;
        while (reader.BaseStream.Position + 8 <= reader.BaseStream.Length)
        {
            var name = new string(reader.ReadChars(4)); var length = reader.ReadInt32(); var end = reader.BaseStream.Position + length;
            if (length < 0 || end > reader.BaseStream.Length) throw new InvalidOperationException("Invalid question audio.");
            if (name == "fmt ") { reader.ReadInt16(); reader.ReadInt16(); reader.ReadInt32(); rate = reader.ReadInt32(); }
            if (name == "data") return Math.Ceiling((double)length / rate);
            reader.BaseStream.Position = end + (length % 2);
        }
        throw new InvalidOperationException("Question audio has no samples.");
    }
    /// <summary>Automatically closes expired questions and advances the session.</summary>
    private async Task Expire(BankSession run, CancellationToken ct)
    {
        RequireCurrentEdition(run);
        var changed = false;
        while (run.Mode == "exam" && run.Deadline is { } end && clock.GetUtcNow() > end.AddSeconds(2) && Current(run) is { } current)
        {
            var phase = Phase(current);
            foreach (var step in run.Steps.Where(x => !x.Excluded && x.SubmissionId is null && Phase(x) == phase).ToArray())
            { step.TimedOut = true; Apply(run, step, memory.Get(run.UserId, run.Id)?.Answers.GetValueOrDefault(step.Position) ?? new(Guid.NewGuid(), null, null, null, null, true)); }
            Advance(run, phase, end); changed = true;
        }
        if (changed) await Save(ct);
    }
    /// <summary>Projects completed steps, scores and the next accessible activity.</summary>
    private static BankProgress Progress(BankSession run)
    {
        int Count(BankStep s) => run.Mode == "exam" ? ItemCount(s) : 1;
        var steps = run.Steps.Where(x => !x.Excluded).ToArray(); var done = steps.Where(x => x.SubmissionId.HasValue).Sum(Count);
        // Until routing occurs, only one of the second-stage branches will count toward completion.
        var total = steps.Sum(Count);
        foreach (var section in new[] { "reading", "listening" })
            if (steps.Any(x => x.Section == section && x.Stage == "lower") && steps.Any(x => x.Section == section && x.Stage == "upper"))
                total -= steps.Where(x => x.Section == section && x.Stage == "upper").Sum(ItemCount);
        var current = Current(run);
        var canReview = run.Mode == "exam" && current is not null && Reviewable(current);
        var accessible = current is null ? Array.Empty<int>() : canReview ? PhaseSteps(run, current).Select(s => s.Position).ToArray() : new[] { current.Position };
        return new(run.Id, run.Mode, run.Form, run.CompletedAt.HasValue, done, total, Math.Min(100, Math.Round(100m * done / Math.Max(1, total), 2)), current?.Position, run.Deadline, steps.Count(x => x.NeedsEvaluation), accessible, canReview, current is null ? null : Phase(current));
    }
    /// <summary>Creates the question text included in the completed exam review.</summary>
    private static string ReviewText(BankEntry e)
    {
        var text = e.Text;
        if (e.Kind == "complete-words") foreach (var q in e.Questions) text = text.Replace("{{" + q.Id + "}}", q.Prefix + q.Answer, StringComparison.Ordinal);
        return text;
    }
    /// <summary>Builds explanatory feedback from the question key and recorded objective scores.</summary>
    private static object Feedback(BankEntry e, int[] scores) => new
    {
        transcript = ReviewText(e),
        score = scores.Sum(),
        maxScore = e.QuestionCount,
        items = e.Questions.Select((q, i) => new { q.Id, score = scores.ElementAtOrDefault(i), correctAnswer = q.Options.Length == 0 ? q.Prefix + q.Answer : q.Options.Single(o => o.Id == q.Answer).Text, q.Explanation })
    };
    /// <summary>Retrieves the owner's latest durable report, independently of the in-memory cache.</summary>
    public Task<ExamReport> ReportAsync(string user, Guid id, CancellationToken ct) => Locked(user, id, () => ReportCore(user, id, ct), ct, wait: true);
    /// <summary>Loads saved detail or preserves a still-available legacy report on first access.</summary>
    private async Task<ExamReport> ReportCore(string user, Guid id, CancellationToken ct)
    {
        var run = await Require(user, id, ct); await Expire(run, ct);
        if (run.Mode != "exam" || run.CompletedAt is null) throw new PracticeConflictException("Finish the exam to view its report.");
        var saved = await SavedReport(run, ct);
        if (saved is not null) return StoredExamReport.Decode(saved.Payload).Report;
        var state = memory.Get(user, id);
        if (state is not null)
        {
            await StoreReport(run, state, ct);
            await Save(ct, archiveCompleted: false);
        }
        return Report(run, state);
    }
    /// <summary>Returns the completed assessment report while its temporary detail is available.</summary>
    private ExamReport Report(BankSession run, VolatileExamStore.State? state)
    {
        var steps = run.Steps.Where(x => !x.Excluded).OrderBy(x => x.Position).ToArray();
        var sections = steps.GroupBy(x => x.Section).Select(g => new SectionResult(g.Key, g.Sum(x => x.Scores.Sum()), g.Sum(x => x.Maximum), g.Count(x => x.NeedsEvaluation))).ToArray();
        var details = state is null ? [] : steps.Select(s =>
        {
            var e = Entry(run, s); var answer = state.Answers.GetValueOrDefault(s.Position);
            return (object)new
            {
                s.Position,
                s.Section,
                e.Kind,
                e.Title,
                e.Instructions,
                questions = Dto(e).Questions,
                e.Requirements,
                e.Posts,
                prompt = e.Prompt.Length > 0 ? e.Prompt : e.Context,
                passage = ReviewText(FullEntry(run, s)),
                s.TimedOut,
                answer = answer is null ? null : new { answers = answer.Answers?.ToDictionary(x => x.Key, x => e.Questions.Single(q => q.Id == x.Key).Options.FirstOrDefault(o => o.Id == x.Value)?.Text ?? x.Value), text = e.Kind == "build-sentence" && answer.Order is not { Length: > 0 } ? null : e.Kind == "build-sentence" ? e.Prefix + string.Join(" ", (answer.Order ?? []).Select(n => e.Tiles.Single(t => t.Id == n).Text)) + e.Suffix : answer.Text },
                score = s.NeedsEvaluation ? (int?)null : s.Scores.Sum(),
                s.Maximum,
                pending = s.NeedsEvaluation,
                feedback = state.Feedback.GetValueOrDefault(s.Position) ?? (e.Questions.Length > 0 ? Feedback(e, s.Scores) : e.Kind == "build-sentence" ? (object)new { correctAnswer = e.Prefix + string.Join(" ", e.Order.Select(n => e.Tiles.Single(t => t.Id == n).Text)) + e.Suffix, e.Explanation } : null)
            };
        }).ToArray();
        return new(run.Id, run.Form, run.CompletedAt!.Value, null, sections, details, Limitations, state is null);
    }
    /// <summary>Generates and caches owner-scoped guidance only after all grading succeeds.</summary>
    public Task<ExamStudyPlan> StudyPlanAsync(string user, Guid id, CancellationToken ct) => Locked(user, id, async () =>
    {
        var run = await Require(user, id, ct);
        if (run.Mode != "exam" || run.CompletedAt is null || run.Steps.Any(s => !s.Excluded && s.NeedsEvaluation))
            throw new PracticeConflictException("Complete all exam evaluations before generating study guidance.");
        _ = await ReportCore(user, id, ct);
        var saved = await SavedReport(run, ct) ?? throw new WritingReportExpiredException();
        var snapshot = StoredExamReport.Decode(saved.Payload);
        if (snapshot.StudyPlan is not null) return snapshot.StudyPlan;
        var plan = await coach.CreateAsync(snapshot.Report, ct);
        saved.Payload = (snapshot with { StudyPlan = plan }).Encode();
        run.Revision++;
        await Save(ct, archiveCompleted: false);
        if (memory.Get(user, id) is { } state) state.StudyPlan = plan;
        return plan;
    }, ct, wait: true);

    /// <summary>Evaluates one pending response while preserving progress and memory consistency.</summary>
    public async Task<ExamReport> EvaluateNextAsync(string user, Guid id, CancellationToken ct)
    {
        var owned = await Require(user, id, ct);
        if (owned.Mode != "exam" || owned.CompletedAt is null) throw new PracticeConflictException("Finish the exam before evaluation.");
        _ = await ReportCore(user, id, ct);
        var saved = await SavedReport(owned, ct) ?? throw new WritingReportExpiredException();
        var snapshot = StoredExamReport.Decode(saved.Payload);
        var state = memory.Restore(user, id, snapshot.Answers, snapshot.Feedback, snapshot.StudyPlan);
        if (!await state.Gate.WaitAsync(0, ct)) throw new PracticeConflictException("An evaluation is already in progress.");
        try
        {
            var run = await Require(user, id, ct);
            if (run.Mode != "exam" || run.CompletedAt is null) throw new PracticeConflictException("Finish the exam before evaluation.");
            var step = run.Steps.Where(x => !x.Excluded && x.NeedsEvaluation).OrderBy(x => x.Position).FirstOrDefault();
            if (step is null) return Report(run, state);
            var e = Entry(run, step); var answer = state.Answers.GetValueOrDefault(step.Position) ?? throw new WritingReportExpiredException();
            int score; object feedback; BankAnswer? transcriptAnswer = null;
            if (e.Section == "speaking")
            {
                var result = await RetryGeneratedAsync(() => speaking.ScoreAsync(new(e.Id, e.Kind == "listen-repeat" ? SpeakingTaskKind.ListenRepeat : SpeakingTaskKind.Interview, e.Title, e.Instructions, e.Context, e.Prompt, e.AudioFile, e.ResponseSeconds, e.ScenePosition), SpeakingAudio.Parse(answer.AudioBase64!, e.ResponseSeconds), ct), ct);
                score = result.Feedback.Score; feedback = result.Feedback;
                transcriptAnswer = answer with { AudioBase64 = null, Text = result.Feedback.Transcript };
            }
            else
            {
                var result = await RetryGeneratedAsync(() => writing.ScoreAsync(new(e.Id, e.Kind == "email" ? WritingTaskKind.Email : WritingTaskKind.AcademicDiscussion, e.Title, e.Instructions, e.Context, e.Requirements, e.Posts), answer.Text!, ct), ct);
                score = result.Feedback.Score; feedback = result.Feedback;
            }
            step.Scores = [score]; step.NeedsEvaluation = false; run.Revision++;
            using var staged = transcriptAnswer is null ? null : memory.Stage(state, new Dictionary<int, BankAnswer> { [step.Position] = transcriptAnswer });
            var previousFeedback = state.Feedback.GetValueOrDefault(step.Position);
            state.Feedback[step.Position] = feedback;
            try { await Save(ct); staged?.Commit(); }
            catch
            {
                if (previousFeedback is null) state.Feedback.TryRemove(step.Position, out _);
                else state.Feedback[step.Position] = previousFeedback;
                throw;
            }
            return Report(run, state);
        }
        finally { state.Gate.Release(); }
    }
    /// <summary>Retries malformed AI output once; provider failures are never converted to grades.</summary>
    private static async Task<T> RetryGeneratedAsync<T>(Func<Task<T>> evaluate, CancellationToken ct)
    {
        try { return await evaluate(); }
        catch (Exception e) when ((e is WritingScoringException || e is SpeakingScoringException) &&
            e.Message.StartsWith("The provider did not return a valid assessment.", StringComparison.Ordinal))
        {
            await Task.Delay(500, ct);
            return await evaluate();
        }
    }
    /// <summary>Loads only this owner's current report; replaced reports cannot be revived through an old URL.</summary>
    private async Task<LatestExamReport?> SavedReport(BankSession run, CancellationToken ct)
    {
        var saved = await db.LatestExamReports.SingleOrDefaultAsync(x => x.UserId == run.UserId && x.Form == run.Form, ct);
        if (saved is not null && saved.SessionId != run.Id)
            throw new PracticeConflictException("This report was replaced by a newer completed exam. Open your latest report from Practice Exams.");
        return saved;
    }

    /// <summary>Stages one compressed snapshot in the same SQL transaction as the completed run or AI score.</summary>
    private async Task StoreReport(BankSession run, VolatileExamStore.State state, CancellationToken ct)
    {
        var saved = await db.LatestExamReports.SingleOrDefaultAsync(x => x.UserId == run.UserId && x.Form == run.Form, ct);
        if (saved is not null && saved.SessionId != run.Id &&
            (saved.StartedAt > run.StartedAt || (saved.StartedAt == run.StartedAt && run.RestartedFromId != saved.SessionId)))
            throw new PracticeConflictException("A newer completed report is already saved. Open it from Practice Exams.");
        if (saved is null)
        {
            saved = new LatestExamReport { UserId = run.UserId, Form = run.Form };
            db.LatestExamReports.Add(saved);
        }
        saved.SessionId = run.Id;
        saved.StartedAt = run.StartedAt;
        saved.CompletedAt = run.CompletedAt!.Value;
        var json = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        saved.Payload = new StoredExamReport(Report(run, state), state.Answers.ToDictionary(),
            state.Feedback.ToDictionary(x => x.Key, x => JsonSerializer.SerializeToElement(x.Value, json)), state.StudyPlan).Encode();
    }

    /// <summary>Atomically saves latest reports and scores; version checks prevent stale AI work from replacing a newer report.</summary>
    private async Task Save(CancellationToken ct, bool archiveCompleted = true)
    {
        if (archiveCompleted)
            foreach (var entry in db.ChangeTracker.Entries<BankSession>().Where(x => x.State is EntityState.Added or EntityState.Modified).ToArray())
                if (entry.Entity is { Mode: "exam", CompletedAt: not null } run && memory.Get(run.UserId, run.Id) is { } state)
                    await StoreReport(run, state, ct);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw new PracticeConflictException("Progress changed. Reload and retry the same submission."); }
        catch (DbUpdateException e) when (e.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 }) { throw new PracticeConflictException("This operation has already been completed. Reload your progress."); }
    }
}

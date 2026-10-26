using ToeflExam.Application.ContentBank;
using ToeflExam.Application.Learning.BuildSentence;

namespace ToeflExam.Infrastructure.Assessment;

// Single-node workspace for active exams. Completed reports are persisted separately in SQL.
// Cached completed workspaces expire after 30 minutes; unfinished exams expire after 3 hours.
public sealed class VolatileExamStore : IDisposable
{
    public sealed class State(string user, DateTimeOffset expires)
    {
        public string User { get; } = user;
        public DateTimeOffset Expires { get; set; } = expires;
        public System.Collections.Concurrent.ConcurrentDictionary<int, BankAnswer> Answers { get; } = new();
        public System.Collections.Concurrent.ConcurrentDictionary<int, object> Feedback { get; } = new();
        public ExamStudyPlan? StudyPlan { get; set; }
        public SemaphoreSlim Gate { get; } = new(1, 1);
    }
    private readonly object sync = new();
    private readonly Dictionary<Guid, State> states = [];
    private readonly TimeProvider clock;
    private readonly ITimer sweeper;
    /// <summary>Initializes the volatile exam store with its required dependencies or validated state.</summary>
    public VolatileExamStore(TimeProvider clock)
    { this.clock = clock; sweeper = clock.CreateTimer(_ => Sweep(), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1)); }
    /// <summary>Removes expired exam workspaces from process memory.</summary>
    private void Sweep()
    { lock (sync) foreach (var id in states.Where(x => x.Value.Expires <= clock.GetUtcNow()).Select(x => x.Key).ToArray()) states.Remove(id); }
    /// <summary>Creates the requested practice state using the selected content edition.</summary>
    public State Create(string user, Guid id)
    {
        lock (sync)
        {
            Sweep();
            if (states.TryGetValue(id, out var old)) return old.User == user ? old : throw new PracticeNotFoundException();
            if (states.Count >= 32) throw new ToeflExam.Application.Speaking.SpeakingScoringException("Exam capacity is temporarily full. Please try again later.", true);
            return states[id] = new State(user, clock.GetUtcNow().AddHours(3));
        }
    }
    /// <summary>Retrieves the owner-scoped exam workspace after removing expired states.</summary>
    public State? Get(string user, Guid id)
    { lock (sync) { Sweep(); return states.TryGetValue(id, out var state) && state.User == user ? state : null; } }
    /// <summary>Atomically restores pending assessment inputs from an owner-checked completed report.</summary>
    public State Restore(string user, Guid id, IReadOnlyDictionary<int, BankAnswer> answers,
        IReadOnlyDictionary<int, System.Text.Json.JsonElement> feedback, ExamStudyPlan? plan)
    {
        lock (sync)
        {
            if (Get(user, id) is { } existing) return existing;
            var state = Create(user, id);
            try
            {
                using var staged = Stage(state, answers);
                foreach (var (position, value) in feedback) state.Feedback[position] = value;
                state.StudyPlan = plan;
                Complete(state);
                staged.Commit();
                return state;
            }
            catch { states.Remove(id); throw; }
        }
    }
    /// <summary>Stores temporary assessment detail without writing student answers to SQL.</summary>
    public void Put(State state, int position, BankAnswer answer)
    { using var pending = Stage(state, new Dictionary<int, BankAnswer> { { position, answer } }); pending.Commit(); }
    /// <summary>Removes a temporary exam workspace only when the owner matches.</summary>
    public void Remove(string user, Guid id)
    { lock (sync) if (states.TryGetValue(id, out var state) && state.User == user) states.Remove(id); }
    // The caller holds State.Gate. Reserve memory before SQL save, rolling back if SQL fails.
    public PendingSubmission Stage(State state, IReadOnlyDictionary<int, BankAnswer> answers)
    {
        lock (sync)
        {
            var previous = answers.Keys.ToDictionary(p => p, p => state.Answers.GetValueOrDefault(p));
            var bytes = states.Values.Sum(s => s.Answers.Values.Sum(Size));
            var delta = answers.Values.Sum(Size) - previous.Values.Sum(Size);
            if (bytes + delta > 128L * 1024 * 1024)
                throw new ToeflExam.Application.Speaking.SpeakingScoringException("Exam memory capacity is full. Your submission has not been accepted; retry shortly.", true);
            foreach (var (position, answer) in answers) state.Answers[position] = answer;
            return new PendingSubmission(this, state, previous);
        }
    }
    /// <summary>Estimates retained answer memory, including text and encoded audio.</summary>
    private static long Size(BankAnswer? a) => a is null ? 0 : (long)(a.AudioBase64?.Length ?? 0) * 2 + (a.Text?.Length ?? 0) * 2 + 4096;
    public sealed class PendingSubmission(VolatileExamStore owner, State state, Dictionary<int, BankAnswer?> previous) : IDisposable
    {
        private bool committed;
        /// <summary>Confirms staged in-memory answers after durable progress has been saved.</summary>
        public void Commit() => committed = true;
        /// <summary>Rolls back staged answers unless their SQL progress update was committed.</summary>
        public void Dispose()
        {
            if (committed) return;
            lock (owner.sync) foreach (var (position, answer) in previous)
                if (answer is null) state.Answers.TryRemove(position, out _); else state.Answers[position] = answer;
            committed = true;
        }
    }
    /// <summary>Starts the 30-minute retention window for a completed exam report.</summary>
    public void Complete(State state) { lock (sync) state.Expires = clock.GetUtcNow().AddMinutes(30); }
    /// <summary>Releases owned timers or cached resources.</summary>
    public void Dispose() => sweeper.Dispose();
}

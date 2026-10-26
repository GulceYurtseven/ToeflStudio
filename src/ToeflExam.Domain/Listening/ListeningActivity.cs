using System.Text.RegularExpressions;

namespace ToeflExam.Domain.Listening;

public enum ListeningTaskKind { ChooseResponse = 1, Conversation = 2, Announcement = 3, AcademicTalk = 4 }
public enum ListeningAccent { NorthAmerican = 1, British = 2, Australian = 3 }
public sealed record ListeningOption(int Id, string Text);
public sealed record ListeningSpeaker(string Name, ListeningAccent Accent);
public sealed record ListeningAudio(string AssetPath, int DurationMilliseconds);

public sealed class ListeningQuestion
{
    public int Id { get; }
    public string Prompt { get; }
    public IReadOnlyList<ListeningOption> Options { get; }
    public int CorrectOptionId { get; }
    public string Explanation { get; }

    /// <summary>Initializes the listening question with its required dependencies or validated state.</summary>
    public ListeningQuestion(int id, string prompt, IEnumerable<ListeningOption> options, int correctOptionId, string explanation)
    {
        var copy = options.ToArray();
        if (id < 1 || string.IsNullOrWhiteSpace(prompt) || string.IsNullOrWhiteSpace(explanation) ||
            copy.Length != 4 || copy.Any(o => o is null || o.Id < 1 || string.IsNullOrWhiteSpace(o.Text)) ||
            copy.Select(o => o.Id).Distinct().Count() != 4 || !copy.Any(o => o.Id == correctOptionId))
            throw new ArgumentException("A listening question requires four distinct options, a valid key and an explanation.");
        Id = id; Prompt = prompt; Options = Array.AsReadOnly(copy); CorrectOptionId = correctOptionId; Explanation = explanation;
    }

    /// <summary>Computes an objective raw score using the validated answer key.</summary>
    public int Score(int? selectedOptionId)
    {
        if (selectedOptionId is { } id && !Options.Any(o => o.Id == id))
            throw new ArgumentException("Unknown listening option.");
        return selectedOptionId == CorrectOptionId ? 1 : 0;
    }
}

// Versioned authored material. Transcript/key remain server-side until the future feedback phase.
public sealed class ListeningActivity
{
    public Guid Id { get; }
    public int Version { get; }
    public ListeningTaskKind Kind { get; }
    public string Title { get; }
    public string Transcript { get; }
    public IReadOnlyList<ListeningSpeaker> Speakers { get; }
    public IReadOnlyList<ListeningQuestion> Questions { get; }
    public ListeningAudio? Audio { get; private set; }
    public bool IsPublished { get; private set; }

    /// <summary>Initializes the listening activity with its required dependencies or validated state.</summary>
    public ListeningActivity(Guid id, int version, ListeningTaskKind kind, string title, string transcript,
        IEnumerable<ListeningSpeaker> speakers, IEnumerable<ListeningQuestion> questions)
    {
        var speakerCopy = speakers.ToArray(); var questionCopy = questions.ToArray();
        if (id == Guid.Empty || version < 1 || !Enum.IsDefined(kind) || string.IsNullOrWhiteSpace(title) ||
            string.IsNullOrWhiteSpace(transcript) || speakerCopy.Length == 0 || questionCopy.Length == 0 ||
            speakerCopy.Any(s => s is null || string.IsNullOrWhiteSpace(s.Name) || !Enum.IsDefined(s.Accent)) ||
            questionCopy.Any(q => q is null) || questionCopy.Select(q => q.Id).Distinct().Count() != questionCopy.Length)
            throw new ArgumentException("Invalid listening content.");
        if (kind == ListeningTaskKind.Conversation && speakerCopy.Length != 2)
            throw new ArgumentException("Conversation requires two speakers.");
        if (kind != ListeningTaskKind.Conversation && speakerCopy.Length != 1)
            throw new ArgumentException("This task requires one stimulus speaker.");
        if (kind == ListeningTaskKind.ChooseResponse && questionCopy.Length != 1)
            throw new ArgumentException("Choose a Response requires one question.");
        // Input is the spoken text only; narrator labels are separate metadata.
        if (kind == ListeningTaskKind.AcademicTalk && Regex.Matches(transcript, @"\b[\p{L}\p{N}]+(?:['’-][\p{L}\p{N}]+)*\b").Count is < 175 or > 250)
            throw new ArgumentException("Academic Talk must contain 175–250 spoken words.");
        Id = id; Version = version; Kind = kind; Title = title; Transcript = transcript;
        Speakers = Array.AsReadOnly(speakerCopy); Questions = Array.AsReadOnly(questionCopy);
    }

    /// <summary>Validates the Listening activity before allowing it to be served as published content.</summary>
    public void Publish(ListeningAudio audio, bool editorialApproved, bool audioApproved)
    {
        if (IsPublished) throw new InvalidOperationException("Published listening content is immutable; create a new version.");
        if (!editorialApproved || !audioApproved)
            throw new InvalidOperationException("Both editorial and audio review are required.");
        // Local asset IDs only. Storage existence/integrity checks belong to the future publishing service.
        if (audio is null || audio.DurationMilliseconds <= 0 ||
            !Regex.IsMatch(audio.AssetPath ?? "", @"^/media/listening/[a-zA-Z0-9_-]+\.(mp3|wav|ogg)$"))
            throw new ArgumentException("A reviewed local audio asset and duration are required.");
        Audio = audio; IsPublished = true;
    }
}

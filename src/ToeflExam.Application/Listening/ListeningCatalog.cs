using ToeflExam.Application.Assessment;
using ToeflExam.Domain.Listening;

namespace ToeflExam.Application.Listening;

public sealed record ListeningModuleGuide(string Module, string Title, string Description, string Instructions, string[] Skills)
{
    public string Status => "preparing";
    public bool CanStart => false;
    public int PublishedActivities => 0;
}

public static class ListeningCatalog
{
    /// <summary>Lists the supported Listening tasks and their student-facing instructions.</summary>
    public static ListeningModuleGuide[] Modules() =>
    [
        new("choose-response", "Listen and Choose a Response", "Choose the most appropriate reply to a short spoken prompt.",
            "Listen to a question or statement. The spoken prompt is not displayed on screen. Select the best response from four written options.",
            ["Everyday expressions", "Implied meaning", "Socially appropriate responses", "Intonation and stress"]),
        new("conversation", "Listen to a Conversation", "Follow an exchange between two speakers.",
            "Listen to two people in an everyday or campus setting. Answer questions about the main idea, details, speaker purpose, and likely next actions.",
            ["Main ideas and details", "Speaker purpose", "Inferences", "Connections between speaker turns"]),
        new("announcement", "Listen to an Announcement", "Identify important information in a short announcement.",
            "Listen to an announcement in an academic setting. Pay attention to its purpose, key information, implied meaning, and what listeners should do next.",
            ["Purpose", "Key details", "Inferences", "Likely next actions"]),
        new("academic-talk", "Listen to an Academic Talk", "Connect the ideas in a short academic presentation.",
            "Listen to an academic talk of 175–250 words. No prior knowledge of the topic is required. Base your answers on what you hear.",
            ["Main ideas and support", "Organization", "Inferences", "Vocabulary in context"])
    ];
    public static string Source => TestFormat2026.Manual;
}

// Explicit learner projection prevents accidental transcript/key serialization.
public sealed record ListeningQuestionDto(int Id, string Prompt, ListeningOption[] Options);
public sealed record ListeningActivityDto(Guid Id, int Version, ListeningTaskKind Kind, string Title,
    ListeningAudio Audio, ListeningQuestionDto[] Questions)
{
    /// <summary>Projects published Listening content without exposing answer keys.</summary>
    public static ListeningActivityDto FromPublished(ListeningActivity activity)
    {
        if (!activity.IsPublished || activity.Audio is null)
            throw new InvalidOperationException("Listening content is not available to learners.");
        return new(activity.Id, activity.Version, activity.Kind, activity.Title, activity.Audio,
            activity.Questions.Select(q => new ListeningQuestionDto(q.Id,
                activity.Kind == ListeningTaskKind.ChooseResponse ? "Choose the best response." : q.Prompt,
                q.Options.ToArray())).ToArray());
    }
}

using ToeflExam.Domain.Content;

namespace ToeflExam.Application.Content.GetBuildSentenceQuestion;

/// <summary>
/// Projects a question into student-facing data. Before exposing this through HTTP,
/// add session/path access checks at the application boundary.
/// This lookup is for new presentation, not resuming a pinned historical session.
/// </summary>
public sealed class GetBuildSentenceQuestionHandler
{
    private readonly IBuildSentenceQuestionReader _reader;

    /// <summary>Initializes the get build sentence question handler with its required dependencies or validated state.</summary>
    public GetBuildSentenceQuestionHandler(IBuildSentenceQuestionReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        _reader = reader;
    }

    /// <summary>Retrieves a published question and maps only student-visible fields.</summary>
    public async Task<BuildSentenceQuestionDto?> HandleAsync(
        Guid questionId, CancellationToken cancellationToken = default)
    {
        if (questionId == Guid.Empty)
        {
            throw new ArgumentException("A question identifier is required.", nameof(questionId));
        }

        cancellationToken.ThrowIfCancellationRequested();
        var question = await _reader.FindAsync(questionId, cancellationToken);
        if (question is null || question.PublicationStatus != PublicationStatus.Published)
        {
            return null;
        }

        return new BuildSentenceQuestionDto(
            question.Id,
            question.Prompt,
            Array.AsReadOnly(question.Tiles.Select(tile => new SentenceTileDto(tile.Id, tile.Text)).ToArray()),
            Array.AsReadOnly(question.ResponseParts.Select(part => new SentencePartDto(part.IsSlot, part.Text)).ToArray()),
            question.SlotCount);
    }
}

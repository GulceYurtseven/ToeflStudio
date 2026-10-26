using ToeflExam.Domain.Content;

namespace ToeflExam.Application.Content;

/// <summary>
/// Read boundary for question definitions. Implemented by Infrastructure later.
/// This internal lookup does not perform student/session authorization.
/// </summary>
public interface IBuildSentenceQuestionReader
{
    Task<BuildSentenceQuestion?> FindAsync(Guid questionId, CancellationToken cancellationToken);
}

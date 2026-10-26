namespace ToeflExam.Infrastructure.Persistence.Entities;

/// <summary>One durable, compressed report per student and exam form; the next completed run replaces it.</summary>
public sealed class LatestExamReport
{
    public string UserId { get; set; } = "";
    public string Form { get; set; } = "";
    public Guid SessionId { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset CompletedAt { get; set; }
    public byte[] Payload { get; set; } = [];
    public byte[] Version { get; set; } = [];
}

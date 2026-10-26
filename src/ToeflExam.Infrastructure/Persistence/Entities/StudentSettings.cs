namespace ToeflExam.Infrastructure.Persistence.Entities;
public sealed class StudentSettings
{
    public string UserId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public int DailyPracticeMinutes { get; set; } = 20;
    public DateOnly? TargetTestDate { get; set; }
}

namespace ToeflExam.Application.ContentBank;

public sealed record StudyPriority(string Skill, string Evidence, int[] Positions, string Exercise, string SuccessCheck);
public sealed record SectionStudyPlan(string Section, string Summary, StudyPriority[] Priorities);
public sealed record ExamStudyPlan(SectionStudyPlan[] Sections);
public interface IExamCoach
{
    Task<ExamStudyPlan> CreateAsync(ExamReport report, CancellationToken ct);
}

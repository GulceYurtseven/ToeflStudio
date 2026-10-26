using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ToeflExam.Application.Writing;
using ToeflExam.Domain.Content;
using ToeflExam.Domain.Writing;
using ToeflExam.Infrastructure.Persistence.Entities;

namespace ToeflExam.Infrastructure.Persistence;

// Original practice material. Published versions are frozen; add new versions to expand the curriculum.
internal static class WritingSeed
{
    /// <summary>Defines the model mapping, constraints or deterministic seed data for this content.</summary>
    public static void Configure(ModelBuilder b)
    {
        foreach (var kind in Enum.GetValues<WritingTaskKind>())
            b.Entity<WritingPathVersion>().HasData(new { Id = PathId(kind), Kind = kind, Version = 1,
                Title = kind == WritingTaskKind.Email ? "Write an Email — Foundation" : "Academic Discussion — Foundation", IsPublished = true });

        Email(1, "Request a study room",
            "You organize a study group of six students. Your usual room is unavailable next Thursday. Write to the university library coordinator, Ms. Chen.",
            ["Explain why your group needs a room.", "Describe the time and equipment you need.", "Ask about booking an alternative room."]);
        Email(2, "Resolve a timetable conflict",
            "A required laboratory session overlaps with a workshop you registered for. Write to the workshop organizer, Mr. Rivera.",
            ["Explain the scheduling conflict.", "Explain why attending the workshop matters to you.", "Suggest a practical alternative and ask whether it is possible."]);
        Email(3, "Report a housing problem",
            "The shared kitchen in your university residence has had a broken refrigerator for two days. Write to the housing manager, Ms. Ali.",
            ["Describe the problem and its impact.", "Explain what residents have already tried.", "Request a repair and suggest a temporary solution."]);
        Email(4, "Improve a campus event",
            "You attended a university careers event. The talks were helpful, but students had little opportunity to ask questions. Write to the event coordinator, Dr. Brooks.",
            ["Mention something you appreciated.", "Explain the difficulty you experienced.", "Propose a specific improvement for the next event."]);
        Email(5, "Organize a volunteer project",
            "Your student club wants to clean a nearby park with a community organization. Write to its director, Ms. Patel.",
            ["Introduce the club and explain your goal.", "Propose a date and describe what volunteers can contribute.", "Ask what arrangements or permissions are needed."]);

        Discussion(1, "Recorded university lectures",
            "Professor Allen: Some universities record every lecture, while others prioritize in-person attendance. Should recordings be available to all students? Explain your position.",
            "Maya", "Recordings help students review difficult topics and allow students who are ill to catch up.",
            "Leo", "Students may stop attending classes if recordings are always available, reducing participation.");
        Discussion(2, "A car-free city center",
            "Professor Kim: A city is considering limiting private cars in its center. Would this policy improve life for residents? Explain your view.",
            "Nora", "Fewer cars would make streets quieter and safer for people walking or cycling.",
            "Omar", "Some residents cannot easily use public transportation, and local businesses need deliveries.");
        Discussion(3, "Group projects at university",
            "Professor Singh: Should university courses assess students mainly through group projects or individual assignments? Explain your preference.",
            "Ella", "Group projects teach collaboration and expose students to different approaches.",
            "Mateo", "Individual assignments make it easier to recognize each student's effort and understanding.");
        Discussion(4, "Funding public libraries",
            "Professor Davis: A town has funding for either more digital library services or longer opening hours at existing libraries. Which should it prioritize?",
            "Aisha", "Digital access reaches people who live far away and lets them study whenever they want.",
            "Ben", "Longer opening hours help people who need quiet space, reliable internet, or personal assistance.");
        Discussion(5, "Learning practical skills",
            "Professor Rossi: Some people believe universities should require a practical life-skills course, such as personal budgeting. Should such a course be compulsory?",
            "Sofia", "A required course could help every student handle common responsibilities after graduation.",
            "Daniel", "Degree programs are already demanding, so students should choose optional workshops instead.");

        void Email(int n, string title, string context, string[] requirements)
            => Add(WritingTaskKind.Email, n, title, "Write an email in English. Address all three points and use an appropriate tone for the recipient.", context, requirements, []);
        void Discussion(int n, string title, string context, string a, string aText, string c, string cText)
            => Add(WritingTaskKind.AcademicDiscussion, n, title,
                "Write a contribution to the discussion in English. State and support your opinion in your own words. Aim for at least 100 words; length alone does not determine the score.",
                context, ["State your position on the professor's question.", "Develop your reasoning with a relevant explanation or example.", "Contribute your own ideas to the discussion."],
                [new(a, aText), new(c, cText)]);
        void Add(WritingTaskKind kind, int n, string title, string instructions, string context, string[] requirements, DiscussionPost[] posts)
        {
            var id = Guid.Parse($"c{(int)kind}200000-0000-4000-8000-{n:D12}");
            b.Entity<WritingQuestionVersion>().HasData(new { Id = id, QuestionId = id, Version = 1, Kind = kind,
                PublicationStatus = PublicationStatus.Published, Title = title, Instructions = instructions, Context = context,
                RequirementsJson = JsonSerializer.Serialize(requirements), PostsJson = JsonSerializer.Serialize(posts) });
            b.Entity<WritingPathStep>().HasData(new { PathId = PathId(kind), Position = n, QuestionId = id });
        }
    }
    /// <summary>Returns the stable seed path identifier for the Writing task kind.</summary>
    private static Guid PathId(WritingTaskKind kind) => Guid.Parse($"c{(int)kind}100000-0000-4000-8000-000000000001");
}

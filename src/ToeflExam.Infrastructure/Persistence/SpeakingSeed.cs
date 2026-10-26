using Microsoft.EntityFrameworkCore;
using ToeflExam.Domain.Content;
using ToeflExam.Domain.Speaking;
using ToeflExam.Infrastructure.Persistence.Entities;

namespace ToeflExam.Infrastructure.Persistence;

// Original practice content and local synthetic speech, not ETS recordings.
internal static class SpeakingSeed
{
    /// <summary>Defines the model mapping, constraints or deterministic seed data for this content.</summary>
    public static void Configure(ModelBuilder b)
    {
        Add(SpeakingTaskKind.ListenRepeat, "Campus learning center", "You are helping a visitor explore the campus learning center. Listen to each sentence, then repeat exactly what you heard. Speak clearly when recording begins.",
            ["Welcome to the learning center.", "The information desk is on your left.",
             "You can borrow a laptop with your student card.", "Please keep your voice low near the individual study rooms.",
             "The computer workshop begins upstairs at ten tomorrow morning.",
             "If you need help with a research project, our staff can arrange a short appointment.",
             "Before leaving the building, make sure you return any borrowed equipment to the information desk."],
            [8, 8, 10, 10, 10, 12, 12]);
        Add(SpeakingTaskKind.Interview, "Learning new skills", "You are taking part in a research interview about learning new skills. Listen to each question, then respond in English with relevant details and examples.",
            ["Think about a skill you learned recently. What was it, and how did you start learning it?",
             "Some people prefer to learn a skill with a teacher, while others prefer to learn on their own. Which do you prefer, and why?",
             "When learning a difficult skill, is it more helpful to practice a little every day or for a longer time once a week? Explain your opinion.",
             "Do you think universities should give students more opportunities to learn practical skills outside their main subject? Why or why not?"],
            [45, 45, 45, 45]);

        void Add(SpeakingTaskKind kind, string context, string instructions, string[] prompts, int[] seconds)
        {
            var pathId = Guid.Parse($"e{(int)kind}100000-0000-4000-8000-000000000001");
            b.Entity<SpeakingPathVersion>().HasData(new { Id = pathId, Kind = kind, Version = 1,
                Title = context, IsPublished = true });
            for (var index = 0; index < prompts.Length; index++)
            {
                var id = Guid.Parse($"e{(int)kind}200000-0000-4000-8000-{index + 1:D12}");
                b.Entity<SpeakingQuestionVersion>().HasData(new SpeakingQuestionVersion
                {
                    Id = id, QuestionId = id, Version = 1, Kind = kind, PublicationStatus = PublicationStatus.Published,
                    Title = context + " · " + (index + 1), Context = context, Instructions = instructions,
                    Prompt = prompts[index], AudioFile = $"{(int)kind}-{index + 1}-v1.wav",
                    ResponseSeconds = seconds[index], ScenePosition = index + 1
                });
                b.Entity<SpeakingPathStep>().HasData(new { PathId = pathId, Position = index + 1, QuestionId = id });
            }
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ToeflExam.Infrastructure.Persistence;
using ToeflExam.Application.Content;
using ToeflExam.Application.Content.GetBuildSentenceQuestion;
using ToeflExam.Application.Learning.BuildSentence;
using ToeflExam.Application.Writing;
using ToeflExam.Infrastructure.Assessment;

namespace ToeflExam.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registers SQL stores, bounded report caches and AI transports with their required lifetimes.</summary>
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<ToeflExamDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("ToeflExam");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("ConnectionStrings:ToeflExam must be configured before using persistence.");
            }

            options.UseSqlServer(connectionString, sql => sql.CommandTimeout(30));
        });

        services.AddScoped<SqlBuildSentencePracticeStore>();
        services.AddScoped<IBuildSentenceQuestionReader>(provider => provider.GetRequiredService<SqlBuildSentencePracticeStore>());
        services.AddScoped<IBuildSentencePracticeStore>(provider => provider.GetRequiredService<SqlBuildSentencePracticeStore>());
        services.AddScoped<GetBuildSentenceQuestionHandler>();
        services.AddScoped<BuildSentencePracticeService>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<QuestionAudioFiles>();
        services.AddSingleton<ToeflExam.Application.Vocabulary.IVocabularyCatalog, ToeflExam.Infrastructure.Vocabulary.EmbeddedVocabularyCatalog>();
        services.AddSingleton<ToeflExam.Application.ContentBank.IContentBank, ToeflExam.Infrastructure.ContentBank.EmbeddedContentBank>();
        services.AddSingleton<VolatileExamStore>();
        services.AddScoped<ToeflExam.Application.ContentBank.IBankSessionService, BankSessionService>();
        services.AddScoped<IWritingStore, SqlWritingStore>();
        services.AddScoped<WritingPracticeService>();
        services.AddScoped<ToeflExam.Application.Reading.IReadingStore, SqlReadingStore>();
        services.AddScoped<ToeflExam.Application.Reading.ReadingPracticeService>();
        services.AddSingleton<IWritingReportCache, MemoryWritingReportCache>();
        services.AddScoped<ToeflExam.Application.Speaking.ISpeakingStore, SqlSpeakingStore>();
        services.AddScoped<ToeflExam.Application.Speaking.SpeakingPracticeService>();
        services.AddSingleton<ToeflExam.Application.Speaking.ISpeakingReportCache, MemorySpeakingReportCache>();
        services.AddHttpClient<ToeflExam.Application.Speaking.ISpeechTranscriber, GeminiSpeechTranscriber>(client => client.Timeout = Timeout.InfiniteTimeSpan)
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect = false });
        services.AddHttpClient<ToeflExam.Application.Speaking.ISpeakingScorer, GeminiSpeakingScorer>(client => client.Timeout = Timeout.InfiniteTimeSpan)
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect = false });
        services.AddHttpClient<ToeflExam.Application.ContentBank.IExamCoach, GeminiExamCoach>(client => client.Timeout = Timeout.InfiniteTimeSpan)
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect = false });
        services.Configure<GeminiOptions>(configuration.GetSection("Gemini"));
        services.AddHttpClient<IWritingScorer, GeminiWritingScorer>(client => client.Timeout = Timeout.InfiniteTimeSpan)
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect = false });
        return services;
    }
}

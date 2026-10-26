using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ToeflExam.Infrastructure.Persistence;

public sealed class ToeflExamDbContextFactory : IDesignTimeDbContextFactory<ToeflExamDbContext>
{
    /// <summary>Builds a design-time SQL context from an explicit connection string.</summary>
    public ToeflExamDbContext CreateDbContext(string[] args)
    {
        // Explicit configuration prevents tooling from silently targeting the wrong database.
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__ToeflExam");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Set ConnectionStrings__ToeflExam before running EF tools.");
        }

        var options = new DbContextOptionsBuilder<ToeflExamDbContext>().UseSqlServer(connectionString).Options;
        return new ToeflExamDbContext(options);
    }
}

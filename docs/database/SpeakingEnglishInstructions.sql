BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009212610_SpeakingEnglishInstructions'
)
BEGIN
    EXEC(N'UPDATE [content].[SpeakingQuestionVersions] SET [Instructions] = N''You are helping a visitor explore the campus learning center. Listen to each sentence, then repeat exactly what you heard. Speak clearly when recording begins.''
    WHERE [Id] = ''e1200000-0000-4000-8000-000000000001'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009212610_SpeakingEnglishInstructions'
)
BEGIN
    EXEC(N'UPDATE [content].[SpeakingQuestionVersions] SET [Instructions] = N''You are helping a visitor explore the campus learning center. Listen to each sentence, then repeat exactly what you heard. Speak clearly when recording begins.''
    WHERE [Id] = ''e1200000-0000-4000-8000-000000000002'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009212610_SpeakingEnglishInstructions'
)
BEGIN
    EXEC(N'UPDATE [content].[SpeakingQuestionVersions] SET [Instructions] = N''You are helping a visitor explore the campus learning center. Listen to each sentence, then repeat exactly what you heard. Speak clearly when recording begins.''
    WHERE [Id] = ''e1200000-0000-4000-8000-000000000003'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009212610_SpeakingEnglishInstructions'
)
BEGIN
    EXEC(N'UPDATE [content].[SpeakingQuestionVersions] SET [Instructions] = N''You are helping a visitor explore the campus learning center. Listen to each sentence, then repeat exactly what you heard. Speak clearly when recording begins.''
    WHERE [Id] = ''e1200000-0000-4000-8000-000000000004'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009212610_SpeakingEnglishInstructions'
)
BEGIN
    EXEC(N'UPDATE [content].[SpeakingQuestionVersions] SET [Instructions] = N''You are helping a visitor explore the campus learning center. Listen to each sentence, then repeat exactly what you heard. Speak clearly when recording begins.''
    WHERE [Id] = ''e1200000-0000-4000-8000-000000000005'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009212610_SpeakingEnglishInstructions'
)
BEGIN
    EXEC(N'UPDATE [content].[SpeakingQuestionVersions] SET [Instructions] = N''You are helping a visitor explore the campus learning center. Listen to each sentence, then repeat exactly what you heard. Speak clearly when recording begins.''
    WHERE [Id] = ''e1200000-0000-4000-8000-000000000006'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009212610_SpeakingEnglishInstructions'
)
BEGIN
    EXEC(N'UPDATE [content].[SpeakingQuestionVersions] SET [Instructions] = N''You are helping a visitor explore the campus learning center. Listen to each sentence, then repeat exactly what you heard. Speak clearly when recording begins.''
    WHERE [Id] = ''e1200000-0000-4000-8000-000000000007'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009212610_SpeakingEnglishInstructions'
)
BEGIN
    EXEC(N'UPDATE [content].[SpeakingQuestionVersions] SET [Instructions] = N''You are taking part in a research interview about learning new skills. Listen to each question, then respond in English with relevant details and examples.''
    WHERE [Id] = ''e2200000-0000-4000-8000-000000000001'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009212610_SpeakingEnglishInstructions'
)
BEGIN
    EXEC(N'UPDATE [content].[SpeakingQuestionVersions] SET [Instructions] = N''You are taking part in a research interview about learning new skills. Listen to each question, then respond in English with relevant details and examples.''
    WHERE [Id] = ''e2200000-0000-4000-8000-000000000002'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009212610_SpeakingEnglishInstructions'
)
BEGIN
    EXEC(N'UPDATE [content].[SpeakingQuestionVersions] SET [Instructions] = N''You are taking part in a research interview about learning new skills. Listen to each question, then respond in English with relevant details and examples.''
    WHERE [Id] = ''e2200000-0000-4000-8000-000000000003'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009212610_SpeakingEnglishInstructions'
)
BEGIN
    EXEC(N'UPDATE [content].[SpeakingQuestionVersions] SET [Instructions] = N''You are taking part in a research interview about learning new skills. Listen to each question, then respond in English with relevant details and examples.''
    WHERE [Id] = ''e2200000-0000-4000-8000-000000000004'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009212610_SpeakingEnglishInstructions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261009212610_SpeakingEnglishInstructions', N'10.0.12');
END;

COMMIT;
GO


IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929221250_InitialQuestionContent'
)
BEGIN
    IF SCHEMA_ID(N'content') IS NULL EXEC(N'CREATE SCHEMA [content];');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929221250_InitialQuestionContent'
)
BEGIN
    CREATE TABLE [content].[BuildSentenceQuestionVersions] (
        [Id] uniqueidentifier NOT NULL,
        [QuestionId] uniqueidentifier NOT NULL,
        [VersionNumber] int NOT NULL,
        [Prompt] nvarchar(4000) NOT NULL,
        [PublicationStatus] int NOT NULL,
        [TilesJson] nvarchar(max) NOT NULL,
        [ResponsePartsJson] nvarchar(max) NOT NULL,
        [AcceptedOrdersJson] nvarchar(max) NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_BuildSentenceQuestionVersions] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_BuildSentenceQuestionVersions_AcceptedOrdersJson] CHECK (ISJSON([AcceptedOrdersJson]) = 1 AND LEFT(LTRIM([AcceptedOrdersJson]), 1) = '['),
        CONSTRAINT [CK_BuildSentenceQuestionVersions_ResponsePartsJson] CHECK (ISJSON([ResponsePartsJson]) = 1 AND LEFT(LTRIM([ResponsePartsJson]), 1) = '['),
        CONSTRAINT [CK_BuildSentenceQuestionVersions_Status] CHECK ([PublicationStatus] IN (0, 1, 2, 3)),
        CONSTRAINT [CK_BuildSentenceQuestionVersions_TilesJson] CHECK (ISJSON([TilesJson]) = 1 AND LEFT(LTRIM([TilesJson]), 1) = '['),
        CONSTRAINT [CK_BuildSentenceQuestionVersions_Version] CHECK ([VersionNumber] > 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929221250_InitialQuestionContent'
)
BEGIN
    CREATE INDEX [IX_BuildSentenceQuestionVersions_PublicationStatus] ON [content].[BuildSentenceQuestionVersions] ([PublicationStatus]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929221250_InitialQuestionContent'
)
BEGIN
    CREATE UNIQUE INDEX [IX_BuildSentenceQuestionVersions_QuestionId_VersionNumber] ON [content].[BuildSentenceQuestionVersions] ([QuestionId], [VersionNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929221250_InitialQuestionContent'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260929221250_InitialQuestionContent', N'10.0.12');
END;

COMMIT;
GO


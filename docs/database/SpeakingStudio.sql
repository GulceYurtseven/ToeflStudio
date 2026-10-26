BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007104353_SpeakingStudio'
)
BEGIN
    CREATE TABLE [learning].[SpeakingPaths] (
        [Id] uniqueidentifier NOT NULL,
        [Kind] int NOT NULL,
        [Version] int NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [IsPublished] bit NOT NULL,
        CONSTRAINT [PK_SpeakingPaths] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_SpeakingPath_KindVersion] CHECK ([Kind] IN (1,2) AND [Version] > 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007104353_SpeakingStudio'
)
BEGIN
    CREATE TABLE [content].[SpeakingQuestionVersions] (
        [Id] uniqueidentifier NOT NULL,
        [QuestionId] uniqueidentifier NOT NULL,
        [Version] int NOT NULL,
        [Kind] int NOT NULL,
        [PublicationStatus] int NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Instructions] nvarchar(2000) NOT NULL,
        [Context] nvarchar(max) NOT NULL,
        [Prompt] nvarchar(2000) NOT NULL,
        [AudioFile] nvarchar(100) NOT NULL,
        [ResponseSeconds] int NOT NULL,
        [ScenePosition] int NOT NULL,
        CONSTRAINT [PK_SpeakingQuestionVersions] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_SpeakingQuestion_Kind] CHECK ([Kind] IN (1,2)),
        CONSTRAINT [CK_SpeakingQuestion_Status] CHECK ([PublicationStatus] BETWEEN 0 AND 3),
        CONSTRAINT [CK_SpeakingQuestion_Timing] CHECK ([ResponseSeconds] BETWEEN 1 AND 45 AND [ScenePosition] BETWEEN 1 AND 7),
        CONSTRAINT [CK_SpeakingQuestion_Version] CHECK ([Version] > 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007104353_SpeakingStudio'
)
BEGIN
    CREATE TABLE [learning].[SpeakingRuns] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        [Kind] int NOT NULL,
        [PathId] uniqueidentifier NOT NULL,
        [RestartedFromId] uniqueidentifier NULL,
        [StartedAt] datetimeoffset NOT NULL,
        [IsCompleted] bit NOT NULL,
        [Revision] int NOT NULL,
        CONSTRAINT [PK_SpeakingRuns] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SpeakingRuns_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SpeakingRuns_SpeakingPaths_PathId] FOREIGN KEY ([PathId]) REFERENCES [learning].[SpeakingPaths] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SpeakingRuns_SpeakingRuns_RestartedFromId] FOREIGN KEY ([RestartedFromId]) REFERENCES [learning].[SpeakingRuns] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007104353_SpeakingStudio'
)
BEGIN
    CREATE TABLE [learning].[SpeakingPathSteps] (
        [PathId] uniqueidentifier NOT NULL,
        [Position] int NOT NULL,
        [QuestionId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_SpeakingPathSteps] PRIMARY KEY ([PathId], [Position]),
        CONSTRAINT [FK_SpeakingPathSteps_SpeakingPaths_PathId] FOREIGN KEY ([PathId]) REFERENCES [learning].[SpeakingPaths] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SpeakingPathSteps_SpeakingQuestionVersions_QuestionId] FOREIGN KEY ([QuestionId]) REFERENCES [content].[SpeakingQuestionVersions] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007104353_SpeakingStudio'
)
BEGIN
    CREATE TABLE [learning].[SpeakingRunSteps] (
        [RunId] uniqueidentifier NOT NULL,
        [Position] int NOT NULL,
        [QuestionId] uniqueidentifier NOT NULL,
        [SubmissionId] uniqueidentifier NULL,
        [LeaseToken] uniqueidentifier NULL,
        [LeaseUntil] datetimeoffset NULL,
        [Score] int NULL,
        [DurationMilliseconds] int NULL,
        [Model] nvarchar(120) NULL,
        [RubricVersion] nvarchar(60) NULL,
        [CompletedAt] datetimeoffset NULL,
        CONSTRAINT [PK_SpeakingRunSteps] PRIMARY KEY ([RunId], [Position]),
        CONSTRAINT [CK_SpeakingStep_Score] CHECK ([Score] IS NULL OR ([Score] BETWEEN 0 AND 5 AND [DurationMilliseconds] >= 0)),
        CONSTRAINT [CK_SpeakingStep_State] CHECK (([Score] IS NULL AND [CompletedAt] IS NULL AND [DurationMilliseconds] IS NULL AND [Model] IS NULL AND [RubricVersion] IS NULL AND (([SubmissionId] IS NULL AND [LeaseToken] IS NULL AND [LeaseUntil] IS NULL) OR ([SubmissionId] IS NOT NULL AND [LeaseToken] IS NOT NULL AND [LeaseUntil] IS NOT NULL))) OR ([Score] IS NOT NULL AND [CompletedAt] IS NOT NULL AND [DurationMilliseconds] IS NOT NULL AND [Model] IS NOT NULL AND [RubricVersion] IS NOT NULL AND [SubmissionId] IS NOT NULL AND [LeaseToken] IS NULL AND [LeaseUntil] IS NULL)),
        CONSTRAINT [FK_SpeakingRunSteps_SpeakingQuestionVersions_QuestionId] FOREIGN KEY ([QuestionId]) REFERENCES [content].[SpeakingQuestionVersions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SpeakingRunSteps_SpeakingRuns_RunId] FOREIGN KEY ([RunId]) REFERENCES [learning].[SpeakingRuns] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007104353_SpeakingStudio'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'IsPublished', N'Kind', N'Title', N'Version') AND [object_id] = OBJECT_ID(N'[learning].[SpeakingPaths]'))
        SET IDENTITY_INSERT [learning].[SpeakingPaths] ON;
    EXEC(N'INSERT INTO [learning].[SpeakingPaths] ([Id], [IsPublished], [Kind], [Title], [Version])
    VALUES (''e1100000-0000-4000-8000-000000000001'', CAST(1 AS bit), 1, N''Campus learning center'', 1),
    (''e2100000-0000-4000-8000-000000000001'', CAST(1 AS bit), 2, N''Learning new skills'', 1)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'IsPublished', N'Kind', N'Title', N'Version') AND [object_id] = OBJECT_ID(N'[learning].[SpeakingPaths]'))
        SET IDENTITY_INSERT [learning].[SpeakingPaths] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007104353_SpeakingStudio'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AudioFile', N'Context', N'Instructions', N'Kind', N'Prompt', N'PublicationStatus', N'QuestionId', N'ResponseSeconds', N'ScenePosition', N'Title', N'Version') AND [object_id] = OBJECT_ID(N'[content].[SpeakingQuestionVersions]'))
        SET IDENTITY_INSERT [content].[SpeakingQuestionVersions] ON;
    EXEC(N'INSERT INTO [content].[SpeakingQuestionVersions] ([Id], [AudioFile], [Context], [Instructions], [Kind], [Prompt], [PublicationStatus], [QuestionId], [ResponseSeconds], [ScenePosition], [Title], [Version])
    VALUES (''e1200000-0000-4000-8000-000000000001'', N''1-1-v1.wav'', N''Campus learning center'', N''Kampüs öğrenme merkezini geziyorsun. Cümleyi dinle, ses bittikten sonra duyduğunu aynı şekilde tekrar et.'', 1, N''Welcome to the learning center.'', 2, ''e1200000-0000-4000-8000-000000000001'', 8, 1, N''Campus learning center · 1'', 1),
    (''e1200000-0000-4000-8000-000000000002'', N''1-2-v1.wav'', N''Campus learning center'', N''Kampüs öğrenme merkezini geziyorsun. Cümleyi dinle, ses bittikten sonra duyduğunu aynı şekilde tekrar et.'', 1, N''The information desk is on your left.'', 2, ''e1200000-0000-4000-8000-000000000002'', 8, 2, N''Campus learning center · 2'', 1),
    (''e1200000-0000-4000-8000-000000000003'', N''1-3-v1.wav'', N''Campus learning center'', N''Kampüs öğrenme merkezini geziyorsun. Cümleyi dinle, ses bittikten sonra duyduğunu aynı şekilde tekrar et.'', 1, N''You can borrow a laptop with your student card.'', 2, ''e1200000-0000-4000-8000-000000000003'', 10, 3, N''Campus learning center · 3'', 1),
    (''e1200000-0000-4000-8000-000000000004'', N''1-4-v1.wav'', N''Campus learning center'', N''Kampüs öğrenme merkezini geziyorsun. Cümleyi dinle, ses bittikten sonra duyduğunu aynı şekilde tekrar et.'', 1, N''Please keep your voice low near the individual study rooms.'', 2, ''e1200000-0000-4000-8000-000000000004'', 10, 4, N''Campus learning center · 4'', 1),
    (''e1200000-0000-4000-8000-000000000005'', N''1-5-v1.wav'', N''Campus learning center'', N''Kampüs öğrenme merkezini geziyorsun. Cümleyi dinle, ses bittikten sonra duyduğunu aynı şekilde tekrar et.'', 1, N''The computer workshop begins upstairs at ten tomorrow morning.'', 2, ''e1200000-0000-4000-8000-000000000005'', 10, 5, N''Campus learning center · 5'', 1),
    (''e1200000-0000-4000-8000-000000000006'', N''1-6-v1.wav'', N''Campus learning center'', N''Kampüs öğrenme merkezini geziyorsun. Cümleyi dinle, ses bittikten sonra duyduğunu aynı şekilde tekrar et.'', 1, N''If you need help with a research project, our staff can arrange a short appointment.'', 2, ''e1200000-0000-4000-8000-000000000006'', 12, 6, N''Campus learning center · 6'', 1),
    (''e1200000-0000-4000-8000-000000000007'', N''1-7-v1.wav'', N''Campus learning center'', N''Kampüs öğrenme merkezini geziyorsun. Cümleyi dinle, ses bittikten sonra duyduğunu aynı şekilde tekrar et.'', 1, N''Before leaving the building, make sure you return any borrowed equipment to the information desk.'', 2, ''e1200000-0000-4000-8000-000000000007'', 12, 7, N''Campus learning center · 7'', 1),
    (''e2200000-0000-4000-8000-000000000001'', N''2-1-v1.wav'', N''Learning new skills'', N''Yeni beceriler öğrenmek üzerine yapılan bir araştırma görüşmesine katılıyorsun. Soruyu dinle; ardından kendi deneyim ve görüşlerini İngilizce anlat.'', 2, N''Think about a skill you learned recently. What was it, and how did you start learning it?'', 2, ''e2200000-0000-4000-8000-000000000001'', 45, 1, N''Learning new skills · 1'', 1),
    (''e2200000-0000-4000-8000-000000000002'', N''2-2-v1.wav'', N''Learning new skills'', N''Yeni beceriler öğrenmek üzerine yapılan bir araştırma görüşmesine katılıyorsun. Soruyu dinle; ardından kendi deneyim ve görüşlerini İngilizce anlat.'', 2, N''Some people prefer to learn a skill with a teacher, while others prefer to learn on their own. Which do you prefer, and why?'', 2, ''e2200000-0000-4000-8000-000000000002'', 45, 2, N''Learning new skills · 2'', 1),
    (''e2200000-0000-4000-8000-000000000003'', N''2-3-v1.wav'', N''Learning new skills'', N''Yeni beceriler öğrenmek üzerine yapılan bir araştırma görüşmesine katılıyorsun. Soruyu dinle; ardından kendi deneyim ve görüşlerini İngilizce anlat.'', 2, N''When learning a difficult skill, is it more helpful to practice a little every day or for a longer time once a week? Explain your opinion.'', 2, ''e2200000-0000-4000-8000-000000000003'', 45, 3, N''Learning new skills · 3'', 1),
    (''e2200000-0000-4000-8000-000000000004'', N''2-4-v1.wav'', N''Learning new skills'', N''Yeni beceriler öğrenmek üzerine yapılan bir araştırma görüşmesine katılıyorsun. Soruyu dinle; ardından kendi deneyim ve görüşlerini İngilizce anlat.'', 2, N''Do you think universities should give students more opportunities to learn practical skills outside their main subject? Why or why not?'', 2, ''e2200000-0000-4000-8000-000000000004'', 45, 4, N''Learning new skills · 4'', 1)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AudioFile', N'Context', N'Instructions', N'Kind', N'Prompt', N'PublicationStatus', N'QuestionId', N'ResponseSeconds', N'ScenePosition', N'Title', N'Version') AND [object_id] = OBJECT_ID(N'[content].[SpeakingQuestionVersions]'))
        SET IDENTITY_INSERT [content].[SpeakingQuestionVersions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007104353_SpeakingStudio'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PathId', N'Position', N'QuestionId') AND [object_id] = OBJECT_ID(N'[learning].[SpeakingPathSteps]'))
        SET IDENTITY_INSERT [learning].[SpeakingPathSteps] ON;
    EXEC(N'INSERT INTO [learning].[SpeakingPathSteps] ([PathId], [Position], [QuestionId])
    VALUES (''e1100000-0000-4000-8000-000000000001'', 1, ''e1200000-0000-4000-8000-000000000001''),
    (''e1100000-0000-4000-8000-000000000001'', 2, ''e1200000-0000-4000-8000-000000000002''),
    (''e1100000-0000-4000-8000-000000000001'', 3, ''e1200000-0000-4000-8000-000000000003''),
    (''e1100000-0000-4000-8000-000000000001'', 4, ''e1200000-0000-4000-8000-000000000004''),
    (''e1100000-0000-4000-8000-000000000001'', 5, ''e1200000-0000-4000-8000-000000000005''),
    (''e1100000-0000-4000-8000-000000000001'', 6, ''e1200000-0000-4000-8000-000000000006''),
    (''e1100000-0000-4000-8000-000000000001'', 7, ''e1200000-0000-4000-8000-000000000007''),
    (''e2100000-0000-4000-8000-000000000001'', 1, ''e2200000-0000-4000-8000-000000000001''),
    (''e2100000-0000-4000-8000-000000000001'', 2, ''e2200000-0000-4000-8000-000000000002''),
    (''e2100000-0000-4000-8000-000000000001'', 3, ''e2200000-0000-4000-8000-000000000003''),
    (''e2100000-0000-4000-8000-000000000001'', 4, ''e2200000-0000-4000-8000-000000000004'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PathId', N'Position', N'QuestionId') AND [object_id] = OBJECT_ID(N'[learning].[SpeakingPathSteps]'))
        SET IDENTITY_INSERT [learning].[SpeakingPathSteps] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007104353_SpeakingStudio'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SpeakingPaths_Kind_Version] ON [learning].[SpeakingPaths] ([Kind], [Version]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007104353_SpeakingStudio'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SpeakingPathSteps_PathId_QuestionId] ON [learning].[SpeakingPathSteps] ([PathId], [QuestionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007104353_SpeakingStudio'
)
BEGIN
    CREATE INDEX [IX_SpeakingPathSteps_QuestionId] ON [learning].[SpeakingPathSteps] ([QuestionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007104353_SpeakingStudio'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SpeakingQuestionVersions_QuestionId_Version] ON [content].[SpeakingQuestionVersions] ([QuestionId], [Version]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007104353_SpeakingStudio'
)
BEGIN
    CREATE INDEX [IX_SpeakingRuns_PathId] ON [learning].[SpeakingRuns] ([PathId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007104353_SpeakingStudio'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_SpeakingRuns_RestartedFromId] ON [learning].[SpeakingRuns] ([RestartedFromId]) WHERE [RestartedFromId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007104353_SpeakingStudio'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_SpeakingRuns_UserId_Kind] ON [learning].[SpeakingRuns] ([UserId], [Kind]) WHERE [IsCompleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007104353_SpeakingStudio'
)
BEGIN
    CREATE INDEX [IX_SpeakingRunSteps_QuestionId] ON [learning].[SpeakingRunSteps] ([QuestionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007104353_SpeakingStudio'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_SpeakingRunSteps_RunId_SubmissionId] ON [learning].[SpeakingRunSteps] ([RunId], [SubmissionId]) WHERE [SubmissionId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007104353_SpeakingStudio'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261007104353_SpeakingStudio', N'10.0.12');
END;

COMMIT;
GO


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

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006140919_BuildSentencePractice'
)
BEGIN
    IF SCHEMA_ID(N'learning') IS NULL EXEC(N'CREATE SCHEMA [learning];');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006140919_BuildSentencePractice'
)
BEGIN
    ALTER TABLE [content].[BuildSentenceQuestionVersions] ADD [Explanation] nvarchar(4000) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006140919_BuildSentencePractice'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006140919_BuildSentencePractice'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006140919_BuildSentencePractice'
)
BEGIN
    CREATE TABLE [learning].[BuildSentencePaths] (
        [Id] uniqueidentifier NOT NULL,
        [Version] int NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [IsPublished] bit NOT NULL,
        CONSTRAINT [PK_BuildSentencePaths] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006140919_BuildSentencePractice'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006140919_BuildSentencePractice'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006140919_BuildSentencePractice'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006140919_BuildSentencePractice'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006140919_BuildSentencePractice'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006140919_BuildSentencePractice'
)
BEGIN
    CREATE TABLE [learning].[BuildSentencePathSteps] (
        [PathVersionId] uniqueidentifier NOT NULL,
        [Position] int NOT NULL,
        [QuestionVersionId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_BuildSentencePathSteps] PRIMARY KEY ([PathVersionId], [Position]),
        CONSTRAINT [FK_BuildSentencePathSteps_BuildSentencePaths_PathVersionId] FOREIGN KEY ([PathVersionId]) REFERENCES [learning].[BuildSentencePaths] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_BuildSentencePathSteps_BuildSentenceQuestionVersions_QuestionVersionId] FOREIGN KEY ([QuestionVersionId]) REFERENCES [content].[BuildSentenceQuestionVersions] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006140919_BuildSentencePractice'
)
BEGIN
    CREATE TABLE [learning].[BuildSentenceRuns] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        [PathVersionId] uniqueidentifier NOT NULL,
        [RestartedFromId] uniqueidentifier NULL,
        [StartedAt] datetimeoffset NOT NULL,
        [CompletedAt] datetimeoffset NULL,
        [IsCompleted] bit NOT NULL,
        [Revision] int NOT NULL,
        CONSTRAINT [PK_BuildSentenceRuns] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BuildSentenceRuns_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_BuildSentenceRuns_BuildSentencePaths_PathVersionId] FOREIGN KEY ([PathVersionId]) REFERENCES [learning].[BuildSentencePaths] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_BuildSentenceRuns_BuildSentenceRuns_RestartedFromId] FOREIGN KEY ([RestartedFromId]) REFERENCES [learning].[BuildSentenceRuns] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006140919_BuildSentencePractice'
)
BEGIN
    CREATE TABLE [learning].[BuildSentenceRunSteps] (
        [RunId] uniqueidentifier NOT NULL,
        [Position] int NOT NULL,
        [QuestionVersionId] uniqueidentifier NOT NULL,
        [SubmissionId] uniqueidentifier NULL,
        [RawScore] int NULL,
        [CompletedAt] datetimeoffset NULL,
        CONSTRAINT [PK_BuildSentenceRunSteps] PRIMARY KEY ([RunId], [Position]),
        CONSTRAINT [CK_BuildSentenceRunSteps_Completion] CHECK (([RawScore] IS NULL AND [SubmissionId] IS NULL AND [CompletedAt] IS NULL) OR ([RawScore] IN (0, 1) AND [RawScore] IS NOT NULL AND [SubmissionId] IS NOT NULL AND [CompletedAt] IS NOT NULL)),
        CONSTRAINT [FK_BuildSentenceRunSteps_BuildSentenceQuestionVersions_QuestionVersionId] FOREIGN KEY ([QuestionVersionId]) REFERENCES [content].[BuildSentenceQuestionVersions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_BuildSentenceRunSteps_BuildSentenceRuns_RunId] FOREIGN KEY ([RunId]) REFERENCES [learning].[BuildSentenceRuns] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006140919_BuildSentencePractice'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'IsPublished', N'Title', N'Version') AND [object_id] = OBJECT_ID(N'[learning].[BuildSentencePaths]'))
        SET IDENTITY_INSERT [learning].[BuildSentencePaths] ON;
    EXEC(N'INSERT INTO [learning].[BuildSentencePaths] ([Id], [IsPublished], [Title], [Version])
    VALUES (''b5100000-0000-4000-8000-000000000001'', CAST(1 AS bit), N''Build a Sentence — Başlangıç'', 1)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'IsPublished', N'Title', N'Version') AND [object_id] = OBJECT_ID(N'[learning].[BuildSentencePaths]'))
        SET IDENTITY_INSERT [learning].[BuildSentencePaths] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006140919_BuildSentencePractice'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AcceptedOrdersJson', N'Explanation', N'Prompt', N'PublicationStatus', N'QuestionId', N'ResponsePartsJson', N'TilesJson', N'VersionNumber') AND [object_id] = OBJECT_ID(N'[content].[BuildSentenceQuestionVersions]'))
        SET IDENTITY_INSERT [content].[BuildSentenceQuestionVersions] ON;
    EXEC(N'INSERT INTO [content].[BuildSentenceQuestionVersions] ([Id], [AcceptedOrdersJson], [Explanation], [Prompt], [PublicationStatus], [QuestionId], [ResponsePartsJson], [TilesJson], [VersionNumber])
    VALUES (''b5200000-0000-4000-8000-000000000001'', N''[[42,87,16]]'', N''Maya üçüncü tekil şahıs olduğu için geniş zamanda ''''visits'''' kullanılır. Fiilden sonra nesne olan ''''the library'''' gelir; zaman ifadesi cümlenin başındadır.'', N''What does Maya do before class?'', 2, ''b5200000-0000-4000-8000-000000000001'', N''[{"IsSlot":false,"Text":"Before class, Maya "},{"IsSlot":true,"Text":null},{"IsSlot":true,"Text":null},{"IsSlot":true,"Text":null},{"IsSlot":false,"Text":"."}]'', N''[{"Id":87,"Text":"the"},{"Id":95,"Text":"visit"},{"Id":16,"Text":"library"},{"Id":42,"Text":"visits"}]'', 1),
    (''b5200000-0000-4000-8000-000000000002'', N''[[68,14]]'', N''Yer sorusunda ''''be'''' fiili kullanılır: Where + is + özne. ''''Does'''' tek başına bir konum belirtmez.'', N''Ask about the location of the science lab.'', 2, ''b5200000-0000-4000-8000-000000000002'', N''[{"IsSlot":false,"Text":"Where "},{"IsSlot":true,"Text":null},{"IsSlot":true,"Text":null},{"IsSlot":false,"Text":"?"}]'', N''[{"Id":14,"Text":"the science lab"},{"Id":68,"Text":"is"},{"Id":39,"Text":"does"}]'', 1),
    (''b5200000-0000-4000-8000-000000000003'', N''[[93,27,61]]'', N''Geçmişteki bir olaydan önce tamamlanan eylemi past perfect anlatır: had + past participle. ''''Has'''' çoğul özneyle uyuşmaz ve bu geçmiş zaman ilişkisini kurmaz.'', N''What happened before the lecture?'', 2, ''b5200000-0000-4000-8000-000000000003'', N''[{"IsSlot":false,"Text":"Before the lecture began, the students "},{"IsSlot":true,"Text":null},{"IsSlot":true,"Text":null},{"IsSlot":true,"Text":null},{"IsSlot":false,"Text":"."}]'', N''[{"Id":45,"Text":"has"},{"Id":27,"Text":"finished"},{"Id":61,"Text":"their homework"},{"Id":93,"Text":"had"}]'', 1),
    (''b5200000-0000-4000-8000-000000000004'', N''[[31,82,47]]'', N''Dolaylı soruda yardımcı fiil ile özne yer değiştirmez: ''''when the assignment is due''''. ''''Could you tell me'''' nazik bir sorudur.'', N''Ask politely about the deadline.'', 2, ''b5200000-0000-4000-8000-000000000004'', N''[{"IsSlot":false,"Text":"Could you "},{"IsSlot":true,"Text":null},{"IsSlot":true,"Text":null},{"IsSlot":true,"Text":null},{"IsSlot":false,"Text":"?"}]'', N''[{"Id":19,"Text":"is the assignment due"},{"Id":31,"Text":"tell me"},{"Id":82,"Text":"when"},{"Id":47,"Text":"the assignment is due"}]'', 1),
    (''b5200000-0000-4000-8000-000000000005'', N''[[76,24,59]]'', N''''''That I borrowed from the library'''' kitabı tanımlayan relative clause''''dur. Bu yapıda ''''what'''' yerine ''''that'''' kullanılır.'', N''Describe the book you borrowed.'', 2, ''b5200000-0000-4000-8000-000000000005'', N''[{"IsSlot":false,"Text":"The book "},{"IsSlot":true,"Text":null},{"IsSlot":true,"Text":null},{"IsSlot":true,"Text":null},{"IsSlot":false,"Text":" from the library was very useful."}]'', N''[{"Id":24,"Text":"I"},{"Id":59,"Text":"borrowed"},{"Id":76,"Text":"that"},{"Id":83,"Text":"what"}]'', 1),
    (''b5200000-0000-4000-8000-000000000006'', N''[[18,64,91]]'', N''Birinci koşul yapısında if cümlesi geniş zaman, sonuç cümlesi will + fiil kullanır.'', N''What will you do if the weather improves?'', 2, ''b5200000-0000-4000-8000-000000000006'', N''[{"IsSlot":false,"Text":"If "},{"IsSlot":true,"Text":null},{"IsSlot":true,"Text":null},{"IsSlot":true,"Text":null},{"IsSlot":false,"Text":"."}]'', N''[{"Id":64,"Text":"we will have"},{"Id":91,"Text":"the meeting outside"},{"Id":18,"Text":"the weather improves,"},{"Id":37,"Text":"the weather will improve,"}]'', 1),
    (''b5200000-0000-4000-8000-000000000007'', N''[[54,29,73]]'', N''Tekil ''''this course'''' öznesi ''''is'''' alır. Karşılaştırmalı ''''more challenging'''' ifadesinden sonra karşılaştırılan ders ''''than'''' ile belirtilir.'', N''Compare the two courses.'', 2, ''b5200000-0000-4000-8000-000000000007'', N''[{"IsSlot":false,"Text":"This course "},{"IsSlot":true,"Text":null},{"IsSlot":true,"Text":null},{"IsSlot":true,"Text":null},{"IsSlot":false,"Text":"."}]'', N''[{"Id":11,"Text":"are"},{"Id":73,"Text":"the introductory course"},{"Id":54,"Text":"is"},{"Id":29,"Text":"more challenging than"}]'', 1),
    (''b5200000-0000-4000-8000-000000000008'', N''[[86,41,22]]'', N''''''Decide'''' fiilinden sonra to-infinitive kullanılır. Bir burs için başvurmak ''''apply for a scholarship'''' şeklindedir.'', N''Explain the student''''s decision.'', 2, ''b5200000-0000-4000-8000-000000000008'', N''[{"IsSlot":false,"Text":"She decided "},{"IsSlot":true,"Text":null},{"IsSlot":true,"Text":null},{"IsSlot":true,"Text":null},{"IsSlot":false,"Text":"."}]'', N''[{"Id":41,"Text":"apply for"},{"Id":22,"Text":"the scholarship"},{"Id":86,"Text":"to"},{"Id":67,"Text":"applying"}]'', 1),
    (''b5200000-0000-4000-8000-000000000009'', N''[[35,92,48]]'', N''Tekil özneyle geçmiş zaman edilgen yapı ''''was revised'''' olur. ''''By'''' eylemi yapan kişiyi, ''''yesterday'''' zamanı belirtir.'', N''Describe the new schedule.'', 2, ''b5200000-0000-4000-8000-000000000009'', N''[{"IsSlot":false,"Text":"Yesterday, the schedule "},{"IsSlot":true,"Text":null},{"IsSlot":true,"Text":null},{"IsSlot":true,"Text":null},{"IsSlot":false,"Text":"."}]'', N''[{"Id":92,"Text":"revised by"},{"Id":48,"Text":"the coordinator"},{"Id":35,"Text":"was"},{"Id":17,"Text":"were"}]'', 1),
    (''b5200000-0000-4000-8000-000000000010'', N''[[63,26,81]]'', N''Geçmişte başlayıp süren eylem için present perfect continuous kullanılabilir. Çoğul ''''researchers'''' öznesi ''''have been'''' alır; başlangıç yılı ''''since'''' ile belirtilir.'', N''Say how long the researchers have worked here.'', 2, ''b5200000-0000-4000-8000-000000000010'', N''[{"IsSlot":false,"Text":"Since 2021, the researchers "},{"IsSlot":true,"Text":null},{"IsSlot":true,"Text":null},{"IsSlot":true,"Text":null},{"IsSlot":false,"Text":"."}]'', N''[{"Id":44,"Text":"has been"},{"Id":63,"Text":"have been"},{"Id":81,"Text":"this university"},{"Id":26,"Text":"working at"}]'', 1)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AcceptedOrdersJson', N'Explanation', N'Prompt', N'PublicationStatus', N'QuestionId', N'ResponsePartsJson', N'TilesJson', N'VersionNumber') AND [object_id] = OBJECT_ID(N'[content].[BuildSentenceQuestionVersions]'))
        SET IDENTITY_INSERT [content].[BuildSentenceQuestionVersions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006140919_BuildSentencePractice'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PathVersionId', N'Position', N'QuestionVersionId') AND [object_id] = OBJECT_ID(N'[learning].[BuildSentencePathSteps]'))
        SET IDENTITY_INSERT [learning].[BuildSentencePathSteps] ON;
    EXEC(N'INSERT INTO [learning].[BuildSentencePathSteps] ([PathVersionId], [Position], [QuestionVersionId])
    VALUES (''b5100000-0000-4000-8000-000000000001'', 1, ''b5200000-0000-4000-8000-000000000001''),
    (''b5100000-0000-4000-8000-000000000001'', 2, ''b5200000-0000-4000-8000-000000000002''),
    (''b5100000-0000-4000-8000-000000000001'', 3, ''b5200000-0000-4000-8000-000000000003''),
    (''b5100000-0000-4000-8000-000000000001'', 4, ''b5200000-0000-4000-8000-000000000004''),
    (''b5100000-0000-4000-8000-000000000001'', 5, ''b5200000-0000-4000-8000-000000000005''),
    (''b5100000-0000-4000-8000-000000000001'', 6, ''b5200000-0000-4000-8000-000000000006''),
    (''b5100000-0000-4000-8000-000000000001'', 7, ''b5200000-0000-4000-8000-000000000007''),
    (''b5100000-0000-4000-8000-000000000001'', 8, ''b5200000-0000-4000-8000-000000000008''),
    (''b5100000-0000-4000-8000-000000000001'', 9, ''b5200000-0000-4000-8000-000000000009''),
    (''b5100000-0000-4000-8000-000000000001'', 10, ''b5200000-0000-4000-8000-000000000010'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PathVersionId', N'Position', N'QuestionVersionId') AND [object_id] = OBJECT_ID(N'[learning].[BuildSentencePathSteps]'))
        SET IDENTITY_INSERT [learning].[BuildSentencePathSteps] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006140919_BuildSentencePractice'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006140919_BuildSentencePractice'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006140919_BuildSentencePractice'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006140919_BuildSentencePractice'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006140919_BuildSentencePractice'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006140919_BuildSentencePractice'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006140919_BuildSentencePractice'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006140919_BuildSentencePractice'
)
BEGIN
    CREATE UNIQUE INDEX [IX_BuildSentencePaths_Version] ON [learning].[BuildSentencePaths] ([Version]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006140919_BuildSentencePractice'
)
BEGIN
    CREATE UNIQUE INDEX [IX_BuildSentencePathSteps_PathVersionId_QuestionVersionId] ON [learning].[BuildSentencePathSteps] ([PathVersionId], [QuestionVersionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006140919_BuildSentencePractice'
)
BEGIN
    CREATE INDEX [IX_BuildSentencePathSteps_QuestionVersionId] ON [learning].[BuildSentencePathSteps] ([QuestionVersionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006140919_BuildSentencePractice'
)
BEGIN
    CREATE INDEX [IX_BuildSentenceRuns_PathVersionId] ON [learning].[BuildSentenceRuns] ([PathVersionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006140919_BuildSentencePractice'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_BuildSentenceRuns_RestartedFromId] ON [learning].[BuildSentenceRuns] ([RestartedFromId]) WHERE [RestartedFromId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006140919_BuildSentencePractice'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_BuildSentenceRuns_UserId] ON [learning].[BuildSentenceRuns] ([UserId]) WHERE [IsCompleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006140919_BuildSentencePractice'
)
BEGIN
    CREATE INDEX [IX_BuildSentenceRunSteps_QuestionVersionId] ON [learning].[BuildSentenceRunSteps] ([QuestionVersionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006140919_BuildSentencePractice'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_BuildSentenceRunSteps_RunId_SubmissionId] ON [learning].[BuildSentenceRunSteps] ([RunId], [SubmissionId]) WHERE [SubmissionId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006140919_BuildSentencePractice'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261006140919_BuildSentencePractice', N'10.0.12');
END;

COMMIT;
GO


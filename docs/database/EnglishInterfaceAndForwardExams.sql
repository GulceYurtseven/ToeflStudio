BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011195749_EnglishInterfaceAndForwardExams'
)
BEGIN
    ALTER TABLE [learning].[BankSessionSteps] ADD [ItemIndex] int NOT NULL DEFAULT -1;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011195749_EnglishInterfaceAndForwardExams'
)
BEGIN
    CREATE TABLE [learning].[StudentSettings] (
        [UserId] nvarchar(450) NOT NULL,
        [DisplayName] nvarchar(100) NOT NULL,
        [DailyPracticeMinutes] int NOT NULL,
        [TargetTestDate] date NULL,
        CONSTRAINT [PK_StudentSettings] PRIMARY KEY ([UserId]),
        CONSTRAINT [CK_StudentSettings_Goal] CHECK ([DailyPracticeMinutes] BETWEEN 5 AND 180),
        CONSTRAINT [FK_StudentSettings_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011195749_EnglishInterfaceAndForwardExams'
)
BEGIN
    EXEC(N'UPDATE [learning].[BuildSentencePaths] SET [Title] = N''Build a Sentence — Foundation''
    WHERE [Id] = ''b5100000-0000-4000-8000-000000000001'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011195749_EnglishInterfaceAndForwardExams'
)
BEGIN
    EXEC(N'UPDATE [content].[BuildSentenceQuestionVersions] SET [Explanation] = N''Maya is third-person singular, so the present-simple verb is ''''visits''''. The object ''''the library'''' follows the verb; the time phrase comes first.''
    WHERE [Id] = ''b5200000-0000-4000-8000-000000000001'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011195749_EnglishInterfaceAndForwardExams'
)
BEGIN
    EXEC(N'UPDATE [content].[BuildSentenceQuestionVersions] SET [Explanation] = N''Use be for a location question: Where + is + subject. Does alone cannot express location.''
    WHERE [Id] = ''b5200000-0000-4000-8000-000000000002'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011195749_EnglishInterfaceAndForwardExams'
)
BEGIN
    EXEC(N'UPDATE [content].[BuildSentenceQuestionVersions] SET [Explanation] = N''Past perfect describes an action completed before another past event: had + past participle. Has does not agree with the plural subject or express this past relationship.''
    WHERE [Id] = ''b5200000-0000-4000-8000-000000000003'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011195749_EnglishInterfaceAndForwardExams'
)
BEGIN
    EXEC(N'UPDATE [content].[BuildSentenceQuestionVersions] SET [Explanation] = N''Embedded questions keep statement order: ''''when the assignment is due''''. ''''Could you tell me'''' makes the question polite.''
    WHERE [Id] = ''b5200000-0000-4000-8000-000000000004'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011195749_EnglishInterfaceAndForwardExams'
)
BEGIN
    EXEC(N'UPDATE [content].[BuildSentenceQuestionVersions] SET [Explanation] = N''''''That I borrowed from the library'''' is a relative clause describing the book. Use that, not what, after this noun.''
    WHERE [Id] = ''b5200000-0000-4000-8000-000000000005'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011195749_EnglishInterfaceAndForwardExams'
)
BEGIN
    EXEC(N'UPDATE [content].[BuildSentenceQuestionVersions] SET [Explanation] = N''A first conditional uses present simple in the if-clause and will + base verb in the result.''
    WHERE [Id] = ''b5200000-0000-4000-8000-000000000006'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011195749_EnglishInterfaceAndForwardExams'
)
BEGIN
    EXEC(N'UPDATE [content].[BuildSentenceQuestionVersions] SET [Explanation] = N''The singular subject ''''this course'''' takes is. The comparative ''''more challenging'''' is followed by than.''
    WHERE [Id] = ''b5200000-0000-4000-8000-000000000007'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011195749_EnglishInterfaceAndForwardExams'
)
BEGIN
    EXEC(N'UPDATE [content].[BuildSentenceQuestionVersions] SET [Explanation] = N''Decide takes a to-infinitive. The correct phrase is ''''apply for a scholarship''''.''
    WHERE [Id] = ''b5200000-0000-4000-8000-000000000008'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011195749_EnglishInterfaceAndForwardExams'
)
BEGIN
    EXEC(N'UPDATE [content].[BuildSentenceQuestionVersions] SET [Explanation] = N''The singular past passive is ''''was revised''''. By identifies the person responsible; yesterday gives the time.''
    WHERE [Id] = ''b5200000-0000-4000-8000-000000000009'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011195749_EnglishInterfaceAndForwardExams'
)
BEGIN
    EXEC(N'UPDATE [content].[BuildSentenceQuestionVersions] SET [Explanation] = N''Present perfect continuous describes an action continuing from the past. The plural subject researchers takes have been; since introduces the starting year.''
    WHERE [Id] = ''b5200000-0000-4000-8000-000000000010'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011195749_EnglishInterfaceAndForwardExams'
)
BEGIN
    EXEC(N'UPDATE [content].[ReadingSetVersions] SET [ItemsJson] = N''[{"Id":1,"Prompt":"Where should students collect reserved books?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"At the ground-floor desk"},{"Id":"B","Text":"At the second-floor office"},{"Id":"C","Text":"At the main entrance"},{"Id":"D","Text":"At their department"}],"Answer":"A","Explanation":"The notice explicitly changes the collection point to the ground-floor desk."},{"Id":2,"Prompt":"What should a student do if unable to collect a book on time?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"Reserve another copy"},{"Id":"B","Text":"Call after three days"},{"Id":"C","Text":"Reply before the deadline"},{"Id":"D","Text":"Visit the old office"}],"Answer":"C","Explanation":"Students must reply to the email before the deadline to request an extension."}]''
    WHERE [Id] = ''2be42540-8ebe-dd1a-629f-1c278f97d796'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011195749_EnglishInterfaceAndForwardExams'
)
BEGIN
    EXEC(N'UPDATE [content].[ReadingSetVersions] SET [ItemsJson] = N''[{"Id":1,"Prompt":"What is the passage mainly concerned with?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"Using lake sediment to investigate past environments"},{"Id":"B","Text":"Predicting the exact date of future fires"},{"Id":"C","Text":"Removing all sediment from lakes"},{"Id":"D","Text":"Showing that pollen is useless"}],"Answer":"A","Explanation":"The passage explains how layers reveal environmental history and the limits of interpretation."},{"Id":2,"Prompt":"What is generally true of deeper sediment?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"It contains no pollen"},{"Id":"B","Text":"It is always grass"},{"Id":"C","Text":"It is older"},{"Id":"D","Text":"It has no environmental value"}],"Answer":"C","Explanation":"The first paragraph states that deeper material is generally older."},{"Id":3,"Prompt":"Why might an increase in grass pollen be difficult to interpret?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"Grass never produces pollen"},{"Id":"B","Text":"More than one process could explain it"},{"Id":"C","Text":"Trees cannot grow near lakes"},{"Id":"D","Text":"All pollen has the same age"}],"Answer":"B","Explanation":"Climate change and human land clearing can produce the same pattern."},{"Id":4,"Prompt":"The word \u0027indicator\u0027 is closest in meaning to","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"instruction"},{"Id":"B","Text":"container"},{"Id":"C","Text":"obstacle"},{"Id":"D","Text":"sign"}],"Answer":"D","Explanation":"Indicator means a sign or clue about past conditions."},{"Id":5,"Prompt":"Why does the author contrast sediment with a precise diary?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"To claim scientists should use diaries only"},{"Id":"B","Text":"To emphasize that interpretation involves uncertainty"},{"Id":"C","Text":"To argue that deeper layers are always younger"},{"Id":"D","Text":"To dismiss historical records"}],"Answer":"B","Explanation":"Uncertainty about dates and causes prevents reading sediment as a precise diary."}]''
    WHERE [Id] = ''2ccdfc6f-d8ea-ca61-5ef5-538a83551ca2'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011195749_EnglishInterfaceAndForwardExams'
)
BEGIN
    EXEC(N'UPDATE [content].[ReadingSetVersions] SET [ItemsJson] = N''[{"Id":1,"Prompt":"Fill in the missing letters.","Prefix":"wor","MissingLetters":3,"Options":[],"Answer":"ker","Explanation":"worker: a singular noun following a."},{"Id":2,"Prompt":"Fill in the missing letters.","Prefix":"ho","MissingLetters":2,"Options":[],"Answer":"me","Explanation":"home: the destination after returns."},{"Id":3,"Prompt":"Fill in the missing letters.","Prefix":"fin","MissingLetters":4,"Options":[],"Answer":"ding","Explanation":"finding: an -ing form after the preposition after."},{"Id":4,"Prompt":"Fill in the missing letters.","Prefix":"a","MissingLetters":2,"Options":[],"Answer":"nd","Explanation":"and: connects two actions."},{"Id":5,"Prompt":"Fill in the missing letters.","Prefix":"move","MissingLetters":5,"Options":[],"Answer":"ments","Explanation":"movements: the object of performs."},{"Id":6,"Prompt":"Fill in the missing letters.","Prefix":"indi","MissingLetters":4,"Options":[],"Answer":"cate","Explanation":"indicate: agrees with the plural subject movements."},{"Id":7,"Prompt":"Fill in the missing letters.","Prefix":"wh","MissingLetters":3,"Options":[],"Answer":"ile","Explanation":"while: connects simultaneous actions."},{"Id":8,"Prompt":"Fill in the missing letters.","Prefix":"be","MissingLetters":2,"Options":[],"Answer":"es","Explanation":"bees: a plural noun following other."},{"Id":9,"Prompt":"Fill in the missing letters.","Prefix":"a","MissingLetters":2,"Options":[],"Answer":"nd","Explanation":"and: connects watch and receive."},{"Id":10,"Prompt":"Fill in the missing letters.","Prefix":"use","MissingLetters":3,"Options":[],"Answer":"ful","Explanation":"useful: modifies information."}]''
    WHERE [Id] = ''4b3c67e5-19f2-b5c9-a8f6-a6f1a5841156'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011195749_EnglishInterfaceAndForwardExams'
)
BEGIN
    EXEC(N'UPDATE [content].[ReadingSetVersions] SET [ItemsJson] = N''[{"Id":1,"Prompt":"What is the passage mainly about?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"Why rural areas need paved roads"},{"Id":"B","Text":"How trees cool cities and what supports their success"},{"Id":"C","Text":"Why cities should stop planting trees"},{"Id":"D","Text":"How to measure the age of a tree"}],"Answer":"B","Explanation":"The passage combines cooling mechanisms, limitations, and long-term planting design."},{"Id":2,"Prompt":"How does evaporation help cool the air?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"It prevents all rainfall"},{"Id":"B","Text":"It releases stored solar energy"},{"Id":"C","Text":"It takes heat from the air"},{"Id":"D","Text":"It increases road temperature"}],"Answer":"C","Explanation":"The first paragraph explains that evaporating water takes heat from surrounding air."},{"Id":3,"Prompt":"The word \u0027constraints\u0027 is closest in meaning to","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"celebrations"},{"Id":"B","Text":"measurements"},{"Id":"C","Text":"predictions"},{"Id":"D","Text":"limitations"}],"Answer":"D","Explanation":"The phrase refers to conditions affecting success, such as water needs and climate."},{"Id":4,"Prompt":"Why does the author mention permeable paving?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"To give an example of supporting tree survival"},{"Id":"B","Text":"To argue that shade is unnecessary"},{"Id":"C","Text":"To describe a rural farming method"},{"Id":"D","Text":"To show that roots should remain dry"}],"Answer":"A","Explanation":"Permeable surfaces illustrate planning trees as part of the surrounding system."},{"Id":5,"Prompt":"What can be inferred about counting newly planted trees?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"It always measures cooling accurately"},{"Id":"B","Text":"It is sufficient for planning"},{"Id":"C","Text":"It does not alone show long-term benefits"},{"Id":"D","Text":"It proves that trees will survive"}],"Answer":"C","Explanation":"The final paragraph emphasizes conditions for survival rather than the number planted."}]''
    WHERE [Id] = ''6315129c-ce48-f6fe-a506-d3be04758083'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011195749_EnglishInterfaceAndForwardExams'
)
BEGIN
    EXEC(N'UPDATE [content].[ReadingSetVersions] SET [ItemsJson] = N''[{"Id":1,"Prompt":"Why was this email sent?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"To cancel the club"},{"Id":"B","Text":"To explain a change of activity"},{"Id":"C","Text":"To request camera repairs"},{"Id":"D","Text":"To announce a new starting time"}],"Answer":"B","Explanation":"The weather changes the location and activity, but not the starting time."},{"Id":2,"Prompt":"What should participants bring?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"Raincoats"},{"Id":"B","Text":"New cameras"},{"Id":"C","Text":"Printed schedules"},{"Id":"D","Text":"Three existing photos"}],"Answer":"D","Explanation":"Participants are asked to bring three photos they have already taken."},{"Id":3,"Prompt":"What remains unchanged?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"The return date for cameras"},{"Id":"B","Text":"The workshop location"},{"Id":"C","Text":"The outdoor activity"},{"Id":"D","Text":"The need to take new pictures"}],"Answer":"A","Explanation":"Originally planned confirms that cameras are still due on Monday."}]''
    WHERE [Id] = ''68544981-7eb8-bc6b-9ba2-cc0fd04151d8'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011195749_EnglishInterfaceAndForwardExams'
)
BEGIN
    EXEC(N'UPDATE [content].[ReadingSetVersions] SET [ItemsJson] = N''[{"Id":1,"Prompt":"Fill in the missing letters.","Prefix":"pot","MissingLetters":4,"Options":[],"Answer":"tery","Explanation":"pottery: a collective noun modified by broken."},{"Id":2,"Prompt":"Fill in the missing letters.","Prefix":"rev","MissingLetters":4,"Options":[],"Answer":"eals","Explanation":"reveals: agrees with singular pottery."},{"Id":3,"Prompt":"Fill in the missing letters.","Prefix":"ab","MissingLetters":3,"Options":[],"Answer":"out","Explanation":"about: the pattern is reveal information about."},{"Id":4,"Prompt":"Fill in the missing letters.","Prefix":"li","MissingLetters":2,"Options":[],"Answer":"fe","Explanation":"life: part of the phrase daily life."},{"Id":5,"Prompt":"Fill in the missing letters.","Prefix":"expe","MissingLetters":5,"Options":[],"Answer":"nsive","Explanation":"expensive: an adjective modifying objects."},{"Id":6,"Prompt":"Fill in the missing letters.","Prefix":"bec","MissingLetters":4,"Options":[],"Answer":"ause","Explanation":"because: introduces the reason."},{"Id":7,"Prompt":"Fill in the missing letters.","Prefix":"to","MissingLetters":3,"Options":[],"Answer":"ols","Explanation":"tools: a plural noun modified by common."},{"Id":8,"Prompt":"Fill in the missing letters.","Prefix":"hab","MissingLetters":3,"Options":[],"Answer":"its","Explanation":"habits: the object of reflect."},{"Id":9,"Prompt":"Fill in the missing letters.","Prefix":"b","MissingLetters":1,"Options":[],"Answer":"y","Explanation":"by: identifies who shares something in shared by."},{"Id":10,"Prompt":"Fill in the missing letters.","Prefix":"ordi","MissingLetters":4,"Options":[],"Answer":"nary","Explanation":"ordinary: an adjective modifying people."}]''
    WHERE [Id] = ''865059ed-7c8d-5859-3fad-54880b77186d'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011195749_EnglishInterfaceAndForwardExams'
)
BEGIN
    EXEC(N'UPDATE [content].[ReadingSetVersions] SET [ItemsJson] = N''[{"Id":1,"Prompt":"What is the main idea?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"Gold is always more informative than pottery"},{"Id":"B","Text":"Chemical tests eliminate uncertainty"},{"Id":"C","Text":"Pottery and its context together reveal past activities"},{"Id":"D","Text":"Broken objects should be discarded"}],"Answer":"C","Explanation":"Both the material and the context of its discovery provide evidence about past lives."},{"Id":2,"Prompt":"What might chemical analysis reveal?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"The name of the pot\u0027s owner"},{"Id":"B","Text":"Food or drink residues"},{"Id":"C","Text":"The exact date of every scratch"},{"Id":"D","Text":"The value of gold"}],"Answer":"B","Explanation":"The second paragraph explicitly mentions food and drink residues."},{"Id":3,"Prompt":"Why are possible alternative explanations mentioned?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"To show why several observations are needed"},{"Id":"B","Text":"To reject all chemical testing"},{"Id":"C","Text":"To prove every pot was buried deliberately"},{"Id":"D","Text":"To explain why records are unnecessary"}],"Answer":"A","Explanation":"Different possible causes of residues and scratches show why a single observation is insufficient."},{"Id":4,"Prompt":"The word \u0027these\u0027 in \u0027these relationships\u0027 refers to","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"Chemical substances"},{"Id":"B","Text":"Improved methods"},{"Id":"C","Text":"Golden ornaments"},{"Id":"D","Text":"Connections between vessels and their surroundings"}],"Answer":"D","Explanation":"The preceding sentence describes different uses of vessels in homes and shared spaces."},{"Id":5,"Prompt":"Where would this sentence best fit? \u2018Neither kind of evidence offers a complete story on its own.\u2019 Choose a marked location [A]\u2013[D] in the passage.","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"Location [A]"},{"Id":"B","Text":"Location [B]"},{"Id":"C","Text":"Location [C]"},{"Id":"D","Text":"Location [D]"}],"Answer":"B","Explanation":"Neither kind refers to chemical residues and surface marks. Position [B] follows their introduction; the next sentences explain why neither gives a definite conclusion alone."}]''
    WHERE [Id] = ''911d2255-f075-41eb-5140-b11f2cd96377'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011195749_EnglishInterfaceAndForwardExams'
)
BEGIN
    EXEC(N'UPDATE [content].[ReadingSetVersions] SET [ItemsJson] = N''[{"Id":1,"Prompt":"How much does a sandwich and soup cost at 12:00 on Tuesday?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"$7"},{"Id":"B","Text":"$11"},{"Id":"C","Text":"$9"},{"Id":"D","Text":"$13"}],"Answer":"C","Explanation":"The weekday deal includes both items for $9 between 11:30 and 14:00."},{"Id":2,"Prompt":"Who can receive free tea?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"Everyone ordering soup"},{"Id":"B","Text":"A student showing a card with a lunch deal"},{"Id":"C","Text":"Anyone visiting after 14:00"},{"Id":"D","Text":"Anyone buying a salad"}],"Answer":"B","Explanation":"Free tea requires both a lunch deal and a student card."}]''
    WHERE [Id] = ''92c7d63a-f66c-44fb-f278-2953b5967fcf'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011195749_EnglishInterfaceAndForwardExams'
)
BEGIN
    EXEC(N'UPDATE [content].[ReadingSetVersions] SET [ItemsJson] = N''[{"Id":1,"Prompt":"What does the passage explain?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"Why all islands have identical habitats"},{"Id":"B","Text":"How isolation and area influence species communities"},{"Id":"C","Text":"Why birds cannot transport seeds"},{"Id":"D","Text":"How farmland increases every population"}],"Answer":"B","Explanation":"The passage connects distance, area, and habitat fragments on land."},{"Id":2,"Prompt":"Why can small populations be vulnerable?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"They always move too far"},{"Id":"B","Text":"They have too many habitats"},{"Id":"C","Text":"A single event may affect a large part of them"},{"Id":"D","Text":"They cannot use nesting sites"}],"Answer":"C","Explanation":"The example of a storm destroying a single nesting site illustrates this risk."},{"Id":3,"Prompt":"What does \u0027local disappearances\u0027 mean?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"Loss from a particular place"},{"Id":"B","Text":"Extinction across the whole planet"},{"Id":"C","Text":"Seasonal changes in weather"},{"Id":"D","Text":"The formation of new islands"}],"Answer":"A","Explanation":"The second paragraph distinguishes disappearance from one island from global extinction."},{"Id":4,"Prompt":"Why is a forest surrounded by farms compared to an island?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"Both are always surrounded by water"},{"Id":"B","Text":"Both have no birds"},{"Id":"C","Text":"Both contain only one habitat"},{"Id":"D","Text":"The surrounding area may prevent movement"}],"Answer":"D","Explanation":"A forest is functionally isolated for species unable to cross the surrounding farmland."},{"Id":5,"Prompt":"What is implied about wildlife corridors?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"They benefit every species equally"},{"Id":"B","Text":"Their usefulness depends on the organism"},{"Id":"C","Text":"They always eliminate local loss"},{"Id":"D","Text":"They are unrelated to movement"}],"Answer":"B","Explanation":"The usefulness of a corridor depends on how the particular species moves."}]''
    WHERE [Id] = ''93e23019-d9ed-83e2-ab87-78112f7883af'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011195749_EnglishInterfaceAndForwardExams'
)
BEGIN
    EXEC(N'UPDATE [content].[ReadingSetVersions] SET [ItemsJson] = N''[{"Id":1,"Prompt":"What is the main purpose of the post?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"To sell household items"},{"Id":"B","Text":"To recruit appliance manufacturers"},{"Id":"C","Text":"To invite people to a repair event"},{"Id":"D","Text":"To offer free replacement parts"}],"Answer":"C","Explanation":"The place, time, and participation details invite people to a repair event."},{"Id":2,"Prompt":"What might a participant have to pay for?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"Volunteer advice"},{"Id":"B","Text":"Replacement parts"},{"Id":"C","Text":"An entry ticket"},{"Id":"D","Text":"A reservation"}],"Answer":"B","Explanation":"Advice is free, but the owner pays for replacement parts."},{"Id":3,"Prompt":"What can be inferred about attending without booking?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"Repair is guaranteed"},{"Id":"B","Text":"Only experts can attend"},{"Id":"C","Text":"Large items are accepted"},{"Id":"D","Text":"Help may not be available"}],"Answer":"D","Explanation":"People without reservations receive help only if time permits."}]''
    WHERE [Id] = ''a01c9a8e-e268-74e1-f0e6-066ec4706f98'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011195749_EnglishInterfaceAndForwardExams'
)
BEGIN
    EXEC(N'UPDATE [content].[ReadingSetVersions] SET [ItemsJson] = N''[{"Id":1,"Prompt":"Where will the students first meet if it rains?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"In Room 12"},{"Id":"B","Text":"In the caf\u00E9"},{"Id":"C","Text":"In the courtyard"},{"Id":"D","Text":"By the library entrance"}],"Answer":"D","Explanation":"Otherwise refers to the condition in which the weather is not dry."},{"Id":2,"Prompt":"What has Nora already done?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"Uploaded the article"},{"Id":"B","Text":"Printed the charts"},{"Id":"C","Text":"Booked Room 12"},{"Id":"D","Text":"Found an indoor room"}],"Answer":"A","Explanation":"Done and group folder show that the article has been shared online."}]''
    WHERE [Id] = ''a1b67254-22bb-c34a-536a-dbe31a433274'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011195749_EnglishInterfaceAndForwardExams'
)
BEGIN
    EXEC(N'UPDATE [content].[ReadingSetVersions] SET [ItemsJson] = N''[{"Id":1,"Prompt":"Fill in the missing letters.","Prefix":"lea","MissingLetters":3,"Options":[],"Answer":"ves","Explanation":"leaves: the plural noun for the parts of a tree that provide shade."},{"Id":2,"Prompt":"Fill in the missing letters.","Prefix":"sh","MissingLetters":3,"Options":[],"Answer":"ade","Explanation":"shade: the object of provide."},{"Id":3,"Prompt":"Fill in the missing letters.","Prefix":"sum","MissingLetters":3,"Options":[],"Answer":"mer","Explanation":"summer: a season following during."},{"Id":4,"Prompt":"Fill in the missing letters.","Prefix":"wa","MissingLetters":3,"Options":[],"Answer":"ter","Explanation":"water: the subject of evaporates."},{"Id":5,"Prompt":"Fill in the missing letters.","Prefix":"fr","MissingLetters":2,"Options":[],"Answer":"om","Explanation":"from: identifies the starting point of evaporation."},{"Id":6,"Prompt":"Fill in the missing letters.","Prefix":"open","MissingLetters":4,"Options":[],"Answer":"ings","Explanation":"openings: a plural noun modified by tiny."},{"Id":7,"Prompt":"Fill in the missing letters.","Prefix":"co","MissingLetters":3,"Options":[],"Answer":"ols","Explanation":"cools: agrees with the singular subject water."},{"Id":8,"Prompt":"Fill in the missing letters.","Prefix":"surro","MissingLetters":6,"Options":[],"Answer":"unding","Explanation":"surrounding: an adjective modifying air."},{"Id":9,"Prompt":"Fill in the missing letters.","Prefix":"throu","MissingLetters":5,"Options":[],"Answer":"ghout","Explanation":"throughout: means during the whole period."},{"Id":10,"Prompt":"Fill in the missing letters.","Prefix":"d","MissingLetters":2,"Options":[],"Answer":"ay","Explanation":"day: a time noun used with the."}]''
    WHERE [Id] = ''d1100000-0000-4000-8000-000000000001'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011195749_EnglishInterfaceAndForwardExams'
)
BEGIN
    EXEC(N'UPDATE [content].[ReadingSetVersions] SET [ItemsJson] = N''[{"Id":1,"Prompt":"Fill in the missing letters.","Prefix":"peo","MissingLetters":3,"Options":[],"Answer":"ple","Explanation":"people: the plural subject of recall."},{"Id":2,"Prompt":"Fill in the missing letters.","Prefix":"eve","MissingLetters":3,"Options":[],"Answer":"nts","Explanation":"events: the things that are recalled."},{"Id":3,"Prompt":"Fill in the missing letters.","Prefix":"bra","MissingLetters":3,"Options":[],"Answer":"ins","Explanation":"brains: a plural noun following their."},{"Id":4,"Prompt":"Fill in the missing letters.","Prefix":"sto","MissingLetters":3,"Options":[],"Answer":"red","Explanation":"stored: describes details previously kept in memory."},{"Id":5,"Prompt":"Fill in the missing letters.","Prefix":"wi","MissingLetters":2,"Options":[],"Answer":"th","Explanation":"with: the pattern is combine A with B."},{"Id":6,"Prompt":"Fill in the missing letters.","Prefix":"know","MissingLetters":5,"Options":[],"Answer":"ledge","Explanation":"knowledge: a noun following the adjective current."},{"Id":7,"Prompt":"Fill in the missing letters.","Prefix":"memo","MissingLetters":4,"Options":[],"Answer":"ries","Explanation":"memories: the subject of change."},{"Id":8,"Prompt":"Fill in the missing letters.","Prefix":"cha","MissingLetters":3,"Options":[],"Answer":"nge","Explanation":"change: the base-form verb agrees with plural memories."},{"Id":9,"Prompt":"Fill in the missing letters.","Prefix":"ea","MissingLetters":2,"Options":[],"Answer":"ch","Explanation":"each: refers to every new experience."},{"Id":10,"Prompt":"Fill in the missing letters.","Prefix":"exper","MissingLetters":5,"Options":[],"Answer":"ience","Explanation":"experience: a singular noun following new."}]''
    WHERE [Id] = ''d1100000-0000-4000-8000-000000000002'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011195749_EnglishInterfaceAndForwardExams'
)
BEGIN
    EXEC(N'UPDATE [content].[ReadingSetVersions] SET [ItemsJson] = N''[{"Id":1,"Prompt":"Fill in the missing letters.","Prefix":"ra","MissingLetters":2,"Options":[],"Answer":"in","Explanation":"rain: the subject of falls."},{"Id":2,"Prompt":"Fill in the missing letters.","Prefix":"so","MissingLetters":2,"Options":[],"Answer":"me","Explanation":"some: indicates an amount of water."},{"Id":3,"Prompt":"Fill in the missing letters.","Prefix":"mo","MissingLetters":3,"Options":[],"Answer":"ves","Explanation":"moves: agrees with singular water."},{"Id":4,"Prompt":"Fill in the missing letters.","Prefix":"sm","MissingLetters":3,"Options":[],"Answer":"all","Explanation":"small: modifies spaces."},{"Id":5,"Prompt":"Fill in the missing letters.","Prefix":"bet","MissingLetters":4,"Options":[],"Answer":"ween","Explanation":"between: describes the position among rocks."},{"Id":6,"Prompt":"Fill in the missing letters.","Prefix":"wh","MissingLetters":3,"Options":[],"Answer":"ere","Explanation":"where: refers to the location described by rocks."},{"Id":7,"Prompt":"Fill in the missing letters.","Prefix":"lay","MissingLetters":3,"Options":[],"Answer":"ers","Explanation":"layers: a noun following underground."},{"Id":8,"Prompt":"Fill in the missing letters.","Prefix":"col","MissingLetters":4,"Options":[],"Answer":"lect","Explanation":"collect: agrees with plural layers."},{"Id":9,"Prompt":"Fill in the missing letters.","Prefix":"st","MissingLetters":3,"Options":[],"Answer":"ore","Explanation":"store: collect and store are parallel verbs."},{"Id":10,"Prompt":"Fill in the missing letters.","Prefix":"valu","MissingLetters":4,"Options":[],"Answer":"able","Explanation":"valuable: an adjective modifying resource."}]''
    WHERE [Id] = ''d1100000-0000-4000-8000-000000000003'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011195749_EnglishInterfaceAndForwardExams'
)
BEGIN
    EXEC(N'UPDATE [content].[ReadingSetVersions] SET [ItemsJson] = N''[{"Id":1,"Prompt":"Fill in the missing letters.","Prefix":"wor","MissingLetters":3,"Options":[],"Answer":"ker","Explanation":"worker: a singular noun following a."},{"Id":2,"Prompt":"Fill in the missing letters.","Prefix":"ho","MissingLetters":2,"Options":[],"Answer":"me","Explanation":"home: the destination after returns."},{"Id":3,"Prompt":"Fill in the missing letters.","Prefix":"fin","MissingLetters":4,"Options":[],"Answer":"ding","Explanation":"finding: an -ing form after the preposition after."},{"Id":4,"Prompt":"Fill in the missing letters.","Prefix":"a","MissingLetters":2,"Options":[],"Answer":"nd","Explanation":"and: connects two actions."},{"Id":5,"Prompt":"Fill in the missing letters.","Prefix":"move","MissingLetters":5,"Options":[],"Answer":"ments","Explanation":"movements: the object of performs."},{"Id":6,"Prompt":"Fill in the missing letters.","Prefix":"indi","MissingLetters":4,"Options":[],"Answer":"cate","Explanation":"indicate: agrees with the plural subject movements."},{"Id":7,"Prompt":"Fill in the missing letters.","Prefix":"wh","MissingLetters":3,"Options":[],"Answer":"ile","Explanation":"while: connects simultaneous actions."},{"Id":8,"Prompt":"Fill in the missing letters.","Prefix":"be","MissingLetters":2,"Options":[],"Answer":"es","Explanation":"bees: a plural noun following other."},{"Id":9,"Prompt":"Fill in the missing letters.","Prefix":"a","MissingLetters":2,"Options":[],"Answer":"nd","Explanation":"and: connects watch and receive."},{"Id":10,"Prompt":"Fill in the missing letters.","Prefix":"use","MissingLetters":3,"Options":[],"Answer":"ful","Explanation":"useful: modifies information."}]''
    WHERE [Id] = ''d1100000-0000-4000-8000-000000000004'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011195749_EnglishInterfaceAndForwardExams'
)
BEGIN
    EXEC(N'UPDATE [content].[ReadingSetVersions] SET [ItemsJson] = N''[{"Id":1,"Prompt":"Fill in the missing letters.","Prefix":"pot","MissingLetters":4,"Options":[],"Answer":"tery","Explanation":"pottery: a collective noun modified by broken."},{"Id":2,"Prompt":"Fill in the missing letters.","Prefix":"rev","MissingLetters":4,"Options":[],"Answer":"eals","Explanation":"reveals: agrees with singular pottery."},{"Id":3,"Prompt":"Fill in the missing letters.","Prefix":"ab","MissingLetters":3,"Options":[],"Answer":"out","Explanation":"about: the pattern is reveal information about."},{"Id":4,"Prompt":"Fill in the missing letters.","Prefix":"li","MissingLetters":2,"Options":[],"Answer":"fe","Explanation":"life: part of the phrase daily life."},{"Id":5,"Prompt":"Fill in the missing letters.","Prefix":"expe","MissingLetters":5,"Options":[],"Answer":"nsive","Explanation":"expensive: an adjective modifying objects."},{"Id":6,"Prompt":"Fill in the missing letters.","Prefix":"bec","MissingLetters":4,"Options":[],"Answer":"ause","Explanation":"because: introduces the reason."},{"Id":7,"Prompt":"Fill in the missing letters.","Prefix":"to","MissingLetters":3,"Options":[],"Answer":"ols","Explanation":"tools: a plural noun modified by common."},{"Id":8,"Prompt":"Fill in the missing letters.","Prefix":"hab","MissingLetters":3,"Options":[],"Answer":"its","Explanation":"habits: the object of reflect."},{"Id":9,"Prompt":"Fill in the missing letters.","Prefix":"b","MissingLetters":1,"Options":[],"Answer":"y","Explanation":"by: identifies who shares something in shared by."},{"Id":10,"Prompt":"Fill in the missing letters.","Prefix":"ordi","MissingLetters":4,"Options":[],"Answer":"nary","Explanation":"ordinary: an adjective modifying people."}]''
    WHERE [Id] = ''d1100000-0000-4000-8000-000000000005'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011195749_EnglishInterfaceAndForwardExams'
)
BEGIN
    EXEC(N'UPDATE [content].[ReadingSetVersions] SET [ItemsJson] = N''[{"Id":1,"Prompt":"Where should students collect reserved books?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"At the ground-floor desk"},{"Id":"B","Text":"At the second-floor office"},{"Id":"C","Text":"At the main entrance"},{"Id":"D","Text":"At their department"}],"Answer":"A","Explanation":"The notice explicitly changes the collection point to the ground-floor desk."},{"Id":2,"Prompt":"What should a student do if unable to collect a book on time?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"Reserve another copy"},{"Id":"B","Text":"Call after three days"},{"Id":"C","Text":"Reply before the deadline"},{"Id":"D","Text":"Visit the old office"}],"Answer":"C","Explanation":"Students must reply to the email before the deadline to request an extension."}]''
    WHERE [Id] = ''d2100000-0000-4000-8000-000000000001'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011195749_EnglishInterfaceAndForwardExams'
)
BEGIN
    EXEC(N'UPDATE [content].[ReadingSetVersions] SET [ItemsJson] = N''[{"Id":1,"Prompt":"Why was this email sent?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"To cancel the club"},{"Id":"B","Text":"To explain a change of activity"},{"Id":"C","Text":"To request camera repairs"},{"Id":"D","Text":"To announce a new starting time"}],"Answer":"B","Explanation":"The weather changes the location and activity, but not the starting time."},{"Id":2,"Prompt":"What should participants bring?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"Raincoats"},{"Id":"B","Text":"New cameras"},{"Id":"C","Text":"Printed schedules"},{"Id":"D","Text":"Three existing photos"}],"Answer":"D","Explanation":"Participants are asked to bring three photos they have already taken."},{"Id":3,"Prompt":"What remains unchanged?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"The return date for cameras"},{"Id":"B","Text":"The workshop location"},{"Id":"C","Text":"The outdoor activity"},{"Id":"D","Text":"The need to take new pictures"}],"Answer":"A","Explanation":"Originally planned confirms that cameras are still due on Monday."}]''
    WHERE [Id] = ''d2100000-0000-4000-8000-000000000002'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011195749_EnglishInterfaceAndForwardExams'
)
BEGIN
    EXEC(N'UPDATE [content].[ReadingSetVersions] SET [ItemsJson] = N''[{"Id":1,"Prompt":"How much does a sandwich and soup cost at 12:00 on Tuesday?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"$7"},{"Id":"B","Text":"$11"},{"Id":"C","Text":"$9"},{"Id":"D","Text":"$13"}],"Answer":"C","Explanation":"The weekday deal includes both items for $9 between 11:30 and 14:00."},{"Id":2,"Prompt":"Who can receive free tea?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"Everyone ordering soup"},{"Id":"B","Text":"A student showing a card with a lunch deal"},{"Id":"C","Text":"Anyone visiting after 14:00"},{"Id":"D","Text":"Anyone buying a salad"}],"Answer":"B","Explanation":"Free tea requires both a lunch deal and a student card."}]''
    WHERE [Id] = ''d2100000-0000-4000-8000-000000000003'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011195749_EnglishInterfaceAndForwardExams'
)
BEGIN
    EXEC(N'UPDATE [content].[ReadingSetVersions] SET [ItemsJson] = N''[{"Id":1,"Prompt":"Where will the students first meet if it rains?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"In Room 12"},{"Id":"B","Text":"In the caf\u00E9"},{"Id":"C","Text":"In the courtyard"},{"Id":"D","Text":"By the library entrance"}],"Answer":"D","Explanation":"Otherwise refers to the condition in which the weather is not dry."},{"Id":2,"Prompt":"What has Nora already done?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"Uploaded the article"},{"Id":"B","Text":"Printed the charts"},{"Id":"C","Text":"Booked Room 12"},{"Id":"D","Text":"Found an indoor room"}],"Answer":"A","Explanation":"Done and group folder show that the article has been shared online."}]''
    WHERE [Id] = ''d2100000-0000-4000-8000-000000000004'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011195749_EnglishInterfaceAndForwardExams'
)
BEGIN
    EXEC(N'UPDATE [content].[ReadingSetVersions] SET [ItemsJson] = N''[{"Id":1,"Prompt":"What is the main purpose of the post?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"To sell household items"},{"Id":"B","Text":"To recruit appliance manufacturers"},{"Id":"C","Text":"To invite people to a repair event"},{"Id":"D","Text":"To offer free replacement parts"}],"Answer":"C","Explanation":"The place, time, and participation details invite people to a repair event."},{"Id":2,"Prompt":"What might a participant have to pay for?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"Volunteer advice"},{"Id":"B","Text":"Replacement parts"},{"Id":"C","Text":"An entry ticket"},{"Id":"D","Text":"A reservation"}],"Answer":"B","Explanation":"Advice is free, but the owner pays for replacement parts."},{"Id":3,"Prompt":"What can be inferred about attending without booking?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"Repair is guaranteed"},{"Id":"B","Text":"Only experts can attend"},{"Id":"C","Text":"Large items are accepted"},{"Id":"D","Text":"Help may not be available"}],"Answer":"D","Explanation":"People without reservations receive help only if time permits."}]''
    WHERE [Id] = ''d2100000-0000-4000-8000-000000000005'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011195749_EnglishInterfaceAndForwardExams'
)
BEGIN
    EXEC(N'UPDATE [content].[ReadingSetVersions] SET [ItemsJson] = N''[{"Id":1,"Prompt":"What is the passage mainly about?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"Why rural areas need paved roads"},{"Id":"B","Text":"How trees cool cities and what supports their success"},{"Id":"C","Text":"Why cities should stop planting trees"},{"Id":"D","Text":"How to measure the age of a tree"}],"Answer":"B","Explanation":"The passage combines cooling mechanisms, limitations, and long-term planting design."},{"Id":2,"Prompt":"How does evaporation help cool the air?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"It prevents all rainfall"},{"Id":"B","Text":"It releases stored solar energy"},{"Id":"C","Text":"It takes heat from the air"},{"Id":"D","Text":"It increases road temperature"}],"Answer":"C","Explanation":"The first paragraph explains that evaporating water takes heat from surrounding air."},{"Id":3,"Prompt":"The word \u0027constraints\u0027 is closest in meaning to","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"celebrations"},{"Id":"B","Text":"measurements"},{"Id":"C","Text":"predictions"},{"Id":"D","Text":"limitations"}],"Answer":"D","Explanation":"The phrase refers to conditions affecting success, such as water needs and climate."},{"Id":4,"Prompt":"Why does the author mention permeable paving?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"To give an example of supporting tree survival"},{"Id":"B","Text":"To argue that shade is unnecessary"},{"Id":"C","Text":"To describe a rural farming method"},{"Id":"D","Text":"To show that roots should remain dry"}],"Answer":"A","Explanation":"Permeable surfaces illustrate planning trees as part of the surrounding system."},{"Id":5,"Prompt":"What can be inferred about counting newly planted trees?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"It always measures cooling accurately"},{"Id":"B","Text":"It is sufficient for planning"},{"Id":"C","Text":"It does not alone show long-term benefits"},{"Id":"D","Text":"It proves that trees will survive"}],"Answer":"C","Explanation":"The final paragraph emphasizes conditions for survival rather than the number planted."}]''
    WHERE [Id] = ''d3100000-0000-4000-8000-000000000001'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011195749_EnglishInterfaceAndForwardExams'
)
BEGIN
    EXEC(N'UPDATE [content].[ReadingSetVersions] SET [ItemsJson] = N''[{"Id":1,"Prompt":"What distinction is central to the passage?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"Printed versus digital notes"},{"Id":"B","Text":"Familiarity versus unaided recall"},{"Id":"C","Text":"Morning versus evening study"},{"Id":"D","Text":"Science versus history lessons"}],"Answer":"B","Explanation":"The first paragraph distinguishes familiarity from recalling without help."},{"Id":2,"Prompt":"Why should learners check answers after retrieval?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"To avoid all future effort"},{"Id":"B","Text":"To memorize page numbers"},{"Id":"C","Text":"To identify and correct gaps"},{"Id":"D","Text":"To make sessions longer"}],"Answer":"C","Explanation":"Comparing an answer with its source reveals gaps and errors."},{"Id":3,"Prompt":"What does \u0027accessible\u0027 mean in the final paragraph?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"Available to recall"},{"Id":"B","Text":"Printed in large type"},{"Id":"C","Text":"Free to purchase"},{"Id":"D","Text":"Located nearby"}],"Answer":"A","Explanation":"The phrase describes information remaining available in memory over time."},{"Id":4,"Prompt":"What can be inferred about an easy study session?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"It must involve retrieval"},{"Id":"B","Text":"It guarantees long-term learning"},{"Id":"C","Text":"It requires no feedback"},{"Id":"D","Text":"It may give a misleading impression of learning"}],"Answer":"D","Explanation":"Ease and familiarity alone do not demonstrate lasting learning."},{"Id":5,"Prompt":"How is the second paragraph organized?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"A historical event followed by dates"},{"Id":"B","Text":"A benefit followed by a condition for its success"},{"Id":"C","Text":"Two unrelated definitions"},{"Id":"D","Text":"A list of examination rules"}],"Answer":"B","Explanation":"The author explains the benefit of identifying gaps, then adds the need for accurate feedback."}]''
    WHERE [Id] = ''d3100000-0000-4000-8000-000000000002'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011195749_EnglishInterfaceAndForwardExams'
)
BEGIN
    EXEC(N'UPDATE [content].[ReadingSetVersions] SET [ItemsJson] = N''[{"Id":1,"Prompt":"What is the main idea?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"Gold is always more informative than pottery"},{"Id":"B","Text":"Chemical tests eliminate uncertainty"},{"Id":"C","Text":"Pottery and its context together reveal past activities"},{"Id":"D","Text":"Broken objects should be discarded"}],"Answer":"C","Explanation":"Both the material and the context of its discovery provide evidence about past lives."},{"Id":2,"Prompt":"What might chemical analysis reveal?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"The name of the pot\u0027s owner"},{"Id":"B","Text":"Food or drink residues"},{"Id":"C","Text":"The exact date of every scratch"},{"Id":"D","Text":"The value of gold"}],"Answer":"B","Explanation":"The second paragraph explicitly mentions food and drink residues."},{"Id":3,"Prompt":"Why are possible alternative explanations mentioned?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"To show why several observations are needed"},{"Id":"B","Text":"To reject all chemical testing"},{"Id":"C","Text":"To prove every pot was buried deliberately"},{"Id":"D","Text":"To explain why records are unnecessary"}],"Answer":"A","Explanation":"Different possible causes of residues and scratches show why a single observation is insufficient."},{"Id":4,"Prompt":"The word \u0027these\u0027 in \u0027these relationships\u0027 refers to","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"Chemical substances"},{"Id":"B","Text":"Improved methods"},{"Id":"C","Text":"Golden ornaments"},{"Id":"D","Text":"Connections between vessels and their surroundings"}],"Answer":"D","Explanation":"The preceding sentence describes different uses of vessels in homes and shared spaces."},{"Id":5,"Prompt":"What can be inferred about careful records?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"They prevent new interpretations"},{"Id":"B","Text":"They are useful only to the first researcher"},{"Id":"C","Text":"They allow evidence to be reconsidered later"},{"Id":"D","Text":"They replace the need to preserve objects"}],"Answer":"C","Explanation":"The final paragraph explains how new methods can test earlier interpretations."}]''
    WHERE [Id] = ''d3100000-0000-4000-8000-000000000003'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011195749_EnglishInterfaceAndForwardExams'
)
BEGIN
    EXEC(N'UPDATE [content].[ReadingSetVersions] SET [ItemsJson] = N''[{"Id":1,"Prompt":"What does the passage explain?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"Why all islands have identical habitats"},{"Id":"B","Text":"How isolation and area influence species communities"},{"Id":"C","Text":"Why birds cannot transport seeds"},{"Id":"D","Text":"How farmland increases every population"}],"Answer":"B","Explanation":"The passage connects distance, area, and habitat fragments on land."},{"Id":2,"Prompt":"Why can small populations be vulnerable?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"They always move too far"},{"Id":"B","Text":"They have too many habitats"},{"Id":"C","Text":"A single event may affect a large part of them"},{"Id":"D","Text":"They cannot use nesting sites"}],"Answer":"C","Explanation":"The example of a storm destroying a single nesting site illustrates this risk."},{"Id":3,"Prompt":"What does \u0027local disappearances\u0027 mean?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"Loss from a particular place"},{"Id":"B","Text":"Extinction across the whole planet"},{"Id":"C","Text":"Seasonal changes in weather"},{"Id":"D","Text":"The formation of new islands"}],"Answer":"A","Explanation":"The second paragraph distinguishes disappearance from one island from global extinction."},{"Id":4,"Prompt":"Why is a forest surrounded by farms compared to an island?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"Both are always surrounded by water"},{"Id":"B","Text":"Both have no birds"},{"Id":"C","Text":"Both contain only one habitat"},{"Id":"D","Text":"The surrounding area may prevent movement"}],"Answer":"D","Explanation":"A forest is functionally isolated for species unable to cross the surrounding farmland."},{"Id":5,"Prompt":"What is implied about wildlife corridors?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"They benefit every species equally"},{"Id":"B","Text":"Their usefulness depends on the organism"},{"Id":"C","Text":"They always eliminate local loss"},{"Id":"D","Text":"They are unrelated to movement"}],"Answer":"B","Explanation":"The usefulness of a corridor depends on how the particular species moves."}]''
    WHERE [Id] = ''d3100000-0000-4000-8000-000000000004'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011195749_EnglishInterfaceAndForwardExams'
)
BEGIN
    EXEC(N'UPDATE [content].[ReadingSetVersions] SET [ItemsJson] = N''[{"Id":1,"Prompt":"What is the passage mainly concerned with?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"Using lake sediment to investigate past environments"},{"Id":"B","Text":"Predicting the exact date of future fires"},{"Id":"C","Text":"Removing all sediment from lakes"},{"Id":"D","Text":"Showing that pollen is useless"}],"Answer":"A","Explanation":"The passage explains how layers reveal environmental history and the limits of interpretation."},{"Id":2,"Prompt":"What is generally true of deeper sediment?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"It contains no pollen"},{"Id":"B","Text":"It is always grass"},{"Id":"C","Text":"It is older"},{"Id":"D","Text":"It has no environmental value"}],"Answer":"C","Explanation":"The first paragraph states that deeper material is generally older."},{"Id":3,"Prompt":"Why might an increase in grass pollen be difficult to interpret?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"Grass never produces pollen"},{"Id":"B","Text":"More than one process could explain it"},{"Id":"C","Text":"Trees cannot grow near lakes"},{"Id":"D","Text":"All pollen has the same age"}],"Answer":"B","Explanation":"Climate change and human land clearing can produce the same pattern."},{"Id":4,"Prompt":"The word \u0027indicator\u0027 is closest in meaning to","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"instruction"},{"Id":"B","Text":"container"},{"Id":"C","Text":"obstacle"},{"Id":"D","Text":"sign"}],"Answer":"D","Explanation":"Indicator means a sign or clue about past conditions."},{"Id":5,"Prompt":"Why does the author contrast sediment with a precise diary?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"To claim scientists should use diaries only"},{"Id":"B","Text":"To emphasize that interpretation involves uncertainty"},{"Id":"C","Text":"To argue that deeper layers are always younger"},{"Id":"D","Text":"To dismiss historical records"}],"Answer":"B","Explanation":"Uncertainty about dates and causes prevents reading sediment as a precise diary."}]''
    WHERE [Id] = ''d3100000-0000-4000-8000-000000000005'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011195749_EnglishInterfaceAndForwardExams'
)
BEGIN
    EXEC(N'UPDATE [content].[ReadingSetVersions] SET [ItemsJson] = N''[{"Id":1,"Prompt":"What is the passage mainly about?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"Why rural areas need paved roads"},{"Id":"B","Text":"How trees cool cities and what supports their success"},{"Id":"C","Text":"Why cities should stop planting trees"},{"Id":"D","Text":"How to measure the age of a tree"}],"Answer":"B","Explanation":"The passage combines cooling mechanisms, limitations, and long-term planting design."},{"Id":2,"Prompt":"How does evaporation help cool the air?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"It prevents all rainfall"},{"Id":"B","Text":"It releases stored solar energy"},{"Id":"C","Text":"It takes heat from the air"},{"Id":"D","Text":"It increases road temperature"}],"Answer":"C","Explanation":"The first paragraph explains that evaporating water takes heat from surrounding air."},{"Id":3,"Prompt":"The word \u0027constraints\u0027 is closest in meaning to","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"celebrations"},{"Id":"B","Text":"measurements"},{"Id":"C","Text":"predictions"},{"Id":"D","Text":"limitations"}],"Answer":"D","Explanation":"The phrase refers to conditions affecting success, such as water needs and climate."},{"Id":4,"Prompt":"Why does the author mention permeable paving?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"To give an example of supporting tree survival"},{"Id":"B","Text":"To argue that shade is unnecessary"},{"Id":"C","Text":"To describe a rural farming method"},{"Id":"D","Text":"To show that roots should remain dry"}],"Answer":"A","Explanation":"Permeable surfaces illustrate planning trees as part of the surrounding system."},{"Id":5,"Prompt":"What can be inferred about counting newly planted trees?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"It always measures cooling accurately"},{"Id":"B","Text":"It is sufficient for planning"},{"Id":"C","Text":"It does not alone show long-term benefits"},{"Id":"D","Text":"It proves that trees will survive"}],"Answer":"C","Explanation":"The final paragraph emphasizes conditions for survival rather than the number planted."}]''
    WHERE [Id] = ''d3200000-0000-4000-8000-000000000001'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011195749_EnglishInterfaceAndForwardExams'
)
BEGIN
    EXEC(N'UPDATE [content].[ReadingSetVersions] SET [ItemsJson] = N''[{"Id":1,"Prompt":"What distinction is central to the passage?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"Printed versus digital notes"},{"Id":"B","Text":"Familiarity versus unaided recall"},{"Id":"C","Text":"Morning versus evening study"},{"Id":"D","Text":"Science versus history lessons"}],"Answer":"B","Explanation":"The first paragraph distinguishes familiarity from recalling without help."},{"Id":2,"Prompt":"Why should learners check answers after retrieval?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"To avoid all future effort"},{"Id":"B","Text":"To memorize page numbers"},{"Id":"C","Text":"To identify and correct gaps"},{"Id":"D","Text":"To make sessions longer"}],"Answer":"C","Explanation":"Comparing an answer with its source reveals gaps and errors."},{"Id":3,"Prompt":"What does \u0027accessible\u0027 mean in the final paragraph?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"Available to recall"},{"Id":"B","Text":"Printed in large type"},{"Id":"C","Text":"Free to purchase"},{"Id":"D","Text":"Located nearby"}],"Answer":"A","Explanation":"The phrase describes information remaining available in memory over time."},{"Id":4,"Prompt":"What can be inferred about an easy study session?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"It must involve retrieval"},{"Id":"B","Text":"It guarantees long-term learning"},{"Id":"C","Text":"It requires no feedback"},{"Id":"D","Text":"It may give a misleading impression of learning"}],"Answer":"D","Explanation":"Ease and familiarity alone do not demonstrate lasting learning."},{"Id":5,"Prompt":"How is the second paragraph organized?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"A historical event followed by dates"},{"Id":"B","Text":"A benefit followed by a condition for its success"},{"Id":"C","Text":"Two unrelated definitions"},{"Id":"D","Text":"A list of examination rules"}],"Answer":"B","Explanation":"The author explains the benefit of identifying gaps, then adds the need for accurate feedback."}]''
    WHERE [Id] = ''d3200000-0000-4000-8000-000000000002'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011195749_EnglishInterfaceAndForwardExams'
)
BEGIN
    EXEC(N'UPDATE [content].[ReadingSetVersions] SET [ItemsJson] = N''[{"Id":1,"Prompt":"What is the main idea?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"Gold is always more informative than pottery"},{"Id":"B","Text":"Chemical tests eliminate uncertainty"},{"Id":"C","Text":"Pottery and its context together reveal past activities"},{"Id":"D","Text":"Broken objects should be discarded"}],"Answer":"C","Explanation":"Both the material and the context of its discovery provide evidence about past lives."},{"Id":2,"Prompt":"What might chemical analysis reveal?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"The name of the pot\u0027s owner"},{"Id":"B","Text":"Food or drink residues"},{"Id":"C","Text":"The exact date of every scratch"},{"Id":"D","Text":"The value of gold"}],"Answer":"B","Explanation":"The second paragraph explicitly mentions food and drink residues."},{"Id":3,"Prompt":"Why are possible alternative explanations mentioned?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"To show why several observations are needed"},{"Id":"B","Text":"To reject all chemical testing"},{"Id":"C","Text":"To prove every pot was buried deliberately"},{"Id":"D","Text":"To explain why records are unnecessary"}],"Answer":"A","Explanation":"Different possible causes of residues and scratches show why a single observation is insufficient."},{"Id":4,"Prompt":"The word \u0027these\u0027 in \u0027these relationships\u0027 refers to","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"Chemical substances"},{"Id":"B","Text":"Improved methods"},{"Id":"C","Text":"Golden ornaments"},{"Id":"D","Text":"Connections between vessels and their surroundings"}],"Answer":"D","Explanation":"The preceding sentence describes different uses of vessels in homes and shared spaces."},{"Id":5,"Prompt":"Where would this sentence best fit? \u2018Neither kind of evidence offers a complete story on its own.\u2019 Choose a marked location [A]\u2013[D] in the passage.","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"Location [A]"},{"Id":"B","Text":"Location [B]"},{"Id":"C","Text":"Location [C]"},{"Id":"D","Text":"Location [D]"}],"Answer":"B","Explanation":"Neither kind refers to chemical residues and surface marks. Position [B] follows their introduction; the next sentences explain why neither gives a definite conclusion alone."}]''
    WHERE [Id] = ''d3200000-0000-4000-8000-000000000003'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011195749_EnglishInterfaceAndForwardExams'
)
BEGIN
    EXEC(N'UPDATE [content].[ReadingSetVersions] SET [ItemsJson] = N''[{"Id":1,"Prompt":"What does the passage explain?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"Why all islands have identical habitats"},{"Id":"B","Text":"How isolation and area influence species communities"},{"Id":"C","Text":"Why birds cannot transport seeds"},{"Id":"D","Text":"How farmland increases every population"}],"Answer":"B","Explanation":"The passage connects distance, area, and habitat fragments on land."},{"Id":2,"Prompt":"Why can small populations be vulnerable?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"They always move too far"},{"Id":"B","Text":"They have too many habitats"},{"Id":"C","Text":"A single event may affect a large part of them"},{"Id":"D","Text":"They cannot use nesting sites"}],"Answer":"C","Explanation":"The example of a storm destroying a single nesting site illustrates this risk."},{"Id":3,"Prompt":"What does \u0027local disappearances\u0027 mean?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"Loss from a particular place"},{"Id":"B","Text":"Extinction across the whole planet"},{"Id":"C","Text":"Seasonal changes in weather"},{"Id":"D","Text":"The formation of new islands"}],"Answer":"A","Explanation":"The second paragraph distinguishes disappearance from one island from global extinction."},{"Id":4,"Prompt":"Why is a forest surrounded by farms compared to an island?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"Both are always surrounded by water"},{"Id":"B","Text":"Both have no birds"},{"Id":"C","Text":"Both contain only one habitat"},{"Id":"D","Text":"The surrounding area may prevent movement"}],"Answer":"D","Explanation":"A forest is functionally isolated for species unable to cross the surrounding farmland."},{"Id":5,"Prompt":"What is implied about wildlife corridors?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"They benefit every species equally"},{"Id":"B","Text":"Their usefulness depends on the organism"},{"Id":"C","Text":"They always eliminate local loss"},{"Id":"D","Text":"They are unrelated to movement"}],"Answer":"B","Explanation":"The usefulness of a corridor depends on how the particular species moves."}]''
    WHERE [Id] = ''d3200000-0000-4000-8000-000000000004'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011195749_EnglishInterfaceAndForwardExams'
)
BEGIN
    EXEC(N'UPDATE [content].[ReadingSetVersions] SET [ItemsJson] = N''[{"Id":1,"Prompt":"What is the passage mainly concerned with?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"Using lake sediment to investigate past environments"},{"Id":"B","Text":"Predicting the exact date of future fires"},{"Id":"C","Text":"Removing all sediment from lakes"},{"Id":"D","Text":"Showing that pollen is useless"}],"Answer":"A","Explanation":"The passage explains how layers reveal environmental history and the limits of interpretation."},{"Id":2,"Prompt":"What is generally true of deeper sediment?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"It contains no pollen"},{"Id":"B","Text":"It is always grass"},{"Id":"C","Text":"It is older"},{"Id":"D","Text":"It has no environmental value"}],"Answer":"C","Explanation":"The first paragraph states that deeper material is generally older."},{"Id":3,"Prompt":"Why might an increase in grass pollen be difficult to interpret?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"Grass never produces pollen"},{"Id":"B","Text":"More than one process could explain it"},{"Id":"C","Text":"Trees cannot grow near lakes"},{"Id":"D","Text":"All pollen has the same age"}],"Answer":"B","Explanation":"Climate change and human land clearing can produce the same pattern."},{"Id":4,"Prompt":"The word \u0027indicator\u0027 is closest in meaning to","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"instruction"},{"Id":"B","Text":"container"},{"Id":"C","Text":"obstacle"},{"Id":"D","Text":"sign"}],"Answer":"D","Explanation":"Indicator means a sign or clue about past conditions."},{"Id":5,"Prompt":"Why does the author contrast sediment with a precise diary?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"To claim scientists should use diaries only"},{"Id":"B","Text":"To emphasize that interpretation involves uncertainty"},{"Id":"C","Text":"To argue that deeper layers are always younger"},{"Id":"D","Text":"To dismiss historical records"}],"Answer":"B","Explanation":"Uncertainty about dates and causes prevents reading sediment as a precise diary."}]''
    WHERE [Id] = ''d3200000-0000-4000-8000-000000000005'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011195749_EnglishInterfaceAndForwardExams'
)
BEGIN
    EXEC(N'UPDATE [content].[ReadingSetVersions] SET [ItemsJson] = N''[{"Id":1,"Prompt":"Fill in the missing letters.","Prefix":"peo","MissingLetters":3,"Options":[],"Answer":"ple","Explanation":"people: the plural subject of recall."},{"Id":2,"Prompt":"Fill in the missing letters.","Prefix":"eve","MissingLetters":3,"Options":[],"Answer":"nts","Explanation":"events: the things that are recalled."},{"Id":3,"Prompt":"Fill in the missing letters.","Prefix":"bra","MissingLetters":3,"Options":[],"Answer":"ins","Explanation":"brains: a plural noun following their."},{"Id":4,"Prompt":"Fill in the missing letters.","Prefix":"sto","MissingLetters":3,"Options":[],"Answer":"red","Explanation":"stored: describes details previously kept in memory."},{"Id":5,"Prompt":"Fill in the missing letters.","Prefix":"wi","MissingLetters":2,"Options":[],"Answer":"th","Explanation":"with: the pattern is combine A with B."},{"Id":6,"Prompt":"Fill in the missing letters.","Prefix":"know","MissingLetters":5,"Options":[],"Answer":"ledge","Explanation":"knowledge: a noun following the adjective current."},{"Id":7,"Prompt":"Fill in the missing letters.","Prefix":"memo","MissingLetters":4,"Options":[],"Answer":"ries","Explanation":"memories: the subject of change."},{"Id":8,"Prompt":"Fill in the missing letters.","Prefix":"cha","MissingLetters":3,"Options":[],"Answer":"nge","Explanation":"change: the base-form verb agrees with plural memories."},{"Id":9,"Prompt":"Fill in the missing letters.","Prefix":"ea","MissingLetters":2,"Options":[],"Answer":"ch","Explanation":"each: refers to every new experience."},{"Id":10,"Prompt":"Fill in the missing letters.","Prefix":"exper","MissingLetters":5,"Options":[],"Answer":"ience","Explanation":"experience: a singular noun following new."}]''
    WHERE [Id] = ''da1689ce-4f1a-506f-3631-49bb772f5b1a'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011195749_EnglishInterfaceAndForwardExams'
)
BEGIN
    EXEC(N'UPDATE [content].[ReadingSetVersions] SET [ItemsJson] = N''[{"Id":1,"Prompt":"Fill in the missing letters.","Prefix":"lea","MissingLetters":3,"Options":[],"Answer":"ves","Explanation":"leaves: the plural noun for the parts of a tree that provide shade."},{"Id":2,"Prompt":"Fill in the missing letters.","Prefix":"sh","MissingLetters":3,"Options":[],"Answer":"ade","Explanation":"shade: the object of provide."},{"Id":3,"Prompt":"Fill in the missing letters.","Prefix":"sum","MissingLetters":3,"Options":[],"Answer":"mer","Explanation":"summer: a season following during."},{"Id":4,"Prompt":"Fill in the missing letters.","Prefix":"wa","MissingLetters":3,"Options":[],"Answer":"ter","Explanation":"water: the subject of evaporates."},{"Id":5,"Prompt":"Fill in the missing letters.","Prefix":"fr","MissingLetters":2,"Options":[],"Answer":"om","Explanation":"from: identifies the starting point of evaporation."},{"Id":6,"Prompt":"Fill in the missing letters.","Prefix":"open","MissingLetters":4,"Options":[],"Answer":"ings","Explanation":"openings: a plural noun modified by tiny."},{"Id":7,"Prompt":"Fill in the missing letters.","Prefix":"co","MissingLetters":3,"Options":[],"Answer":"ols","Explanation":"cools: agrees with the singular subject water."},{"Id":8,"Prompt":"Fill in the missing letters.","Prefix":"surro","MissingLetters":6,"Options":[],"Answer":"unding","Explanation":"surrounding: an adjective modifying air."},{"Id":9,"Prompt":"Fill in the missing letters.","Prefix":"throu","MissingLetters":5,"Options":[],"Answer":"ghout","Explanation":"throughout: means during the whole period."},{"Id":10,"Prompt":"Fill in the missing letters.","Prefix":"d","MissingLetters":2,"Options":[],"Answer":"ay","Explanation":"day: a time noun used with the."}]''
    WHERE [Id] = ''e108ef5a-f337-46e1-a5ef-73e01ba3fa08'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011195749_EnglishInterfaceAndForwardExams'
)
BEGIN
    EXEC(N'UPDATE [content].[ReadingSetVersions] SET [ItemsJson] = N''[{"Id":1,"Prompt":"What distinction is central to the passage?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"Printed versus digital notes"},{"Id":"B","Text":"Familiarity versus unaided recall"},{"Id":"C","Text":"Morning versus evening study"},{"Id":"D","Text":"Science versus history lessons"}],"Answer":"B","Explanation":"The first paragraph distinguishes familiarity from recalling without help."},{"Id":2,"Prompt":"Why should learners check answers after retrieval?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"To avoid all future effort"},{"Id":"B","Text":"To memorize page numbers"},{"Id":"C","Text":"To identify and correct gaps"},{"Id":"D","Text":"To make sessions longer"}],"Answer":"C","Explanation":"Comparing an answer with its source reveals gaps and errors."},{"Id":3,"Prompt":"What does \u0027accessible\u0027 mean in the final paragraph?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"Available to recall"},{"Id":"B","Text":"Printed in large type"},{"Id":"C","Text":"Free to purchase"},{"Id":"D","Text":"Located nearby"}],"Answer":"A","Explanation":"The phrase describes information remaining available in memory over time."},{"Id":4,"Prompt":"What can be inferred about an easy study session?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"It must involve retrieval"},{"Id":"B","Text":"It guarantees long-term learning"},{"Id":"C","Text":"It requires no feedback"},{"Id":"D","Text":"It may give a misleading impression of learning"}],"Answer":"D","Explanation":"Ease and familiarity alone do not demonstrate lasting learning."},{"Id":5,"Prompt":"How is the second paragraph organized?","Prefix":"","MissingLetters":0,"Options":[{"Id":"A","Text":"A historical event followed by dates"},{"Id":"B","Text":"A benefit followed by a condition for its success"},{"Id":"C","Text":"Two unrelated definitions"},{"Id":"D","Text":"A list of examination rules"}],"Answer":"B","Explanation":"The author explains the benefit of identifying gaps, then adds the need for accurate feedback."}]''
    WHERE [Id] = ''eb7aaaa9-007d-5974-fd3d-e2245160047b'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011195749_EnglishInterfaceAndForwardExams'
)
BEGIN
    EXEC(N'UPDATE [content].[ReadingSetVersions] SET [ItemsJson] = N''[{"Id":1,"Prompt":"Fill in the missing letters.","Prefix":"ra","MissingLetters":2,"Options":[],"Answer":"in","Explanation":"rain: the subject of falls."},{"Id":2,"Prompt":"Fill in the missing letters.","Prefix":"so","MissingLetters":2,"Options":[],"Answer":"me","Explanation":"some: indicates an amount of water."},{"Id":3,"Prompt":"Fill in the missing letters.","Prefix":"mo","MissingLetters":3,"Options":[],"Answer":"ves","Explanation":"moves: agrees with singular water."},{"Id":4,"Prompt":"Fill in the missing letters.","Prefix":"sm","MissingLetters":3,"Options":[],"Answer":"all","Explanation":"small: modifies spaces."},{"Id":5,"Prompt":"Fill in the missing letters.","Prefix":"bet","MissingLetters":4,"Options":[],"Answer":"ween","Explanation":"between: describes the position among rocks."},{"Id":6,"Prompt":"Fill in the missing letters.","Prefix":"wh","MissingLetters":3,"Options":[],"Answer":"ere","Explanation":"where: refers to the location described by rocks."},{"Id":7,"Prompt":"Fill in the missing letters.","Prefix":"lay","MissingLetters":3,"Options":[],"Answer":"ers","Explanation":"layers: a noun following underground."},{"Id":8,"Prompt":"Fill in the missing letters.","Prefix":"col","MissingLetters":4,"Options":[],"Answer":"lect","Explanation":"collect: agrees with plural layers."},{"Id":9,"Prompt":"Fill in the missing letters.","Prefix":"st","MissingLetters":3,"Options":[],"Answer":"ore","Explanation":"store: collect and store are parallel verbs."},{"Id":10,"Prompt":"Fill in the missing letters.","Prefix":"valu","MissingLetters":4,"Options":[],"Answer":"able","Explanation":"valuable: an adjective modifying resource."}]''
    WHERE [Id] = ''fc4dc8ce-ffd7-3253-d7be-5daa2263a65a'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011195749_EnglishInterfaceAndForwardExams'
)
BEGIN
    EXEC(N'UPDATE [learning].[WritingPaths] SET [Title] = N''Write an Email — Foundation''
    WHERE [Id] = ''c1100000-0000-4000-8000-000000000001'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011195749_EnglishInterfaceAndForwardExams'
)
BEGIN
    EXEC(N'UPDATE [learning].[WritingPaths] SET [Title] = N''Academic Discussion — Foundation''
    WHERE [Id] = ''c2100000-0000-4000-8000-000000000001'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011195749_EnglishInterfaceAndForwardExams'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261011195749_EnglishInterfaceAndForwardExams', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011200741_VersionedExamSessions'
)
BEGIN
    DROP INDEX [IX_BankSessions_UserId_Mode_Form] ON [learning].[BankSessions];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011200741_VersionedExamSessions'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_BankSessions_UserId_Mode_Form_Edition] ON [learning].[BankSessions] ([UserId], [Mode], [Form], [Edition]) WHERE [CompletedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011200741_VersionedExamSessions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261011200741_VersionedExamSessions', N'10.0.12');
END;

COMMIT;
GO


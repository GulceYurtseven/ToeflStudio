using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using ToeflExam.Application.Reading;
using ToeflExam.Domain.Content;
using ToeflExam.Infrastructure.Persistence.Entities;

namespace ToeflExam.Infrastructure.Persistence;

// Original authored practice sets, not copied ETS questions. New editions require new IDs/version.
internal static class ReadingSeed
{
    /// <summary>Defines the model mapping, constraints or deterministic seed data for this content.</summary>
    public static void Configure(ModelBuilder b)
    {
        Cloze(1, "Trees in the city",
            "City trees influence the temperature of the streets around them.",
            "Their leaves provide shade during summer while water evaporates from tiny openings and cools the surrounding air throughout the day.",
            "The effect is strongest where several trees grow together. However, trees need enough space for their roots and a reliable supply of water. Planners therefore consider soil conditions as well as appearance when choosing which species to plant along a busy street.",
            ["leaves: the plural noun for the parts of a tree that provide shade.", "shade: the object of provide.", "summer: a season following during.", "water: the subject of evaporates.", "from: identifies the starting point of evaporation.", "openings: a plural noun modified by tiny.", "cools: agrees with the singular subject water.", "surrounding: an adjective modifying air.", "throughout: means during the whole period.", "day: a time noun used with the."]);
        Cloze(2, "How memories change",
            "Human memory is an active process rather than a perfect recording.",
            "When people recall events their brains combine stored details with current knowledge so memories sometimes change after each new experience.",
            "This does not mean that every memory is unreliable. Repeated practice can strengthen information that is useful, such as a route to work. Nevertheless, two witnesses may remember the same scene differently because they noticed different details and later discussed the event with other people.",
            ["people: the plural subject of recall.", "events: the things that are recalled.", "brains: a plural noun following their.", "stored: describes details previously kept in memory.", "with: the pattern is combine A with B.", "knowledge: a noun following the adjective current.", "memories: the subject of change.", "change: the base-form verb agrees with plural memories.", "each: refers to every new experience.", "experience: a singular noun following new."]);
        Cloze(3, "Water below the surface",
            "Much of the water people use is stored beneath the ground.",
            "After rain falls some water moves through small spaces between rocks where underground layers slowly collect and store this valuable resource.",
            "These layers can supply wells even during dry weather. Yet the stored water is not unlimited. If people pump it out faster than rainfall replaces it, the water level drops. Understanding this balance helps communities decide how much water they can safely use for farming and household needs.",
            ["rain: the subject of falls.", "some: indicates an amount of water.", "moves: agrees with singular water.", "small: modifies spaces.", "between: describes the position among rocks.", "where: refers to the location described by rocks.", "layers: a noun following underground.", "collect: agrees with plural layers.", "store: collect and store are parallel verbs.", "valuable: an adjective modifying resource."]);
        Cloze(4, "Signals in a bee colony",
            "Honeybees share information about food with other members of their colony.",
            "A worker returns home after finding flowers and performs movements that indicate direction while other bees watch and receive useful information.",
            "The pattern gives nearby workers clues about where they should search. The message is not a map with exact coordinates. Wind, the quality of the food, and the distance from the colony can affect a bee's journey. Researchers observe repeated trips to understand how the colony responds to changing conditions.",
            ["worker: a singular noun following a.", "home: the destination after returns.", "finding: an -ing form after the preposition after.", "and: connects two actions.", "movements: the object of performs.", "indicate: agrees with the plural subject movements.", "while: connects simultaneous actions.", "bees: a plural noun following other.", "and: connects watch and receive.", "useful: modifies information."]);
        Cloze(5, "Learning from old objects",
            "Archaeologists use ordinary objects to investigate how people lived in the past.",
            "Broken pottery often reveals more about daily life than expensive objects because common tools reflect habits shared by many ordinary people.",
            "A cooking pot may contain traces of food, while worn surfaces can show how an object was handled. Researchers record where each piece was found before removing it. An object loses part of its value as evidence when the relationship between it and the surrounding materials is unknown.",
            ["pottery: a collective noun modified by broken.", "reveals: agrees with singular pottery.", "about: the pattern is reveal information about.", "life: part of the phrase daily life.", "expensive: an adjective modifying objects.", "because: introduces the reason.", "tools: a plural noun modified by common.", "habits: the object of reflect.", "by: identifies who shares something in shared by.", "ordinary: an adjective modifying people."]);

        Add("daily-life", 1, "Library pickup notice", "notice",
            "LIBRARY NOTICE\nReserved books are now collected from the ground-floor desk, not the second-floor office. Bring your student card. Items are held for three working days after the email notification. If you cannot collect yours in time, reply to the email before the deadline to request an extension.",
            Q(1, "Where should students collect reserved books?", ["At the ground-floor desk", "At the second-floor office", "At the main entrance", "At their department"], 0, "The notice explicitly changes the collection point to the ground-floor desk."),
            Q(2, "What should a student do if unable to collect a book on time?", ["Reserve another copy", "Call after three days", "Reply before the deadline", "Visit the old office"], 2, "Students must reply to the email before the deadline to request an extension."));
        Add("daily-life", 2, "A change to the workshop", "email",
            "To: Photography Club\nSubject: Saturday workshop\nHi everyone,\nBecause the forecast predicts heavy rain, our outdoor photography session will move to Studio B. The starting time is still 10 a.m. Please bring three photos you have already taken; we will discuss composition instead of taking new pictures. Anyone who borrowed a club camera should return it on Monday, as originally planned.\nMaya",
            Q(1, "Why was this email sent?", ["To cancel the club", "To explain a change of activity", "To request camera repairs", "To announce a new starting time"], 1, "The weather changes the location and activity, but not the starting time."),
            Q(2, "What should participants bring?", ["Raincoats", "New cameras", "Printed schedules", "Three existing photos"], 3, "Participants are asked to bring three photos they have already taken."),
            Q(3, "What remains unchanged?", ["The return date for cameras", "The workshop location", "The outdoor activity", "The need to take new pictures"], 0, "Originally planned confirms that cameras are still due on Monday."));
        Add("daily-life", 3, "Lunch at the campus café", "menu",
            "CAMPUS CAFÉ — LUNCH\nSoup: $4 | Sandwich: $7 | Salad: $6\nLunch deal: any sandwich + soup for $9, weekdays 11:30–14:00 only.\nTea is $2, or free with a lunch deal when you show a student card.\nAll items can be ordered separately after 14:00 at their regular prices.",
            Q(1, "How much does a sandwich and soup cost at 12:00 on Tuesday?", ["$7", "$11", "$9", "$13"], 2, "The weekday deal includes both items for $9 between 11:30 and 14:00."),
            Q(2, "Who can receive free tea?", ["Everyone ordering soup", "A student showing a card with a lunch deal", "Anyone visiting after 14:00", "Anyone buying a salad"], 1, "Free tea requires both a lunch deal and a student card."));
        Add("daily-life", 4, "A study group conversation", "messages",
            "Nora 16:05: Can we meet in Room 12 at five?\nLeo 16:08: It is booked until six. The café is noisy today.\nNora 16:10: Let's use the library courtyard if it stays dry. Otherwise, meet by the library entrance and we will find a space inside.\nLeo 16:12: Great. I'll bring the printed charts. Can you share the article online before we meet?\nNora 16:13: Done — check the group folder.",
            Q(1, "Where will the students first meet if it rains?", ["In Room 12", "In the café", "In the courtyard", "By the library entrance"], 3, "Otherwise refers to the condition in which the weather is not dry."),
            Q(2, "What has Nora already done?", ["Uploaded the article", "Printed the charts", "Booked Room 12", "Found an indoor room"], 0, "Done and group folder show that the article has been shared online."));
        Add("daily-life", 5, "Volunteer repair day", "social-post",
            "COMMUNITY REPAIR DAY — Sunday, 13 October\nBring one small household item to the community hall between 10:00 and 15:00. Volunteers will help you try to repair it. Advice is free, but replacement parts are paid for by the owner. No previous repair skills are needed. Booking is recommended; people without a reservation will be helped only if time allows. Please do not bring large appliances.",
            Q(1, "What is the main purpose of the post?", ["To sell household items", "To recruit appliance manufacturers", "To invite people to a repair event", "To offer free replacement parts"], 2, "The place, time, and participation details invite people to a repair event."),
            Q(2, "What might a participant have to pay for?", ["Volunteer advice", "Replacement parts", "An entry ticket", "A reservation"], 1, "Advice is free, but the owner pays for replacement parts."),
            Q(3, "What can be inferred about attending without booking?", ["Repair is guaranteed", "Only experts can attend", "Large items are accepted", "Help may not be available"], 3, "People without reservations receive help only if time permits."));

        Add("academic-passage", 1, "Urban trees and local temperatures", "article",
            "Cities are often warmer than nearby rural areas because roads and buildings absorb solar energy and release it slowly. Trees can reduce this local warming in two ways. Their leaves shade surfaces, preventing some energy from reaching the ground. Trees also release water through their leaves; as this water evaporates, it takes heat from the surrounding air.\n\nThe benefits are unevenly distributed. A single tree beside a wide road provides less continuous shade than a connected row of trees. Moreover, a species that grows quickly may require more water than the local climate can supply. Planting without considering these constraints can lead to trees dying before they provide substantial shade.\n\nFor this reason, some urban planners combine tree planting with changes to the surfaces beneath them. Permeable paving allows rainfall to reach roots instead of running directly into drains. Such planning treats trees as part of a larger system. The aim is not merely to increase the number planted, but to create conditions in which they can survive and cool streets for decades.",
            Q(1, "What is the passage mainly about?", ["Why rural areas need paved roads", "How trees cool cities and what supports their success", "Why cities should stop planting trees", "How to measure the age of a tree"], 1, "The passage combines cooling mechanisms, limitations, and long-term planting design."),
            Q(2, "How does evaporation help cool the air?", ["It prevents all rainfall", "It releases stored solar energy", "It takes heat from the air", "It increases road temperature"], 2, "The first paragraph explains that evaporating water takes heat from surrounding air."),
            Q(3, "The word 'constraints' is closest in meaning to", ["celebrations", "measurements", "predictions", "limitations"], 3, "The phrase refers to conditions affecting success, such as water needs and climate."),
            Q(4, "Why does the author mention permeable paving?", ["To give an example of supporting tree survival", "To argue that shade is unnecessary", "To describe a rural farming method", "To show that roots should remain dry"], 0, "Permeable surfaces illustrate planning trees as part of the surrounding system."),
            Q(5, "What can be inferred about counting newly planted trees?", ["It always measures cooling accurately", "It is sufficient for planning", "It does not alone show long-term benefits", "It proves that trees will survive"], 2, "The final paragraph emphasizes conditions for survival rather than the number planted."));
        Add("academic-passage", 2, "When retrieval improves learning", "article",
            "Students often prepare for an examination by reading their notes repeatedly. This can make the material feel familiar, but familiarity is not the same as being able to recall an idea without help. An alternative approach asks students to close their notes and attempt to explain what they remember. This process is known as retrieval practice.\n\nRetrieval can be useful even when the first attempt is incomplete. After answering, a learner checks the original material and identifies gaps. The comparison provides a clearer guide for further study than the general feeling that a page looks familiar. However, feedback matters: repeatedly producing an incorrect answer without checking it may strengthen the error rather than correct it.\n\nSpacing these attempts over several days adds another challenge. Some forgetting occurs between sessions, so recall requires effort again. Although this can feel less comfortable than immediate repetition, it helps reveal whether knowledge remains accessible over time. Effective study therefore involves both attempting to retrieve information and using reliable feedback to refine it, rather than judging success solely by how easy a study session feels.",
            Q(1, "What distinction is central to the passage?", ["Printed versus digital notes", "Familiarity versus unaided recall", "Morning versus evening study", "Science versus history lessons"], 1, "The first paragraph distinguishes familiarity from recalling without help."),
            Q(2, "Why should learners check answers after retrieval?", ["To avoid all future effort", "To memorize page numbers", "To identify and correct gaps", "To make sessions longer"], 2, "Comparing an answer with its source reveals gaps and errors."),
            Q(3, "What does 'accessible' mean in the final paragraph?", ["Available to recall", "Printed in large type", "Free to purchase", "Located nearby"], 0, "The phrase describes information remaining available in memory over time."),
            Q(4, "What can be inferred about an easy study session?", ["It must involve retrieval", "It guarantees long-term learning", "It requires no feedback", "It may give a misleading impression of learning"], 3, "Ease and familiarity alone do not demonstrate lasting learning."),
            Q(5, "How is the second paragraph organized?", ["A historical event followed by dates", "A benefit followed by a condition for its success", "Two unrelated definitions", "A list of examination rules"], 1, "The author explains the benefit of identifying gaps, then adds the need for accurate feedback."));
        Add("academic-passage", 3, "The evidence inside pottery", "article",
            "A broken cooking pot may appear less impressive than a golden ornament, yet it can provide rich evidence about everyday life. Pottery survives in many environments, and even small fragments can preserve traces of how a vessel was made and used. Researchers examine both the material itself and the place where it was discovered.\n\nChemical analysis may reveal residues left by food or drink. Marks on the surface can suggest repeated contact with fire or particular tools. Neither kind of evidence offers a complete story on its own. A substance could have entered the pot after it was discarded, while a scratch might have occurred during burial. Researchers therefore compare several independent observations before proposing an explanation.\n\nLocation is equally important. Similar vessels found in homes and in communal spaces may have served different purposes. If an object is removed without a record of its surroundings, these relationships can be lost. Careful documentation makes it possible for later researchers to test earlier interpretations using improved methods. Preserving information about an object's context can consequently be as valuable as preserving the object itself.",
            Q(1, "What is the main idea?", ["Gold is always more informative than pottery", "Chemical tests eliminate uncertainty", "Pottery and its context together reveal past activities", "Broken objects should be discarded"], 2, "Both the material and the context of its discovery provide evidence about past lives."),
            Q(2, "What might chemical analysis reveal?", ["The name of the pot's owner", "Food or drink residues", "The exact date of every scratch", "The value of gold"], 1, "The second paragraph explicitly mentions food and drink residues."),
            Q(3, "Why are possible alternative explanations mentioned?", ["To show why several observations are needed", "To reject all chemical testing", "To prove every pot was buried deliberately", "To explain why records are unnecessary"], 0, "Different possible causes of residues and scratches show why a single observation is insufficient."),
            Q(4, "The word 'these' in 'these relationships' refers to", ["Chemical substances", "Improved methods", "Golden ornaments", "Connections between vessels and their surroundings"], 3, "The preceding sentence describes different uses of vessels in homes and shared spaces."),
            Q(5, "What can be inferred about careful records?", ["They prevent new interpretations", "They are useful only to the first researcher", "They allow evidence to be reconsidered later", "They replace the need to preserve objects"], 2, "The final paragraph explains how new methods can test earlier interpretations."));
        Add("academic-passage", 4, "Why some islands have fewer species", "article",
            "The number of species on an island reflects a changing balance between arrivals and local disappearances. An island close to a mainland is generally easier for organisms to reach than a distant one. Seeds may travel on wind or birds, while some animals can cross short stretches of water. Distance therefore influences the rate at which new populations become established.\n\nArea also matters. Larger islands often contain more types of habitat and can support larger populations. Small populations are more vulnerable to chance events, such as a storm destroying the only nesting site used by a species. A disappearance from one island does not necessarily mean that the species has become extinct everywhere.\n\nThese ideas have been applied to habitat fragments on land. A forest surrounded by farms is not literally an island, but species that cannot cross farmland may experience it as one. Creating connections between fragments can make movement easier. Nevertheless, a corridor that benefits one species may not help another. Conservation planning must consider how particular organisms move, rather than assuming that every strip of vegetation provides the same connection.",
            Q(1, "What does the passage explain?", ["Why all islands have identical habitats", "How isolation and area influence species communities", "Why birds cannot transport seeds", "How farmland increases every population"], 1, "The passage connects distance, area, and habitat fragments on land."),
            Q(2, "Why can small populations be vulnerable?", ["They always move too far", "They have too many habitats", "A single event may affect a large part of them", "They cannot use nesting sites"], 2, "The example of a storm destroying a single nesting site illustrates this risk."),
            Q(3, "What does 'local disappearances' mean?", ["Loss from a particular place", "Extinction across the whole planet", "Seasonal changes in weather", "The formation of new islands"], 0, "The second paragraph distinguishes disappearance from one island from global extinction."),
            Q(4, "Why is a forest surrounded by farms compared to an island?", ["Both are always surrounded by water", "Both have no birds", "Both contain only one habitat", "The surrounding area may prevent movement"], 3, "A forest is functionally isolated for species unable to cross the surrounding farmland."),
            Q(5, "What is implied about wildlife corridors?", ["They benefit every species equally", "Their usefulness depends on the organism", "They always eliminate local loss", "They are unrelated to movement"], 1, "The usefulness of a corridor depends on how the particular species moves."));
        Add("academic-passage", 5, "A record written in lake sediment", "article",
            "Material gradually settles on the bottoms of lakes, forming layers of sediment. These layers may contain pollen, mineral particles, and remains of small organisms. By removing a narrow column of sediment, researchers can examine a sequence in which deeper material is generally older than material near the surface. The column acts as a partial record of the surrounding environment.\n\nPollen offers clues about plants that grew nearby. If pollen from trees becomes less common while grass pollen increases, the landscape may have become more open. However, the change could reflect either a shift in climate or human activities such as clearing land. A single indicator rarely identifies the cause with certainty.\n\nResearchers strengthen interpretations by comparing pollen with other evidence, including charcoal particles and historical records. Charcoal may indicate burning, though it does not reveal by itself whether the fire was natural or deliberately started. Establishing the age of each layer also involves uncertainty. Rather than treating sediment as a precise diary, scientists combine several lines of evidence to build and revise explanations of environmental change over time.",
            Q(1, "What is the passage mainly concerned with?", ["Using lake sediment to investigate past environments", "Predicting the exact date of future fires", "Removing all sediment from lakes", "Showing that pollen is useless"], 0, "The passage explains how layers reveal environmental history and the limits of interpretation."),
            Q(2, "What is generally true of deeper sediment?", ["It contains no pollen", "It is always grass", "It is older", "It has no environmental value"], 2, "The first paragraph states that deeper material is generally older."),
            Q(3, "Why might an increase in grass pollen be difficult to interpret?", ["Grass never produces pollen", "More than one process could explain it", "Trees cannot grow near lakes", "All pollen has the same age"], 1, "Climate change and human land clearing can produce the same pattern."),
            Q(4, "The word 'indicator' is closest in meaning to", ["instruction", "container", "obstacle", "sign"], 3, "Indicator means a sign or clue about past conditions."),
            Q(5, "Why does the author contrast sediment with a precise diary?", ["To claim scientists should use diaries only", "To emphasize that interpretation involves uncertainty", "To argue that deeper layers are always younger", "To dismiss historical records"], 1, "Uncertainty about dates and causes prevents reading sediment as a precise diary."));

        void Add(string module, int n, string title, string format, string text, params ReadingItem[] items)
        {
            var group = module == "complete-words" ? 1 : module == "daily-life" ? 2 : 3;
            b.Entity<ReadingSetVersion>().HasData(new ReadingSetVersion { Id = Guid.Parse($"d{group}100000-0000-4000-8000-{n:D12}"), Module = module,
                Version = 1, Position = n, PublicationStatus = PublicationStatus.Published, Title = title, Format = format, Text = text,
                ItemsJson = JsonSerializer.Serialize(items) });
            // Preserve all v1 IDs for existing runs. New academic runs use the complete v2 path.
            if (module == "academic-passage")
            {
                if (n == 3)
                {
                    text = text.Replace("Chemical analysis", "[A] Chemical analysis", StringComparison.Ordinal)
                        .Replace("Neither kind of evidence offers a complete story on its own. ", "[B] ", StringComparison.Ordinal)
                        .Replace("Researchers therefore", "[C] Researchers therefore", StringComparison.Ordinal)
                        .Replace("Location is equally important.", "[D] Location is equally important.", StringComparison.Ordinal);
                    items = items[..4].Append(Q(5,
                        "Where would this sentence best fit? ‘Neither kind of evidence offers a complete story on its own.’ Choose a marked location [A]–[D] in the passage.",
                        ["Location [A]", "Location [B]", "Location [C]", "Location [D]"], 1,
                        "Neither kind refers to chemical residues and surface marks. Position [B] follows their introduction; the next sentences explain why neither gives a definite conclusion alone.")).ToArray();
                }
                b.Entity<ReadingSetVersion>().HasData(new ReadingSetVersion {
                    Id = Guid.Parse($"d3200000-0000-4000-8000-{n:D12}"), Module = module,
                    Version = 2, Position = n, PublicationStatus = PublicationStatus.Published,
                    Title = title, Format = format, Text = text, ItemsJson = JsonSerializer.Serialize(items) });
            }
        }
        void Cloze(int n, string title, string first, string middle, string last, string[] explanations)
        {
            var words = middle.Split(' '); var items = new List<ReadingItem>();
            for (var i = 1; items.Count < 10; i += 2)
            {
                var word = Regex.Match(words[i], "^[A-Za-z]+").Value;
                var keep = word.Length / 2;
                var id = items.Count + 1;
                items.Add(new(id, "Fill in the missing letters.", word[..keep], word.Length - keep, [], word[keep..], explanations[id - 1]));
                words[i] = "{{" + id + "}}" + words[i][word.Length..];
            }
            Add("complete-words", n, title, "paragraph", first + " " + string.Join(" ", words) + " " + last, items.ToArray());
        }
    }
    /// <summary>Builds an authored Reading question with its options, answer and explanation.</summary>
    private static ReadingItem Q(int id, string prompt, string[] options, int answer, string explanation)
        => new(id, prompt, "", 0, options.Select((text, i) => new ReadingOption(((char)('A' + i)).ToString(), text)).ToArray(),
            ((char)('A' + answer)).ToString(), explanation);
}

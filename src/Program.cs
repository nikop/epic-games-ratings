using EpicRatingsUpdater;
using EpicRatingsUpdater.GameDatabase;
using EpicRatingsUpdater.Markdown;

using System.Text.Json;

// Configs
var path = new DirectoryInfo(".").FullName;
var index = new Dictionary<string, string>();

var forceUpdateNames = args.Any(x => x == "--update-names");
var skipRatingsUpdate = args.Any(x => x == "--skip-ratings");
var skipAchievementsUpdate = args.Any(x => x == "--skip-achievement");
var skipOffers = args.Any(x => x == "--skip-offers");
var skipItems = args.Any(x => x == "--skip-items");

#if DEBUG
skipOffers = true;
skipItems = true;
skipRatingsUpdate = true;
#endif

var ratingsCutOffNew = DateTimeOffset.UtcNow.AddDays(-30);
var dirDb = Path.Combine(path, "db");
var dirGames = Path.Combine(path, "games");
var dirItems = Path.Combine(path, "items-tracker", "database");
var dirOffers = Path.Combine(path, "offers-tracker", "database");

var gameIndex = new JsonIndexDb<GameDbItem>(dirDb);

if (!Directory.Exists(dirItems) || !Directory.Exists(dirOffers))
{
    Console.WriteLine("Databases missing");
    Environment.Exit(1);
    return;
}

if (forceUpdateNames)
    Console.WriteLine("Updating names");

// Known namespaces
var namespaces = new Dictionary<string, NamespaceDef>{};

NamespaceDef AddNamespace(string ns)
{
    if (namespaces.ContainsKey(ns))
    {
        return namespaces[ns];
    }

    var def = new NamespaceDef
    {
        Namespace = ns,
    };

    namespaces.Add(ns, def);

    return def;
}

// Find namespaces from offers
if (!skipOffers)
{
    Console.WriteLine("Reading offers");

    var offersText = File.ReadAllText(Path.Combine(dirOffers, "namespaces.json"));
    var typeOffers = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(offersText);

    foreach (var i in typeOffers!)
    {
        var def = AddNamespace(i.Key);

        var item = await GetOrCreate(gameIndex, i.Key).ConfigureAwait(false);
        var requiresSave = false;

        List<EpicOffer> offers = new();
        EpicOffer? baseAppOffer = null;

        foreach (var v in i.Value)
        {
            var fileName = Path.Combine(dirOffers, "offers", $"{v}.json");

            try
            {
                var fileContent = await File.ReadAllTextAsync(fileName);
                var epicOffer = JsonSerializer.Deserialize<EpicOffer>(fileContent);

                if (epicOffer == null)
                    continue;

                offers.Add(epicOffer);

                if (epicOffer.categories.Any(x => x.path == "games/edition/base"))
                {
                    baseAppOffer ??= epicOffer;
                }
            }
            catch (Exception)
            {
            }
        }

        if (baseAppOffer != null)
        {
            if (item.Name != baseAppOffer.title)
            {
                requiresSave = true;
                item.Name = baseAppOffer.title;

                await gameIndex.RenameItem(item).ConfigureAwait(false);
            }
        }

        if (requiresSave)
        {
            await gameIndex.SaveItem(item).ConfigureAwait(false);
        }
    }
}

// Find namespaces from items
if (!skipItems)
{
    Console.WriteLine("Reading items");

    var itemsText = File.ReadAllText(Path.Combine(dirItems, "namespaces.json"));
    var typeItems = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(itemsText);

    foreach (var i in typeItems!)
    {
        AddNamespace(i.Key);
        var item = await GetOrCreate(gameIndex, i.Key).ConfigureAwait(false);
    }
}

async ValueTask<GameDbItem> GetOrCreate(JsonIndexDb<GameDbItem> gameIndex, string ns, string? name = null)
{
    var item = await gameIndex.GetItemByKey(ns).ConfigureAwait(false);

    if (item == null)
    {
        item = new GameDbItem
        {
            ID = ns,
            Name = name,
        };

        await gameIndex.SaveItem(item).ConfigureAwait(false);
    }

    return item;
}

var items = await gameIndex.GetAllItems().ConfigureAwait(false);

// Fetch ratings
if (!skipRatingsUpdate)
{
    var action = new UpdateRatings();
    await action.Run(items);
}

// Fetch achievements
if (!skipAchievementsUpdate)
{
    var action = new UpdateAchievements();
    await action.Run(items);
}

// Only games with rating
var filteredList = items.Where(x => x.Rating != null).ToList();

// Rating Ranking
MarkdownHelpers.RankItems(
    filteredList.OrderByDescending(x => x.Rating),
    x => x.Rating ?? 0,
    (x, rank) => x.Ranking_Rating = rank
);

// Popularity/Award Ranking
MarkdownHelpers.RankItems(
    filteredList.OrderByDescending(x => x.NumberOfAwardsMax),
    x => x.NumberOfAwardsMax ?? 0,
    (x, rank) => x.Ranking_Popularity = rank
);

MarkdownHelpers.RankItems(
    filteredList.OrderByDescending(x => x.NumberOfAwards),
    x => x.NumberOfAwards ?? 0,
    (x, rank) => x.Ranking_PopularitySum = rank
);

// EOS Achievements
MarkdownHelpers.RankItems(
    items.OrderByDescending(x => x.EOS_Progressed),
    x => x.EOS_Progressed,
    (x, rank) => x.Ranking_EOS_Progress = rank
);

MarkdownHelpers.RankItems(
    items.OrderByDescending(x => x.EOS_Completed_Percentage),
    x => x.EOS_Completed_Percentage,
    (x, rank) => x.Ranking_EOS_Completed = rank
);

MarkdownHelpers.RankItems(
    items.OrderByDescending(x => x.EOS_NewPlayers),
    x => x.EOS_NewPlayers,
    (x, rank) => x.Ranking_EOS_NewPlayers = rank
);

MarkdownHelpers.RankItems(
    items.OrderByDescending(x => x.EOS_NewCompleters),
    x => x.EOS_NewCompleters,
    (x, rank) => x.Ranking_EOS_NewCompleters = rank
);

foreach (var item in items)
{
    await gameIndex.SaveItem(item).ConfigureAwait(false);

    var name = gameIndex.Files[item.ID] + ".md";
    var fullName = Path.Combine(dirGames, name);

    Directory.CreateDirectory(Path.GetDirectoryName(fullName)!);

    var page = MarkdownHelpers.BuildMarkdownGamePage(item);

    await File.WriteAllTextAsync(fullName, page);
}

string GamesLink(GameDbItem item)
{
    return "games/" + gameIndex?.Files[item.ID].Replace(@"\", "/") + ".md";
}

var nameTable = new MarkdownTable<GameDbItem>()
    .AddColumn("Game", x => $"[{x.Name}]({GamesLink(x)})")
    .AddColumn("Rating", x => MarkdownHelpers.FormatRating(x.Rating))
    .AddColumn("Ranking", x => MarkdownHelpers.FormatRanking(x.Ranking_Rating))
    .AddColumn("Awards", x => MarkdownHelpers.FormatVotes(x.NumberOfAwardsMax))
    .AddColumn("Ranking", x => MarkdownHelpers.FormatRanking(x.Ranking_Popularity));

var ratingTable = new MarkdownTable<GameDbItem>()
    .AddColumn("#", x => MarkdownHelpers.FormatRanking(x.Ranking_Rating))
    .AddColumn("Game", x => $"[{x.Name}]({GamesLink(x)})")
    .AddColumn("Rating", x => MarkdownHelpers.FormatRating(x.Rating))
    .AddColumn("Awards", x => MarkdownHelpers.FormatVotes(x.NumberOfAwardsMax))
    .AddColumn("Popularity Ranking", x => MarkdownHelpers.FormatRanking(x.Ranking_Popularity));

var awardsTable = new MarkdownTable<GameDbItem>()
    .AddColumn("#", x => MarkdownHelpers.FormatRanking(x.Ranking_Popularity))
    .AddColumn("Game", x => $"[{x.Name}]({GamesLink(x)})")
    .AddColumn("Awards", x => MarkdownHelpers.FormatVotes(x.NumberOfAwardsMax))
    .AddColumn("Rating", x => MarkdownHelpers.FormatRating(x.Rating))
    .AddColumn("Rating Ranking", x => MarkdownHelpers.FormatRanking(x.Ranking_Rating));

var awardsSumTable = new MarkdownTable<GameDbItem>()
    .AddColumn("#", x => MarkdownHelpers.FormatRanking(x.Ranking_PopularitySum))
    .AddColumn("Game", x => $"[{x.Name}]({GamesLink(x)})")
    .AddColumn("Awards", x => MarkdownHelpers.FormatVotes(x.NumberOfAwards))
    .AddColumn("Rating", x => MarkdownHelpers.FormatRating(x.Rating))
    .AddColumn("Rating Ranking", x => MarkdownHelpers.FormatRanking(x.Ranking_Rating));

var eosTable = new MarkdownTable<GameDbItem>()
    .AddColumn("#", x => MarkdownHelpers.FormatRanking(x.Ranking_EOS_Progress))
    .AddColumn("Game", x => $"[{x.Name}]({GamesLink(x)})")
    .AddColumn("Progressed", x => MarkdownHelpers.FormatVotes(x.EOS_Progressed))
    .AddColumn("Completed", x => MarkdownHelpers.FormatVotes(x.EOS_Completed));

var eosNewPlayersTable = new MarkdownTable<GameDbItem>()
    .AddColumn("#", x => MarkdownHelpers.FormatRanking(x.Ranking_EOS_NewPlayers))
    .AddColumn("Game", x => $"[{x.Name}]({GamesLink(x)})")
    .AddColumn("New Players", x => MarkdownHelpers.FormatVotes(x.EOS_NewPlayers))
    .AddColumn("Total", x => MarkdownHelpers.FormatVotes(x.EOS_Progressed));

var eosCompletersTable = new MarkdownTable<GameDbItem>()
    .AddColumn("#", x => MarkdownHelpers.FormatRanking(x.Ranking_EOS_Completed))
    .AddColumn("Game", x => $"[{x.Name}]({GamesLink(x)})")
    .AddColumn("Completed", x => $"{MarkdownHelpers.FormatRating(x.EOS_Completed_Percentage)}%");

// Markdown
await File.WriteAllTextAsync(
    Path.Combine(path, "by_name.md"),
    nameTable.FormatTable(items.Where(x => x.Name != null).OrderBy(x => x.Name))
);
await File.WriteAllTextAsync(
    Path.Combine(path, "by_rating.md"), 
    ratingTable.FormatTable(filteredList.OrderByDescending(x => x.Rating).ThenBy(x => x.Name))
);
await File.WriteAllTextAsync(
    Path.Combine(path, "by_awards.md"),
    awardsTable.FormatTable(items.Where(x => x.NumberOfAwardsMax > 0).OrderByDescending(x => x.NumberOfAwardsMax).ThenBy(x => x.Name))
);
await File.WriteAllTextAsync(
    Path.Combine(path, "by_awards_sum.md"),
    awardsSumTable.FormatTable(items.Where(x => x.NumberOfAwards > 0).OrderByDescending(x => x.NumberOfAwards).ThenBy(x => x.Name))
);
await File.WriteAllTextAsync(
    Path.Combine(path, "new_games.md"),
    nameTable.FormatTable(items.Where(x => x.FirstSeen != null && ratingsCutOffNew < x.FirstSeen).OrderByDescending(x => x.FirstSeen).ThenBy(x => x.Name))
);

await File.WriteAllTextAsync(
    Path.Combine(path, "eos_achievers.md"),
    eosTable.FormatTable(items.Where(x => x.EOS_Progressed > 0).OrderByDescending(x => x.EOS_Progressed).ThenBy(x => x.Name))
);

await File.WriteAllTextAsync(
    Path.Combine(path, "eos_new_players.md"),
    eosNewPlayersTable.FormatTable(items.Where(x => x.EOS_NewPlayers > 0).OrderByDescending(x => x.EOS_NewPlayers).ThenBy(x => x.Name))
);

await File.WriteAllTextAsync(
    Path.Combine(path, "eos_completers.md"),
    eosCompletersTable.FormatTable(items.Where(x => x.EOS_Progressed > 0).OrderByDescending(x => x.EOS_Completed_Percentage).ThenBy(x => x.Name))
);

// Stats
await File.WriteAllTextAsync(
    Path.Combine(path, "stats.md"),
    MarkdownHelpers.BuildMarkdownStats(filteredList, items)
);

// Experiments
Directory.CreateDirectory("out/experimental");

var eosGames = new MarkdownTable<GameDbItem>()
    .AddColumn("Game", x => $"[{x.Name}]({GamesLink(x)})")
    .AddColumn("Total Achievements", x => MarkdownHelpers.FormatRanking(x.TotalAchievements))
    .AddColumn("Total XP", x => MarkdownHelpers.FormatRanking(x.TotalAchievementsXP));


var eosGamesSets = new MarkdownTable<GameDbItem>()
    .AddColumn("Game", x => $"[{x.Name}]({GamesLink(x)})")
    .AddColumn("Total Achievements", x => MarkdownHelpers.FormatRanking(x.TotalAchievements))
    .AddColumn("Total XP", x => MarkdownHelpers.FormatRanking(x.TotalAchievementsXP))
    .AddColumn("Sets", x => MarkdownHelpers.FormatRanking(x.AchievementSets.Count));

await File.WriteAllTextAsync(
    Path.Combine(path, "out/experimental/eos_total_achievements.md"),
    eosGames.FormatTable(items.Where(x => x.TotalAchievements > 0).OrderByDescending(x => x.TotalAchievements).ThenBy(x => x.Name))
);

await File.WriteAllTextAsync(
    Path.Combine(path, "out/experimental/eos_total_xp.md"),
    eosGames.FormatTable(items.Where(x => x.TotalAchievementsXP > 0).OrderByDescending(x => x.TotalAchievementsXP).ThenBy(x => x.Name))
);

// Is there any with multiple?
await File.WriteAllTextAsync(
    Path.Combine(path, "out/experimental/eos_num_sets.md"),
    eosGamesSets.FormatTable(items.Where(x => x.AchievementSets.Count > 1).OrderByDescending(x => x.AchievementSets.Count).ThenBy(x => x.Name))
);
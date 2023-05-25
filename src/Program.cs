using EpicRatingsUpdater;
using EpicRatingsUpdater.GameDatabase;
using EpicRatingsUpdater.Markdown;

using System.Text.Json;
using CliWrap;
using CliWrap.Buffered;

// Configs
var gitCmd = Cli.Wrap("git");

var path = new DirectoryInfo(".").FullName;
var index = new Dictionary<string, string>();

var skipCatalog = args.Any(x => x == "--skip-catalog");
var skipAppInfo = args.Any(x => x == "--skip-appinfo");
var skipRatingsUpdate = args.Any(x => x == "--skip-ratings");
var skipAchievementsUpdate = args.Any(x => x == "--skip-achievement");

#if DEBUG
skipCatalog = true;
skipAppInfo = true;
skipRatingsUpdate = true;
skipAchievementsUpdate = true;
#endif

var ratingsCutOffNew = DateTimeOffset.UtcNow.AddDays(-30);
var now = DateTimeOffset.UtcNow;
var dirDb = Path.Combine(path, "db");
var dirGames = Path.Combine(path, "games");

var globalDb = new GlobalDb();

var globalDbFile = Path.Combine(dirDb, "global.json");

if (File.Exists(globalDbFile))
{
    var text = await File.ReadAllTextAsync(globalDbFile);

    var data = JsonSerializer.Deserialize<GlobalDb>(text);

    if (data != null)
    {
        globalDb = data;
    }
}

var gameIndex = new GameDb(dirDb);

// Update Catalog (new games / titles)
if (!skipCatalog)
{
    var action = new UpdateCatalog();
    await action.Run(globalDb, gameIndex);
}

var items = await gameIndex.GetAllItems().ConfigureAwait(false);

// Fetch appInfo (store pages)
if (!skipAppInfo)
{
    var action = new UpdateAppInfo();
    await action.Run(globalDb, items);
}

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

//
foreach (var file in Directory.GetFiles("games", "*.md", SearchOption.AllDirectories))
{
    File.Delete(file);
}

try
{
    var f = JsonSerializer.Serialize(globalDb, new JsonSerializerOptions
    {
        WriteIndented = true,
    });

    await File.WriteAllTextAsync(globalDbFile, f);
}
catch (Exception ex)
{
    Console.WriteLine(ex);
}

foreach (var item in items)
{
    await gameIndex.SaveItem(item).ConfigureAwait(false);

    var name = gameIndex.Files[item.ID] + ".md";
    var fullName = Path.Combine(dirGames, name);

    Directory.CreateDirectory(Path.GetDirectoryName(fullName)!);

    var page = MarkdownHelpers.BuildMarkdownGamePage(item, RawLink(item));

    await File.WriteAllTextAsync(fullName, page);
}

await gitCmd.WithArguments(new[] { "add", "." }).WithValidation(CommandResultValidation.None).ExecuteAsync();
var res = await gitCmd.WithArguments(new[] { "commit", "-m", "Update DB" }).WithValidation(CommandResultValidation.None).ExecuteBufferedAsync();

foreach (var file in Directory.GetFiles("experimental", "*.md", SearchOption.AllDirectories))
{
    File.Delete(file);
}

Console.Error.WriteLine(res.StandardError);
Console.WriteLine(res.StandardOutput);

string RawLink(GameDbItem item)
{
    return "db/" + gameIndex?.Files[item.ID].Replace(@"\", "/") + ".json";
}

string GamesLink(GameDbItem item)
{
    return "games/" + gameIndex?.Files[item.ID].Replace(@"\", "/") + ".md";
}

var nameTable = new MarkdownTable<GameDbItem>()
    .AddColumn("Game", x => $"[{x.Name}]({GamesLink(x)})")
    .AddColumn("Rating", x => MarkdownHelpers.FormatRating(x.Rating))
    .AddColumn("Ranking", x => MarkdownHelpers.FormatRanking(x.Ranking_Rating))
    .AddColumn("Awards", x => MarkdownHelpers.FormatNumber(x.NumberOfAwardsMax))
    .AddColumn("Ranking", x => MarkdownHelpers.FormatRanking(x.Ranking_Popularity));

var nameDateTable = new MarkdownTable<GameDbItem>()
    .AddColumn("Game", x => $"[{x.Name}]({GamesLink(x)})")
    .AddColumn("Release Date", x => MarkdownHelpers.FormatDate(x.Store.ReleaseDate))
    .AddColumn("Achievements", x => x.TotalAchievements > 0 ? $"{MarkdownHelpers.FormatNumber(x.TotalAchievements)} ({MarkdownHelpers.FormatNumber(x.TotalAchievementsXP)} XP)" : "-")
    .AddColumn("Players", x => x.TotalAchievements > 0 ? MarkdownHelpers.FormatNumber(x.EOS_Progressed) : "")
    .AddColumn("Rating", x => MarkdownHelpers.FormatRating(x.Rating))
    .AddColumn("Awards", x => MarkdownHelpers.FormatNumber(x.NumberOfAwardsMax));

var ratingTable = new MarkdownTable<GameDbItem>()
    .AddColumn("#", x => MarkdownHelpers.FormatRanking(x.Ranking_Rating))
    .AddColumn("Game", x => $"[{x.Name}]({GamesLink(x)})")
    .AddColumn("Rating", x => MarkdownHelpers.FormatRating(x.Rating))
    .AddColumn("Awards", x => MarkdownHelpers.FormatNumber(x.NumberOfAwardsMax))
    .AddColumn("Popularity Ranking", x => MarkdownHelpers.FormatRanking(x.Ranking_Popularity));

var awardsTable = new MarkdownTable<GameDbItem>()
    .AddColumn("#", x => MarkdownHelpers.FormatRanking(x.Ranking_Popularity))
    .AddColumn("Game", x => $"[{x.Name}]({GamesLink(x)})")
    .AddColumn("Awards", x => MarkdownHelpers.FormatNumber(x.NumberOfAwardsMax))
    .AddColumn("Rating", x => MarkdownHelpers.FormatRating(x.Rating))
    .AddColumn("Rating Ranking", x => MarkdownHelpers.FormatRanking(x.Ranking_Rating));

var awardsSumTable = new MarkdownTable<GameDbItem>()
    .AddColumn("#", x => MarkdownHelpers.FormatRanking(x.Ranking_PopularitySum))
    .AddColumn("Game", x => $"[{x.Name}]({GamesLink(x)})")
    .AddColumn("Awards", x => MarkdownHelpers.FormatNumber(x.NumberOfAwards))
    .AddColumn("Rating", x => MarkdownHelpers.FormatRating(x.Rating))
    .AddColumn("Rating Ranking", x => MarkdownHelpers.FormatRanking(x.Ranking_Rating));

var eosTable = new MarkdownTable<GameDbItem>()
    .AddColumn("#", x => MarkdownHelpers.FormatRanking(x.Ranking_EOS_Progress))
    .AddColumn("Game", x => $"[{x.Name}]({GamesLink(x)})")
    .AddColumn("Progressed", x => MarkdownHelpers.FormatNumber(x.EOS_Progressed))
    .AddColumn("Completed", x => MarkdownHelpers.FormatNumber(x.EOS_Completed));

var eosNewPlayersTable = new MarkdownTable<GameDbItem>()
    .AddColumn("#", x => MarkdownHelpers.FormatRanking(x.Ranking_EOS_NewPlayers))
    .AddColumn("Game", x => $"[{x.Name}]({GamesLink(x)})")
    .AddColumn("New Players", x => MarkdownHelpers.FormatNumber(x.EOS_NewPlayers))
    .AddColumn("Total", x => MarkdownHelpers.FormatNumber(x.EOS_Progressed));

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
    Path.Combine(path, "achievement_games.md"),
    nameDateTable.FormatTable(
        items
            .Where(x => x.TotalAchievements > 0)
            .OrderByDescending(x => x.Store.ReleaseDate)
            .ThenBy(x => x.Name)
    )
);

// Release Date
await File.WriteAllTextAsync(
    Path.Combine(path, "new_games.md"),
    nameDateTable.FormatTable(
        items
            .Where(x => x.Store.ReleaseDate != null && ratingsCutOffNew < x.Store.ReleaseDate && x.Store.ReleaseDate <= now)
            .OrderByDescending(x => x.Store.ReleaseDate)
            .ThenBy(x => x.Name)
    )
);
await File.WriteAllTextAsync(
    Path.Combine(path, "upcoming_games.md"),
    nameDateTable.FormatTable(
        items
            .Where(x => x.Store.ReleaseDate != null && ratingsCutOffNew < x.Store.ReleaseDate && x.Store.ReleaseDate > now)
            .OrderBy(x => x.Store.ReleaseDate)
            .ThenBy(x => x.Name)
    )
);


await File.WriteAllTextAsync(
    Path.Combine(path, "blockchain.md"),
    nameDateTable.FormatTable(
        items
            .Where(x => x.Store.isBlockchainUsed)
            .OrderByDescending(x => x.Store.ReleaseDate)
            .ThenBy(x => x.Name)
    )
);

// EOS
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
Directory.CreateDirectory("experimental");

var eosGames = new MarkdownTable<GameDbItem>()
    .AddColumn("Game", x => $"[{x.Name}](../{GamesLink(x)})")
    .AddColumn("Total Achievements", x => MarkdownHelpers.FormatRanking(x.TotalAchievements))
    .AddColumn("Total XP", x => MarkdownHelpers.FormatRanking(x.TotalAchievementsXP));


var eosGamesSets = new MarkdownTable<GameDbItem>()
    .AddColumn("Game", x => $"[{x.Name}](../{GamesLink(x)})")
    .AddColumn("Total Achievements", x => MarkdownHelpers.FormatRanking(x.TotalAchievements))
    .AddColumn("Total XP", x => MarkdownHelpers.FormatRanking(x.TotalAchievementsXP))
    .AddColumn("Sets", x => MarkdownHelpers.FormatRanking(x.AchievementSets.Count));

await File.WriteAllTextAsync(
    Path.Combine(path, "experimental/eos_total_achievements.md"),
    eosGames.FormatTable(items.Where(x => x.TotalAchievements > 0).OrderByDescending(x => x.TotalAchievements).ThenBy(x => x.Name))
);

await File.WriteAllTextAsync(
    Path.Combine(path, "experimental/eos_total_xp.md"),
    eosGames.FormatTable(items.Where(x => x.TotalAchievementsXP > 0).OrderByDescending(x => x.TotalAchievementsXP).ThenBy(x => x.Name))
);

await File.WriteAllTextAsync(
    Path.Combine(path, "experimental/eos_num_sets.md"),
    eosGamesSets.FormatTable(items.Where(x => x.AchievementSets.Count > 1).OrderByDescending(x => x.AchievementSets.Count).ThenBy(x => x.Name))
);

// Is there any?
await File.WriteAllTextAsync(
    Path.Combine(path, "experimental/reviews_disabled.md"),
    eosGamesSets.FormatTable(items.Where(x => x.ReviewsDisabled).OrderBy(x => x.Name))
);

//
await gitCmd.WithArguments(new[] { "add", "." }).WithValidation(CommandResultValidation.None).ExecuteAsync();
var mkRes = await gitCmd.WithArguments(new[] { "commit", "-m", "Generate Markdown Lists" }).WithValidation(CommandResultValidation.None).ExecuteBufferedAsync();

Console.Error.WriteLine(mkRes.StandardError);
Console.WriteLine(mkRes.StandardOutput);
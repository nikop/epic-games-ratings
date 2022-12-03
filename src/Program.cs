using EpicRatingsUpdater;

using GraphQL.Client.Http;

using System.Text.Json;

// Configs
var path = new DirectoryInfo(".").FullName;
var index = new Dictionary<string, string>();

var forceUpdateNames = args.Any(x => x == "--update-names");
var skipRatingsUpdate = args.Any(x => x == "--skip-update");
var skipOffers = args.Any(x => x == "--skip-offers");
var skipItems = args.Any(x => x == "--skip-items");

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

        List<EpicItem> items = new();

        foreach (var v in i.Value)
        {
            var fileName = Path.Combine(dirItems, "items", $"{v}.json");

            try
            {
                var fileContent = await File.ReadAllTextAsync(fileName);
                var epicItem = JsonSerializer.Deserialize<EpicItem>(fileContent);

                if (epicItem == null)
                    continue;

                items.Add(epicItem);
            }
            catch (Exception)
            {
            }
        }
    }
}

var c = 0;
var total = namespaces.Count;

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

/**
 * Update rating by namespace
 */
async ValueTask UpdateRating(JsonIndexDb<GameDbItem> gameIndex, NamespaceDef ns, CancellationToken ct)
{
    Interlocked.Increment(ref c);
    Console.Write($"\rUpdating {c} / {total}");

    var dbItem = await GetOrCreate(gameIndex, ns.Namespace).ConfigureAwait(false);
    var requiresSave = false;

    try
    {
        var pi = await EpicApi.GetProductResult(ns.Namespace).ConfigureAwait(false);

        if (pi != null)
        {
            requiresSave = true;

            if (dbItem.FirstSeen == null)
            {
                dbItem.FirstSeen = DateTimeOffset.UtcNow;
            }

            var ratingChanged = dbItem.Rating != pi.averageRating;

            if (ratingChanged)
            {
                if (dbItem.RatingHistory.Count == 0)
                {
                    dbItem.FirstSeenRating = DateTimeOffset.UtcNow;
                }

                dbItem.LastChanged = DateTimeOffset.UtcNow;
                dbItem.RatingHistory.Add(new GameDbItemRatingHistory
                {
                    Time = DateTimeOffset.UtcNow,
                    //NumberOfRatings = pi.ratingCount,
                    Rating = pi.averageRating,
                });
            }

            dbItem.ProductSlug = ns.ProductSlug;
            //dbItem.NumberOfRatings = pi.ratingCount;
            dbItem.Rating = pi.averageRating;

            if (pi.pollResult != null)
            {
                foreach (var pr in pi.pollResult.OrderByDescending(x => x.total))
                {
                    var text = $"{pr.localizations.resultText} {pr.localizations.resultTitle}";
                    var current = dbItem.Tags.FirstOrDefault(x => x.Text == text);

                    if (current != null)
                    {
                        current.Count = pr.total ?? 0;
                    }
                    else
                    {
                        dbItem.Tags.Add(new GameDbItemTag
                        {
                            Text = text,
                            Count = pr.total ?? 0,
                        });
                    }
                }
            }
        }
    }
    catch (GraphQLHttpRequestException)
    {
        Console.WriteLine("Ratelimited...?");
        await Task.Delay(10000, ct);
    }
    catch (Exception)
    {
        // ??
    }

    if (requiresSave)
    {
        await gameIndex.SaveItem(dbItem).ConfigureAwait(false);
    }

    await Task.Delay(forceUpdateNames ? 200 : 75, ct);
}

// Fetch ratings
if (!skipRatingsUpdate)
{
    Console.WriteLine("Updating rating");

    await Parallel.ForEachAsync(
        namespaces.Values, 
        new ParallelOptions {  MaxDegreeOfParallelism = forceUpdateNames ? 2 : 3 },
        (ns, ct) => UpdateRating(gameIndex, ns, ct)
    );
}

// Create combined files
var output = await gameIndex.GetAllItems().ConfigureAwait(false);

// Only games with rating
var filteredList = output.Where(x => x.Rating != null).ToList();

foreach (var item in output)
{
    var name = gameIndex.Files[item.ID] + ".md";
    var fullName = Path.Combine(dirGames, name);

    Directory.CreateDirectory(Path.GetDirectoryName(fullName)!);

    var page = Markdown.BuildMarkdownGamePage(item);

    await File.WriteAllTextAsync(fullName, page);
}

// Markdown
await File.WriteAllTextAsync(
    Path.Combine(path, "by_name.md"),
    Markdown.BuildMarkdownTable(gameIndex, filteredList.OrderBy(x => x.Name))
);
await File.WriteAllTextAsync(
    Path.Combine(path, "by_rating.md"), 
    Markdown.BuildMarkdownTable(gameIndex, filteredList.OrderByDescending(x => x.Rating).ThenByDescending(x => x.NumberOfRatings).ThenBy(x => x.Name), true)
);
await File.WriteAllTextAsync(
    Path.Combine(path, "new_games.md"),
    Markdown.BuildMarkdownTable(gameIndex, filteredList.Where(x => x.FirstSeen != null && ratingsCutOffNew < x.FirstSeen).OrderByDescending(x => x.FirstSeen).ThenBy(x => x.Name), true)
);
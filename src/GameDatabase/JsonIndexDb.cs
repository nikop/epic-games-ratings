using System.Collections.Concurrent;
using System.Text.Json;

namespace EpicRatingsUpdater.GameDatabase
{
    public class JsonIndexDb<T>
        where T : JsonIndexDbItem
    {
        private static readonly char[] invalidFileNameChars = { '\"', '<', '>', '|', '\0', (char)1, (char)2, (char)3, (char)4, (char)5, (char)6, (char)7, (char)8, (char)9, (char)10, (char)11, (char)12, (char)13, (char)14, (char)15, (char)16, (char)17, (char)18, (char)19, (char)20, (char)21, (char)22, (char)23, (char)24, (char)25, (char)26, (char)27, (char)28, (char)29, (char)30, (char)31, ':', '*', '?', '\\', '/' };

        static readonly char[] disallowedChars = new char[] { ' ' };

        public string BasePath { get; }

        public string IndexFile { get; }

        public ConcurrentDictionary<string, string> Files { get; } = new();

        public ConcurrentDictionary<string, T> CachedItems { get; } = new();

        JsonSerializerOptions SerializeOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
        };

        private SemaphoreSlim indexLock = new(1, 1);

        public JsonIndexDb(string path)
        {
            BasePath = path;

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            IndexFile = Path.Combine(path, "index.json");

            if (File.Exists(IndexFile))
            {
                var text = File.ReadAllText(IndexFile);
                var f = JsonSerializer.Deserialize<ConcurrentDictionary<string, string>>(text);

                if (f != null)
                {
                    // Linux fix
                    foreach (var kv in f)
                    {
                        f[kv.Key] = kv.Value.Replace('\\', '/');
                    }

                    Files = f;
                }

            }
        }

        public async Task SaveIndex()
        {
            await indexLock.WaitAsync();

            try
            {
                var f = JsonSerializer.Serialize(Files, SerializeOptions);
                await File.WriteAllTextAsync(IndexFile, f);
            }
            catch (Exception)
            {

            }
            finally
            {
                indexLock.Release();
            }
        }

        public async Task<T?> GetItemByKey(string key)
        {
            if (!Files.TryGetValue(key, out var name))
            {
                return null;
            }

            try
            {
                var fullPath = Path.Combine(BasePath, $"{name}.json");

                var text = await File.ReadAllTextAsync(fullPath);

                var data = JsonSerializer.Deserialize<T>(text);

                return data;
            }
            catch (DirectoryNotFoundException)
            {
                Files.TryRemove(key, out var _);
            }
            catch (FileNotFoundException)
            {
                Files.TryRemove(key, out var _);
            }

            return null;
        }

        protected string GetNewFileName(T item, string? currentName = null)
        {
            var baseName = (item.Name ?? item.ID).Trim().ToLower();

            var validFilename = new string(baseName.Select(ch => disallowedChars.Contains(ch) || invalidFileNameChars.Contains(ch) ? '_' : ch).ToArray()).Trim('_');

            var dir = validFilename[0].ToString();

            var realDir = Path.Combine(BasePath, dir);

            if (!Directory.Exists(realDir))
            {
                Directory.CreateDirectory(realDir);
            }

            var i = 0;

            var fullName = Path.Combine(dir, validFilename);
            var fullPath = Path.Combine(BasePath, $"{fullName}.json");

            if (currentName != null && fullName == currentName)
            {
                return currentName;
            }

            while (File.Exists(fullPath))
            {
                i++;
                fullName = Path.Combine(dir, validFilename) + "_" + i;
                fullPath = Path.Combine(BasePath, $"{fullName}.json");
            }

            return fullName;
        }

        public async Task RenameItem(T item)
        {
            if (!Files.TryGetValue(item.ID, out var currentName))
            {
                return;
            }

            var newName = GetNewFileName(item, currentName);

            if (currentName == newName)
                return;

            if (Files.TryUpdate(item.ID, newName, currentName))
            {
                var oldPath = Path.Combine(BasePath, $"{currentName}.json");
                var newPath = Path.Combine(BasePath, $"{newName}.json");

                File.Move(oldPath, newPath);

                await SaveIndex().ConfigureAwait(false);
            }
            else
            {

            }
        }

        public async Task SaveItem(T item)
        {
            string? fileName;

            if (!Files.TryGetValue(item.ID, out fileName))
            {
                fileName = GetNewFileName(item);

                if (!Files.TryAdd(item.ID, fileName))
                {
                    throw new Exception("Invalid Crash");
                }

                await SaveIndex().ConfigureAwait(false);
            }

            CachedItems[item.ID] = item;

            var fullPath = Path.Combine(BasePath, $"{fileName}.json");
            var data = JsonSerializer.Serialize(item, SerializeOptions);

            await File.WriteAllTextAsync(fullPath, data).ConfigureAwait(false);
        }

        public async Task<List<T>> GetAllItems()
        {
            var list = new List<T>();

            foreach (var kv in Files)
            {
                var item = await GetItemByKey(kv.Key).ConfigureAwait(false);

                if (item != null)
                    list.Add(item);
            }

            return list;
        }
    }
}

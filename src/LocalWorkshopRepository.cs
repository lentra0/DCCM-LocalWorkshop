using System.Text.Json;
using Serilog;

namespace LocalWorkshop
{
    public static class LocalWorkshopRepository
    {
        private static readonly string[] PreviewNames =
            ["preview.png", "preview.jpg", "preview.jpeg", "preview.gif"];

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        public static IReadOnlyList<LocalWorkshopItem> Scan(string root)
        {
            var items = new List<LocalWorkshopItem>();
            if (!Directory.Exists(root))
            {
                Log.Information("[LocalWorkshop] repo root not found, nothing to load: {root}", root);
                return items;
            }

            foreach (var dir in Directory.EnumerateDirectories(root))
            {
                try
                {
                    var item = TryReadItem(dir);
                    if (item != null)
                        items.Add(item);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "[LocalWorkshop] failed to read item folder {dir}", dir);
                }
            }

            items.Sort((a, b) => a.Id.CompareTo(b.Id));
            return items;
        }

        private static LocalWorkshopItem? TryReadItem(string dir)
        {
            var folderName = Path.GetFileName(dir);
            var id = ParseFolderId(folderName);
            var info = ReadInfo(dir);

            var pak = ResolvePak(dir, info);
            if (pak == null)
            {
                Log.Warning("[LocalWorkshop] no .pak in {dir}; skipping", dir);
                return null;
            }

            string? preview = null;
            foreach (var name in PreviewNames)
            {
                var p = Path.Combine(dir, name);
                if (File.Exists(p)) { preview = p; break; }
            }

            return new LocalWorkshopItem
            {
                Id = id,
                FolderPath = dir,
                PakPath = pak,
                PreviewPath = preview,
                Info = info,
            };
        }

        private static LocalWorkshopInfo ReadInfo(string dir)
        {
            foreach (var fileName in new[] { "info.json", "settings.json" })
            {
                var path = Path.Combine(dir, fileName);
                if (!File.Exists(path)) continue;
                try
                {
                    var json = File.ReadAllText(path);
                    var info = JsonSerializer.Deserialize<LocalWorkshopInfo>(json, JsonOpts);
                    if (info != null) return info;
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "[LocalWorkshop] invalid {file} in {dir}, trying next/defaults", fileName, dir);
                }
            }
            return new LocalWorkshopInfo();
        }

        private static string? ResolvePak(string dir, LocalWorkshopInfo info)
        {
            if (!string.IsNullOrWhiteSpace(info.Pak))
            {
                var explicitPak = Path.Combine(dir, info.Pak!);
                return File.Exists(explicitPak) ? explicitPak : null;
            }

            return Directory.EnumerateFiles(dir, "*.pak", SearchOption.TopDirectoryOnly)
                            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                            .FirstOrDefault();
        }

        private static long ParseFolderId(string folderName)
        {
            if (long.TryParse(folderName, out var pure))
                return pure;

            var m = System.Text.RegularExpressions.Regex.Match(folderName, @"^\s*(\d+)");
            if (m.Success && long.TryParse(m.Groups[1].Value, out var lead))
                return lead;

            return StableId(folderName);
        }

        private static long StableId(string s)
        {
            ulong h = 1469598103934665603UL;
            foreach (var c in s)
            {
                h ^= c;
                h *= 1099511628211UL;
            }
            return (long)(h & 0x7FFFFFFFFFFFFFFFUL);
        }
    }
}

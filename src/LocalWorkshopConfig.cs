using System.Text.Json;
using ModCore.Storage;
using Serilog;

namespace LocalWorkshop
{
    public sealed class LocalWorkshopConfig
    {
        private readonly string _path;
        public HashSet<long> EnabledIds { get; private set; } = [];

        private LocalWorkshopConfig(string path) => _path = path;

        public static LocalWorkshopConfig Load()
        {
            var dir = FolderInfo.Config.FullPath;
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "localworkshop.json");
            var cfg = new LocalWorkshopConfig(path);
            try
            {
                if (File.Exists(path))
                {
                    var ids = JsonSerializer.Deserialize<long[]>(File.ReadAllText(path));
                    if (ids != null) cfg.EnabledIds = [.. ids];
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "[LocalWorkshop] could not read config, starting empty");
            }
            return cfg;
        }

        public bool IsEnabled(LocalWorkshopItem item) =>
            EnabledIds.Contains(item.Id) || item.Info.EnabledByDefault;

        public void Set(long id, bool enabled)
        {
            if (enabled) EnabledIds.Add(id);
            else EnabledIds.Remove(id);
            Save();
        }

        private void Save()
        {
            try
            {
                File.WriteAllText(_path,
                    JsonSerializer.Serialize(EnabledIds.ToArray(),
                        new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "[LocalWorkshop] could not write config");
            }
        }
    }
}

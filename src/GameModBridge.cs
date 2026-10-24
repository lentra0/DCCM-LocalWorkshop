using ModCore.Modules;
using ModCore.Utilities;
using Serilog;

namespace LocalWorkshop
{
    public sealed class GameModBridge
    {
        private readonly LocalWorkshopConfig _config;
        private IReadOnlyList<LocalWorkshopItem> _items = [];
        private readonly HashSet<string> _mounted = new(StringComparer.OrdinalIgnoreCase);

        public GameModBridge(LocalWorkshopConfig config) => _config = config;

        public int MountedCount => _mounted.Count;

        public void SetItems(IReadOnlyList<LocalWorkshopItem> items) => _items = items;

        public void ApplyEnabledDiffPaks()
        {
            foreach (var item in _items)
                if (_config.IsEnabled(item)) MountPak(item.PakPath);

            if (_mounted.Count > 0) MergeCdb();
        }

        public void SetEnabled(LocalWorkshopItem item, bool on)
        {
            if (!on) return;
            MountPak(item.PakPath);
            MergeCdb();
        }

        private bool MountPak(string pakPath)
        {
            if (_mounted.Contains(pakPath)) return false;
            try
            {
                FsPak.Instance.FileSystem.loadPak(pakPath.AsHaxeString());
                _mounted.Add(pakPath);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[LocalWorkshop] failed to mount pak {pak}", pakPath);
                return false;
            }
        }

        private void MergeCdb()
        {
            try
            {
                var cdb = CDBManager.Instance;
                cdb.LoadJsonData(RestoreRowOrder(cdb.GetAlteredCDB()));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[LocalWorkshop] CastleDB merge failed");
            }
        }

        private static string RestoreRowOrder(string json)
        {
            try
            {
                json = CdbRowOrder.Restore(json, out var sheets);
                if (sheets > 0) Log.Information("[LocalWorkshop] restored row order in {count} CDB sheet(s)", sheets);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "[LocalWorkshop] could not restore CDB row order, using the DCCM merge as is");
            }
            return json;
        }
    }
}

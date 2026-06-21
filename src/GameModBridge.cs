using dc.tool.mod;
using HaxeProxy.Runtime;
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

        public void Install()
        {
            Hook_ModManager.onSteamInit += (orig, self) =>
            {
                orig(self);
                ApplyEnabledDiffPaks();
            };
        }

        public void SetItems(IReadOnlyList<LocalWorkshopItem> items) => _items = items;

        public void ApplyEnabledDiffPaks()
        {
            foreach (var item in _items)
                if (_config.IsEnabled(item)) MountPak(item.PakPath);

            if (_mounted.Count > 0) MergeCdb();
        }

        public void ReMergeIfMounted()
        {
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
                dc.Data.Class.loadJson.Invoke(
                    CDBManager.Class.instance.getAlteredCDB(),
                    default(Ref<bool>));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[LocalWorkshop] CastleDB merge failed");
            }
        }
    }
}

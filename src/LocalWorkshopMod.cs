using ModCore.Events.Interfaces;
using ModCore.Menu;
using ModCore.Mods;
using Serilog;

namespace LocalWorkshop
{
    public sealed class LocalWorkshopMod : ModBase,
        IOnAfterLoadingAssets,
        IModMenuProvider
    {
        private readonly LocalWorkshopConfig _config;
        private readonly GameModBridge _bridge;
        private LocalWorkshopMenu? _menu;
        private IReadOnlyList<LocalWorkshopItem> _items = [];
        private bool _scanned;

        public LocalWorkshopMod(ModInfo info) : base(info)
        {
            _config = LocalWorkshopConfig.Load();
            _bridge = new GameModBridge(_config);
        }

        public override void Initialize()
        {
            _bridge.Install();
            Scan();
        }

        private string RepoRoot => Info.ModRoot.Info.FullName;

        private void Scan()
        {
            if (_scanned) return;
            _items = LocalWorkshopRepository.Scan(RepoRoot);
            _bridge.SetItems(_items);
            _menu?.SetItems(_items);
            _scanned = true;
            Log.Information("[LocalWorkshop] {count} local mod(s) found", _items.Count);
        }

        void IOnAfterLoadingAssets.OnAfterLoadingAssets()
        {
            Scan();
            _bridge.ReMergeIfMounted();
        }

        IModMenu IModMenuProvider.GetModMenu()
        {
            Scan();
            _menu ??= new LocalWorkshopMenu(_config, _bridge, _items);
            return _menu;
        }
    }
}

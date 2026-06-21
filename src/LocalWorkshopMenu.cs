using dc.ui;
using HaxeProxy.Runtime;
using ModCore.Menu;
using ModCore.Utilities;
using Serilog;

namespace LocalWorkshop
{
    public sealed class LocalWorkshopMenu : IModMenu
    {
        private readonly LocalWorkshopConfig _config;
        private readonly GameModBridge _bridge;
        private IReadOnlyList<LocalWorkshopItem> _items;

        public LocalWorkshopMenu(LocalWorkshopConfig config, GameModBridge bridge,
            IReadOnlyList<LocalWorkshopItem> items)
        {
            _config = config;
            _bridge = bridge;
            _items = items;
        }

        public void SetItems(IReadOnlyList<LocalWorkshopItem> items) => _items = items;

        public string GetName() => "Local Workshop";

        public string GetSubText()
        {
            int enabled = _items.Count(_config.IsEnabled);
            return $"{_items.Count} mod(s), {enabled} enabled";
        }

        public void BuildMenu(Options options)
        {
            try
            {
                options.createScroller(1.0);
                var flow = options.scrollerFlow;

                int enabled = _items.Count(_config.IsEnabled);
                string status = _items.Count == 0
                    ? "No local mods found"
                    : $"{_bridge.MountedCount} loaded, {enabled} of {_items.Count} enabled";
                options.addSeparator(status.AsHaxeString(), flow);

                foreach (var it in _items)
                {
                    var item = it;
                    bool def = _config.IsEnabled(item);

                    HlFunc<bool> onVal = () =>
                    {
                        bool next = !_config.IsEnabled(item);
                        _config.Set(item.Id, next);
                        try { _bridge.SetEnabled(item, next); }
                        catch (Exception ex) { Log.Warning(ex, "[LocalWorkshop] toggle failed for {item}", item); }
                        return next;
                    };

                    options.addToggleWidget(
                        item.DisplayName.AsHaxeString(),
                        ItemSubText(item).AsHaxeString(),
                        onVal,
                        Ref<bool>.In(in def),
                        flow);
                }

                if (_items.Count > 0)
                    options.addSeparator("Enabling applies now; disabling needs a restart".AsHaxeString(), flow);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[LocalWorkshop] failed to build menu");
            }
        }

        private static string ItemSubText(LocalWorkshopItem item)
        {
            if (!string.IsNullOrWhiteSpace(item.Info.Description)) return item.Info.Description!;
            if (!string.IsNullOrWhiteSpace(item.Info.Author)) return $"by {item.Info.Author}";
            if (!string.IsNullOrWhiteSpace(item.Info.Category)) return item.Info.Category!;
            return "local pak";
        }
    }
}

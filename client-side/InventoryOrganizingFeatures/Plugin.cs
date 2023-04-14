using BepInEx;

namespace InventoryOrganizingFeatures
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            // Assign tag and show active tags
            new PostEditTagWindowShow().Enable();
            new PostEditTagWindowClose().Enable();
            // Sort lock
            new PreGClass2166RemoveAll().Enable();
            // Move lock
            new PreItemViewOnPointerDown().Enable();
            new PreItemViewOnBeginDrag().Enable();

            new PostGridSortPanelShow().Enable();
        }
    }
}

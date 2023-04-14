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
            new PostEditTagWindowShow().Enable();
            new PostEditTagWindowClose().Enable();
            new PreGClass2166RemoveAll().Enable();
            new PreItemViewOnBeginDrag().Enable();
        }
    }
}

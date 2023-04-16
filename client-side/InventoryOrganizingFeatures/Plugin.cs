using BepInEx;

namespace InventoryOrganizingFeatures
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static bool EnableLogs = false;
        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            // Pull handbook from the init method.
            new PostInitHanbook().Enable(); 
            // Pre-load image from hideout button for organize button
            new PostMenuScreenInit().Enable();
            // Assign tag and show active tags when saving EditTagWindow.
            new PostEditTagWindowShow().Enable();
            // Sort lock
            new PreGClass2166RemoveAll().Enable(); // Prevent Sorting
            // Move lock
            new PreItemViewOnPointerDown().Enable(); // Prevent Drag
            new PreItemViewOnBeginDrag().Enable(); // Prevent Drag
            new PostGetFailedProperty().Enable(); // Prevent quick move(Ctrl/Shift+Click)
            new PreQuickFindAppropriatePlace().Enable(); // Don't show warnings when item is Move Locked

            new PostGridSortPanelShow().Enable();
        }
    }
}

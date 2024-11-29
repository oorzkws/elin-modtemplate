using BepInEx;
using BepInEx.Logging;

namespace ElinModTemplate;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
internal class ElinModTemplate : BaseUnityPlugin {
    internal static new ManualLogSource Logger { get; private set; }

    private void Awake() {
        Logger = base.Logger;
        var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        harmony.PatchAll();
        Logging.Log($"{MyPluginInfo.PLUGIN_NAME} version {MyPluginInfo.PLUGIN_VERSION} successfully loaded", LogLevel.Message);
    }
}
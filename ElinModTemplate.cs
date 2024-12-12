using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace ElinModTemplate;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
internal class ElinModTemplate : BaseUnityPlugin {
    internal static new ManualLogSource? Logger { get; private set; }
    // Access to settings values
    public static new ConfigFile? Config { get; private set; }
    // Used for access to hook events
    private static Settings settings = null!;
    // Access to harmony instance
    private static Harmony harmony = null!;

    // On assembly load
    private void Awake() {
        Logger = base.Logger;
        Config = base.Config;
        harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        settings = new Settings(Config, harmony);
        harmony.PatchAll();
        if (Config.TryGetEntry<bool>("Display", "LogExample", out var shouldLog) && shouldLog.Value) {
            Logging.Log($"{MyPluginInfo.PLUGIN_NAME} version {MyPluginInfo.PLUGIN_VERSION} successfully loaded", LogLevel.Message);
        }
    }

    // On main menu
    private void Start() {
        settings.BindPatches();
    }

    private void OnDestroy() {
        harmony.UnpatchSelf();
    }
}
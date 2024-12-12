namespace ElinModTemplate;

public partial class Settings {
    // Declare as static for reflection purposes
    internal static Setting<bool> LogExample = new("Display",
        "LogExample",
        true,
        "Log startup message");
    // Used for our conditional patch
    internal static Setting<bool> RemoveSunmapSpam = new("Display",
        "RemoveSunmapSpam",
        true,
        "Applies a transpiler to remove the \"Rebuilding Sunmap\" spam in the Unity debug log");
}
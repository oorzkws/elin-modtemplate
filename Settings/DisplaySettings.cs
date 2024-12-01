namespace ElinModTemplate;

public partial class Settings {
    // Declare as static for reflection purposes
    internal static Setting<bool> IncludeSellPrice = new("Display",
        "LogExample",
        true,
        "Log our example setting in the transpiler");
}
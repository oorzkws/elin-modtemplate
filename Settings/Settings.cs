using BepInEx.Configuration;

namespace ElinModTemplate;

public partial class Settings {

    // This constructor automatically instantiates any Setting<T> property found on the Settings class
    public Settings(ConfigFile config) {
        // Iterate our declared fields and init each setting
        var genericSettingType = typeof(Setting<>);
        var staticGenericFields = typeof(Settings).GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        foreach (var field in staticGenericFields) {
            if (field.FieldType.GetGenericTypeDefinition() != genericSettingType)
                continue;
            field.FieldType.GetMethod("Bind")!.Invoke(field.GetValue(null), [config]);
        }
    }

    internal readonly struct Setting<T>(string section, string key, T defaultValue, string? description = null, AcceptableValueBase? acceptableValues = null, object[]? tags = null) {
        [UsedImplicitly]
        public void Bind(ConfigFile config) {
            config.Bind(new ConfigDefinition(section, key), defaultValue, new ConfigDescription(description, acceptableValues, tags));
        }
    }
}
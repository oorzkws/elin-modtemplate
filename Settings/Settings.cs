using BepInEx.Configuration;
using ElinModTemplate.Extensions;

namespace ElinModTemplate;

public partial class Settings {
    private readonly ConfigFile config;
    private readonly Harmony harmony;
    private bool methodsSearched;
    private readonly Dictionary<(string Section, string Key), Dictionary<PatchClassProcessor, HashSet<MethodBase>>> methodsWithPatches = new();

    // This constructor automatically instantiates any Setting<T> property found on the Settings class
    public Settings(ConfigFile config, Harmony harmony) {
        // Store the harmony instance
        this.harmony = harmony;
        // Iterate our declared fields and init each setting
        var genericSettingType = typeof(Setting<>);
        var staticGenericFields = typeof(Settings).GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        foreach (var field in staticGenericFields) {
            if (field.FieldType.GetGenericTypeDefinition() != genericSettingType)
                continue;
            field.FieldType.GetMethod("Bind")!.Invoke(field.GetValue(null), [config]);
        }
        // Hook setting change
        config.SettingChanged += (_, change) => {
            // Not a boolean or not defined
            if (change.ChangedSetting?.SettingType != typeof(bool))
                return;
            var definition = change.ChangedSetting!.Definition;
            // Define if not yet defined
            var newValue = change.ChangedSetting.BoxedValue as bool?;
            ApplySetting(definition.Section, definition.Key, newValue!.Value);
        };
        this.config = config;
    }

    private void ApplySetting(string section, string key, bool value) {
        if (!methodsSearched) {
            InitializePatchList();
        }
        if (methodsWithPatches.TryGetValue((section, key), out var methods)) {
            methods.Do(pair => {
                pair.Value.Do(m => Logging.Log($"{(value ? "P" : "Unp")}atching {pair.Key.containerType.FullName}:{m.Name} because of {section}.{key}"));
                if (value)
                    harmony.PatchPartial(pair.Key, pair.Value);
                else
                    harmony.UnpatchPartial(pair.Key, pair.Value);
            });
        }

    }


    private void InitializePatchList() {
        var conditionalMethods = GetConditionalMethods();
        foreach (var (patch, setting, patcher) in conditionalMethods) {
            var index = (setting.Section, setting.Key);
            if (!methodsWithPatches.ContainsKey(index)) {
                methodsWithPatches[index] = new();
            }
            var patcherList = methodsWithPatches[index];
            if (!patcherList.ContainsKey(patcher)) {
                patcherList[patcher] = new();
            }
            patcherList[patcher].Add(patch);
        }
        methodsSearched = true;
    }

    private IEnumerable<(MethodBase patch, SettingDependentPatchAttribute setting, PatchClassProcessor patcher)> GetConditionalMethods() {
        foreach (var type in AccessTools.GetTypesFromAssembly(Assembly.GetExecutingAssembly())) {
            var typeSetting = type.GetCustomAttributes(true).OfType<SettingDependentPatchAttribute>().FirstOrDefault();
            var processedClass = new PatchClassProcessor(harmony, type, true);
            foreach (var patch in processedClass.patchMethods) {
                var method = patch.info.method;
                var methodSetting = method.GetCustomAttributes(true).OfType<SettingDependentPatchAttribute>().FirstOrDefault() ?? typeSetting;
                if (methodSetting is null)
                    continue; // Neither class nor method has decoration
                Logging.Log($"Toggle-able patch discovered, controlled by {methodSetting.Section}.{methodSetting.Key}\n\t{patch.info.method.DeclaringType!.FullName}:{patch.info.method.Name} -> {patch.info.GetOriginalMethod().DeclaringType!.FullName}:{patch.info.GetOriginalMethod().Name}");
                yield return (method, methodSetting, processedClass);
            }
        }
    }

    /// <summary>
    /// Call after harmony.PatchAll to bind all applied patches to their settings
    /// </summary>
    public void BindPatches() {
        // Define if not yet defined
        if (!methodsSearched) {
            InitializePatchList();
        }
        methodsWithPatches.Do(pair => {
            var definition = pair.Key;
            if (config.TryGetEntry<bool>(definition.Section, definition.Key, out var newValue) && !newValue.Value) {
                ApplySetting(definition.Section, definition.Key, newValue.Value);
            }
        });
    }

    internal readonly struct Setting<T>(string section, string key, T defaultValue, string? description = null, AcceptableValueBase? acceptableValues = null, object[]? tags = null) {
        [UsedImplicitly]
        public void Bind(ConfigFile config) {
            config.Bind(new ConfigDefinition(section, key), defaultValue, new ConfigDescription(description, acceptableValues, tags));
        }
    }
}
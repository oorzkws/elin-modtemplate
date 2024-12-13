#nullable disable
extern alias UnityCore;
using static ElinModTemplate.EzTranspiler;
using Debug = UnityCore::UnityEngine.Debug;

namespace ElinModTemplate.Patches;

/// <summary>
/// Removes the "Building Sunmap" log spam
/// </summary>
[HarmonyPatch(typeof(VirtualDate), nameof(VirtualDate.BuildSunMap)), SettingDependentPatch("Display", "RemoveSunmapSpam")]
internal static class TranspilerExample {
    [UsedImplicitly]
    public static CodeInstructions Transpiler(CodeInstructions instructions, MethodBase method) {
        var editor = new CodeMatcher(instructions).Start();

        // Toggle in the csproj
        #if TRANSPILERDEMO
        // Manual method
        // Two variants, (object) and (object, Object) 
        var unityLog = AccessTools.Method("UnityEngine.Debug:Log", [typeof(object)]);
        editor.Replace([
            new CodeMatch(i => i.LoadsConstant()),
            Call(unityLog)
        ], []);
        #else
        // Automatic method
        editor.Replace(InstructionSignature(() => Debug.Log("Building Sunmap")), []);
        #endif

        // Done
        return editor.InstructionEnumeration();
    }
}

[HarmonyPatch(typeof(VirtualDate), nameof(VirtualDate.BuildSunMap))]
internal static class PostfixExample {
    [HarmonyPostfix, SettingDependentPatch("Display", "LogExample"), UsedImplicitly]
    // ReSharper disable once InconsistentNaming
    private static void Something(VirtualDate __instance) {
        Logging.Log("Called!");
    }
}
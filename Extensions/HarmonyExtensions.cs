using BepInEx.Logging;
using HarmonyLib.Public.Patching;

namespace ElinModTemplate.Extensions;

public static class HarmonyExtensions {

    /// <summary>
    /// Applies the given patcher, but only with the patches given in patchMethods
    /// </summary>
    /// <param name="harmony"></param>
    /// <param name="patcher"></param>
    /// <param name="patchMethods"></param>
    public static void PatchPartial(this Harmony harmony, PatchClassProcessor patcher, IEnumerable<MethodBase> patchMethods) {
        // Clone since we modify the metadata
        var clonedPatcher = new PatchClassProcessor(harmony, patcher.containerType, true);
        // Remove methods that are untyped, not contained in our list, or already applied
        clonedPatcher.patchMethods.RemoveAll(patch => !patch.type.HasValue || !patchMethods.Contains(patch.info.method) || patch.IsApplied(harmony.Id));
        // Apply
        clonedPatcher.Patch();
    }

    /// <summary>
    /// Unpatches the given patcher, but only the methods contained in our list
    /// </summary>
    /// <remarks>Works by fully unpatching, then partially repatching the targets</remarks>
    /// <param name="harmony"></param>
    /// <param name="patcher"></param>
    /// <param name="patchMethods"></param>
    public static void UnpatchPartial(this Harmony harmony, PatchClassProcessor patcher, IEnumerable<MethodBase> patchMethods) {
        var clonedPatcher = new PatchClassProcessor(harmony, patcher.containerType, true);
        // Unpatch when: has a patch type, is in our array, is already applied 
        clonedPatcher.patchMethods.Where(patch => patch.type.HasValue && patchMethods.Contains(patch.info.method) && patch.IsApplied(harmony.Id))
                     .Do(patch => patch.info.method.OriginalMethods(patch.type!.Value)
                                       .Do(original => harmony.Unpatch(original, patch.type!.Value, harmony.Id)));
        // Remove methods that are untyped, contained in our list, or already applied
        clonedPatcher.patchMethods.RemoveAll(patch => !patch.type.HasValue || patchMethods.Contains(patch.info.method) || patch.IsApplied(harmony.Id));
        // Log what we're patching
        clonedPatcher.patchMethods.Do(p => Logging.Log($"Will re-apply {p.type} patch to {p.info.GetOriginalMethod()}", LogLevel.Debug));
        // Re-patch remainder
        clonedPatcher.Patch();
    }

    /// <summary>
    /// Returns whether the AttributePatch has been applied for the given harmony instance ID
    /// </summary>
    /// <param name="self">AttributePatch</param>
    /// <param name="harmonyId">Harmony instance ID</param>
    /// <returns></returns>
    private static bool IsApplied(this AttributePatch self, string harmonyId = "") => PatchManager.PatchInfos.Values.ToArray()
                                                                                                  .SelectMany(patchInfo => patchInfo.OfType(self.type!.Value))
                                                                                                  .Where(patch => patch.owner.Contains(harmonyId))
                                                                                                  .Any(patch => patch.PatchMethod == self.info.method);


    /// <summary>
    /// Returns the patches from a PatchInfo instance for a given HarmonyPatchType
    /// </summary>
    /// <param name="self">PatchInfo</param>
    /// <param name="patchType">HarmonyPatchType</param>
    /// <returns></returns>
    public static IEnumerable<Patch> OfType(this PatchInfo self, HarmonyPatchType patchType) {
        return patchType switch {
            HarmonyPatchType.All           => Enum.GetValues(typeof(HarmonyPatchType)).Cast<HarmonyPatchType>().Where(subtype => subtype != HarmonyPatchType.All).SelectMany(self.OfType),
            HarmonyPatchType.Prefix        => self.prefixes,
            HarmonyPatchType.Postfix       => self.postfixes,
            HarmonyPatchType.Transpiler    => self.transpilers,
            HarmonyPatchType.Finalizer     => self.finalizers,
            HarmonyPatchType.ReversePatch  => [],
            HarmonyPatchType.ILManipulator => self.ilmanipulators,
            _                              => [],
        };
    }
    
    public static string FullQualifiedName(this MethodBase method) {
        var typeName = method.DeclaringType?.FullName ?? "undefined";
        return $"{typeName}:{method.Name}";
    }

    public static IEnumerable<MethodBase> OriginalMethods(this MethodBase patchMethod, HarmonyPatchType patchType, string instanceId = "") => PatchManager.PatchInfos.SelectMany(pair =>
                                                                                                                                            pair.Value.OfType(patchType)
                                                                                                                                                .Where(p => p.owner.Contains(instanceId) && p.PatchMethod == patchMethod)
                                                                                                                                                .Select(p => pair.Key)
                                                                                                                                          ).Distinct();

    public static MethodBase[] OriginalMethods(this AttributePatch patchInfo, MethodBase patchMethod, string instanceId = ""){
        var original = patchInfo.info.GetOriginalMethod();
        return original is not null ? [original] : patchMethod.OriginalMethods(patchInfo.type ?? HarmonyPatchType.All, instanceId).ToArray();
    }
}
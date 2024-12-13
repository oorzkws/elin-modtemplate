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
        // Remove methods not contained in our list
        clonedPatcher.patchMethods.RemoveAll(a => !patchMethods.Contains(a.info.method));
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
        // List what we want to unpatch
        var validPatches = clonedPatcher.patchMethods.Where(patch =>
            patch.type.HasValue && patchMethods.Contains(patch.info.method)).ToArray();
        // Remove desired patches
        validPatches.Do(patch => harmony.Unpatch(patch.info.GetOriginalMethod(), patch.type!.Value, harmony.Id));
        // Remove entries that we now unpatched
        clonedPatcher.patchMethods.RemoveAll(a => patchMethods.Contains(a.info.method));
        // Re-patch remainder
        clonedPatcher.Patch();
    }
}
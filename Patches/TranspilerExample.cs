#nullable disable
using static ElinModTemplate.EzTranspiler;

namespace ElinModTemplate.Patches;

//[HarmonyPatch(typeof(Scene), nameof(Scene.OnUpdate))]
internal static class TranspilerExample {

    private static bool IsSafeTransition() {
        Logging.Log($"Transition from {EMono.player.lastTransition.lastZone.GetType().Name} to {EMono._zone.GetType().Name}");
        return EMono._zone is Zone_Tent || EMono.player.lastTransition.lastZone is Zone_Tent;
    }

    [UsedImplicitly]
    public static CodeInstructions Transpiler(CodeInstructions instructions, MethodBase method) {
        var editor = new CodeMatcher(instructions);

        // The check here gets appended with a null check (ldnull, cgt_un) - we know it's not null, so we drop those instructions
        var pattern = InstructionMatchSignature(() => EMono.player.lastTransition.lastZone is Region).SkipLast(2).ToArray();
        // ReSharper disable once ConvertClosureToMethodGroup we need to keep the call here or the function itself is converted
        var replacement = InstructionSignature(() => IsSafeTransition());
        editor.Start().Replace(pattern, replacement);
        return editor.InstructionEnumeration();
    }

}
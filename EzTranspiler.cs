using BepInEx.Logging;
using System.Diagnostics;
using System.Reflection.Emit;

namespace ElinModTemplate;

internal static class EzTranspiler {

    internal static bool NotNull(params object[] input) {
        if (input.All(o => o is not null)) {
            return true;
        }

        Logging.Log("Signature match not found", LogLevel.Warning);
        foreach (var obj in input) {
            if (obj is MemberInfo memberObj) {
                Logging.Log($"\tValid entry:{memberObj}", LogLevel.Warning);
            }
        }

        return false;
    }

    /// <summary>
    ///     Returns the type that called a transpiler in a given stack trace
    /// </summary>
    /// <returns></returns>
    private static string GetTranspilerStackFrame() {
        var trace = new StackTrace();
        foreach (var frame in trace.GetFrames()!) {
            var method = frame.GetMethod();
            if (method.Name == "Transpiler") {
                return method.DeclaringType?.FullName ?? "unknown";
            }
        }

        return "unknown";
    }

    // CodeMatcher will throw errors if we try to take actions in an invalid state (i.e. no match)
    internal static CodeMatcher OnSuccess(this CodeMatcher match, Action<CodeMatcher> action, bool suppress = false) {
        switch (match.IsInvalid) {
            case true when !suppress:
                Logging.Log($"Transpiler did not find target @ {GetTranspilerStackFrame()}",
                    LogLevel.Warning);
                break;
            case false: action.Invoke(match); break;
        }

        return match;
    }

    /// <summary>
    ///     Replaces the pattern with the replacement. This is a naive implementation - if you need labels, DIY it.
    /// </summary>
    /// <param name="match">the CodeMatcher instance to use</param>
    /// <param name="pattern">instructions to match, the beginning of this match is where the replacement begins</param>
    /// <param name="replacement">the new instructions</param>
    /// <param name="replace">
    ///     Whether we should keep labels and set the opcodes/operands instead of recreating the
    ///     CodeInstruction
    /// </param>
    /// <param name="suppress">Whether to suppress the log message on a failed match</param>
    /// <returns></returns>
    // ReSharper disable once UnusedMethodReturnValue.Local
    internal static CodeMatcher Replace(
        this CodeMatcher match, CodeMatch[] pattern, CodeInstructions replacement,
        bool replace = true, bool suppress = false
    ) {
        return match.MatchStartForward(pattern).OnSuccess(matcher => {
            var newOps = replacement.ToList();
            for (var i = 0; i < Math.Max(newOps.Count, pattern.Length); i++) {
                if (i < newOps.Count) {
                    var op = newOps[i];
                    if (i < pattern.Length) {
                        if (replace) {
                            // This keeps labels
                            matcher.SetAndAdvance(op.opcode, op.operand);
                        } else {
                            matcher.RemoveInstruction();
                            matcher.InsertAndAdvance(op);
                        }
                    } else {
                        matcher.InsertAndAdvance(op);
                    }
                } else {
                    matcher.RemoveInstruction();
                }
            }
        }, suppress);
    }

    /// <summary>
    ///     Replaces the pattern with the replacement. This is a naive implementation - if you need labels, DIY it.
    /// </summary>
    /// <param name="match">the CodeMatcher instance to use</param>
    /// <param name="pattern">instructions to match, the beginning of this match is where the replacement begins</param>
    /// <param name="replacement">the new instructions</param>
    /// <param name="replace">
    ///     Whether we should keep labels and set the opcodes/operands instead of recreating the
    ///     CodeInstruction
    /// </param>
    /// <param name="suppress">Whether to suppress the log message on a failed match</param>
    /// <returns></returns>
    internal static CodeMatcher Replace(this CodeMatcher match, CodeInstructions pattern, CodeInstructions replacement, bool replace = true, bool suppress = false) {
        var patternArray = pattern.Select(entry => {
            switch (entry) {
                case CodeMatch codeMatch:
                    codeMatch.predicate ??= candidate => entry == candidate;
                    return codeMatch;
                default: return new CodeMatch(candidate => new Container(entry) == candidate);
            }
        }).ToArray();
        return Replace(match, patternArray, replacement, replace, suppress);
    }

    /// <summary>
    ///     Prevents the compiler from removing a given local
    /// </summary>
    /// <param name="variable">The local to protect</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    // ReSharper disable once UnusedParameter.Local
    internal static void Pin<T>(ref T variable) {
        // Do nothing
    }

    /// <summary>
    ///     Returns the given method as a basic ILCode signature, excluding ret statements
    /// </summary>
    /// <param name="method">The method to convert</param>
    /// <returns>A set of CodeInstructions representing the method given</returns>
    /// <remarks>Declared locals need to be pinned with Pin(ref local)</remarks>
    internal static CodeInstructions InstructionSignature(Delegate method) {
        var instructions = new List<CodeInstruction>();
        var locals = method.Method.GetMethodBody()!.LocalVariables;

        // Fetch the given delegate as IL code and mutate it slightly before storing it
        foreach (var instruction in PatchProcessor.GetCurrentInstructions(method.Method)) {
            // Returns are used as a declaration within patterns, so we drop the instruction
            if (instruction == Fish.Return) {
                continue;
            }

            // Nops can be used for alignment or optimization, but we don't want that here as it can mess up our matching
            if (instruction.opcode == OpCodes.Nop) {
                continue;
            }

            // Arg indexes get shifted by 1, as the method is made static with "this" as the 0th arg. We shift them backwards here to match.
            if (instruction.opcode.LoadsArgument() || instruction.opcode.StoresArgument()) {
                // Container cast allows us to avoid branching on all the different starg_n/ldarg_n
                var index = new Container(instruction).GetIndex() - 1;
                // Create a copy with the proper index
                var copy = instruction.IsLdarg() ? FishTranspiler.Argument(index) : FishTranspiler.StoreArgument(index);
                // Push the changes back to the instruction. This allows us to maintain block/label attributes.
                instruction.opcode = copy.OpCode;
                instruction.operand = copy.Operand;
            }

            // For methods that just declare a local, they have to pin it using HarmonyPatches.Pin. We remove this from the match.
            // Doing so is a two-instruction process (Ldloca, Call) so we remove the last and current instructions
            if (instructions.Count > 0) {
                var lastInstruction = new Container(instructions.Last());
                if (lastInstruction.OpCode.LoadsLocalVariable()) {
                    // Fetch the instruction for Pin<T> where T is whatever type lastInstruction accesses...
                    var genericPin = Fish.Call(typeof(EzTranspiler), "Pin", generics: [locals[lastInstruction.GetIndex()].LocalType ?? throw new InvalidOperationException()]);
                    // ...and check it against our current instruction
                    if (instruction == genericPin) {
                        // Remove the ldloca from our list
                        instructions.RemoveAt(instructions.Count - 1);
                        // Skip storing the pin call
                        continue;
                    }
                }
            }

            // Store to our list
            instructions.Add(instruction);
        }

        return instructions;
    }

    /// <summary>
    ///     Returns the given method as a basic ILCode signature wrapped in a CodeMatch, excluding ret statements
    /// </summary>
    /// <param name="method">The method to convert</param>
    /// <returns>A set of CodeInstructions representing the method given</returns>
    /// <remarks>Declared locals need to be pinned with Pin(ref local)</remarks>
    internal static CodeMatch[] InstructionMatchSignature(Delegate method) {
        return InstructionSignature(method).Select(i => new CodeMatch(i.opcode, i.operand)).ToArray();
    }
}
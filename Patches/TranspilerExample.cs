#nullable disable
extern alias UnityCore;
using BepInEx.Logging;
using UnityCore::UnityEngine;
using static FisheryLib.FishTranspiler;

namespace ElinModTemplate.Patches;

[HarmonyPatch(typeof(AI_Fish), nameof(AI_Fish.Makefish))]
internal static class TranspilerExample {
    private static int Roll(int num, int sides, int bonus = 0) {
        var result = 0;
        for (var i = 0; i < num; i++) {
            result += Rand.Range(0, sides);
        }
        return result + bonus;
    }
    // This just recreates the roll func
    private static int TargetedRoll(int num, int sides, int bonus = 0, int rollTarget = int.MaxValue, Card card = null) {
        var numRolls = 1;
        var positiveLuc = true;
        if (card != null) {
            var luc = card.LUC;
            positiveLuc = luc >= 0;
            // Add full dice from increments of 100 LUC
            numRolls += Mathf.Abs(luc / 100);
            // Rolls remaining LUC against 1d100 for an extra die
            numRolls += Mathf.Abs(luc % 100) > Dice.rnd(100) ? 1 : 0;
        }
        var bestRoll = 0;
        for (var i = 0; i < numRolls; i++) {
            var newRoll = Roll(num, sides, bonus);
            if (i == 0) {
                bestRoll = newRoll;
                continue;
            }
            var bestDistance = Math.Abs(rollTarget - bestRoll);
            var newDistance = Math.Abs(rollTarget - newRoll);
            // Better or worse, depending on which side of 0 our LUC is on
            if (positiveLuc && newDistance < bestDistance || !positiveLuc && newDistance > bestDistance) {
                bestRoll = newRoll;
            }
        }
        if (bestRoll == rollTarget && rollTarget > 33) {
            Logging.Log($"Hit roll target from {num}d{sides}+{bonus}", LogLevel.Debug);
        }
        //Logging.Log($"{num}d{sides}+{bonus}, rolled {numRolls}x, aiming for {rollTarget}: {bestRoll}");
        return bestRoll;
    }

    [UsedImplicitly]
    public static CodeInstructions Transpiler(CodeInstructions instructions, MethodBase method) {
        var editor = new CodeMatcher(instructions).Start();

        // EClass.rnd(3 + num) -- we want it to roll higher according to LUC
        editor.Replace([
            Constant(3),
            LocalVariable(0),
            Add,
            Call(EClass.rnd),
        ], [ // TargetedRoll(1, 3 + num, 0, int.MaxValue, chara)
            Constant(1),
            Constant(3),
            LocalVariable(0),
            Add,
            Constant(0),
            Constant(int.MaxValue),
            Argument(0),
            Call(TargetedRoll)
        ]);
        
        int[] rollsToFix = [30, 35, 2, 3, 3, 50, 40, 40];
        // For every == 0 check, we replace with our method that accounts for luck
        foreach (var rollInt in rollsToFix) {
            editor.Replace([
                Constant(rollInt),
                Call(EClass.rnd)
            ], [ //TargetedRoll(1, rollInt, rollTarget:0, card: c)
                Constant(1),
                Constant(rollInt),
                Constant(0),
                Constant(0),
                Argument(0),
                Call(TargetedRoll)
            ]);
        }
        
        // EClass.rnd(5 + num / 3) -- Junk, want to roll higher to avoid
        editor.Replace([
            Constant(5),
             LocalVariable(0),
            Constant(3),
            Divide,
            Add,
            Call(EClass.rnd)
        ], [// TargetedRoll(1, 5 + num / 3, 0, int.MaxValue, chara)
            Constant(1),
            Constant(5),
            LocalVariable(0),
            Constant(3),
            Divide,
            Add,
            Constant(0),
            Constant(int.MaxValue),
            Argument(0),
            Call(TargetedRoll)
        ]);
        // EClass.rnd(num * 2)
        editor.Replace([
            LocalVariable(0),
            Constant(2),
            Multiply,
            Call(EClass.rnd)
        ], [// TargetedRoll(1, num * 2, 0, int.MaxValue, chara)
            Constant(1),
            LocalVariable(0),
            Constant(2),
            Multiply,
            Constant(0),
            Constant(int.MaxValue),
            Argument(0),
            Call(TargetedRoll)
        ]);
        // EClass.rnd(num / (num3 + 10))
        
        editor.Replace([
            LocalVariable(0),
            LocalVariable(6),
            Constant(10),
            Add,
            Divide,
            Call(EClass.rnd)
        ], [// TargetedRoll(1, num / (num3 + 10), 0, int.MaxValue, chara)
            Constant(1),
            LocalVariable(0),
            LocalVariable(6),
            Constant(10),
            Add,
            Divide,
            Constant(0),
            Constant(int.MaxValue),
            Argument(0),
            Call(TargetedRoll)
        ]);
        // EClass.rnd(100)
        
        editor.Replace([
            Constant(100),
            Call(EClass.rnd),
        ], [ // TargetedRoll(1, 100, 0, 0, chara)
            Constant(1),
            Constant(100),
            Constant(0),
            Constant(0),
            Argument(0),
            Call(TargetedRoll)
        ]);
        // Done
        return editor.InstructionEnumeration();
    }

}
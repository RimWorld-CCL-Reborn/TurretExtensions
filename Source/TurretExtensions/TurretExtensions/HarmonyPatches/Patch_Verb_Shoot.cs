using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using RimWorld;
using Verse;
using HarmonyLib;
using UnityEngine;

namespace TurretExtensions
{

    public static class Patch_Verb_Shoot
    {

        [HarmonyPatch(typeof(Verb_Shoot), nameof(Verb_Shoot.WarmupComplete))]
        public static class WarmupComplete
        {

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                // Reason why this transpiler exists instead of just a postfix on the CasterPawn and CasterIsPawn properties is because CasterPawn gets referenced in several other places too, which causes other issues

                #if DEBUG
                    Log.Message("Transpiler start: Verb_Shoot.WarmupComplete (3 matches)");
                #endif

                var instructionList = instructions.ToList();

                var getCasterIsPawnInfo = AccessTools.Property(typeof(Verb), nameof(Verb.CasterIsPawn)).GetGetMethod();
                var casterIsActuallyPawn = AccessTools.Method(typeof(WarmupComplete), nameof(WarmupComplete.CasterIsActuallyPawn));

                var getCasterPawnInfo = AccessTools.Property(typeof(Verb), nameof(Verb.CasterPawn)).GetGetMethod();
                var actualCasterPawnInfo = AccessTools.Method(typeof(WarmupComplete), nameof(WarmupComplete.ActualCasterPawn));

                for (int i = 0; i < instructionList.Count; i++)
                {
                    var instruction = instructionList[i];

                    // Update all 'CasterIsPawn' and 'CasterPawn' calls to factor in CompMannable
                    if (instruction.opcode == OpCodes.Callvirt)
                    {
                        #if DEBUG
                            Log.Message("Verb_Shoot.WarmupComplete match 1 of 3");
                        #endif

                        // CasterIsPawn
                        if (instruction.OperandIs(getCasterIsPawnInfo))
                        {
                            #if DEBUG
                                Log.Message("Verb_Shoot.WarmupComplete match 2 of 3");
                            #endif

                            yield return instruction; // this.CasterIsPawn
                            yield return new CodeInstruction(OpCodes.Ldarg_0); // this
                            instruction = new CodeInstruction(OpCodes.Call, casterIsActuallyPawn); // CasterIsActuallyPawn(this.CasterIsPawn, this)
                        }

                        // CasterPawn
                        else if (instruction.OperandIs(getCasterPawnInfo))
                        {
                            #if DEBUG
                                Log.Message("Verb_Shoot.WarmupComplete match 3 of 3");
                            #endif

                            yield return instruction; // this.CasterPawn
                            yield return new CodeInstruction(OpCodes.Ldarg_0); // this
                            instruction = new CodeInstruction(OpCodes.Call, actualCasterPawnInfo); // ActualCasterPawn(this.CasterPawn, this)
                        }
                    }

                    yield return instruction;
                }
            }

            private static bool CasterIsActuallyPawn(bool original, Verb instance)
            {
                // Factor in CompMannable for exp purposes
                return original || (instance.Caster.TryGetComp<CompMannable>() is CompMannable mannableComp && mannableComp.MannedNow);
            }

            private static Pawn ActualCasterPawn(Pawn original, Verb instance)
            {
                // Factor in CompMannable for exp purposes
                if (original == null && instance.Caster.TryGetComp<CompMannable>() is CompMannable mannableComp)
                    return mannableComp.ManningPawn;
                return original;
            }

        }

        [HarmonyPatch(typeof(Verb_Shoot), "TryCastShot")]
        public static class TryCastShot
        {

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                // WarmupComplete transpiler can be reused
                return WarmupComplete.Transpiler(instructions);
            }

        }



    }

}

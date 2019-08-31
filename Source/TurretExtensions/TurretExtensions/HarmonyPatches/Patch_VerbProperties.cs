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
using Harmony;
using UnityEngine;

namespace TurretExtensions
{

    public static class Patch_Command_VerbTarget
    {

        [HarmonyPatch(typeof(Command_VerbTarget), nameof(Command_VerbTarget.GizmoUpdateOnMouseover))]
        public static class GizmoUpdateOnMouseover
        {

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilGen)
            {
                var instructionList = instructions.ToList();

                var drawRadiusRingInfo = AccessTools.Method(typeof(GenDraw), nameof(GenDraw.DrawRadiusRing));
                var tryDrawFiringConeInfo = AccessTools.Method(typeof(GizmoUpdateOnMouseover), nameof(TryDrawFiringCone));

                int radRingCount = instructionList.Count(i => HarmonyPatchesUtility.CallingInstruction(i) && i.operand == drawRadiusRingInfo);
                int radRingsFound = 0;

                for (int i = 0; i < instructionList.Count; i++)
                {
                    var instruction = instructionList[i];

                    // Look for branching instructions - start looking ahead
                    if (radRingsFound < radRingCount && HarmonyPatchesUtility.BranchingInstruction(instruction))
                    {
                        int j = 1;
                        while (i + j < instructionList.Count)
                        {
                            var xInstructionAhead = instructionList[i + j];

                            // Terminate if another branching instruction is found, or if the branch's destination was reached
                            if (HarmonyPatchesUtility.BranchingInstruction(xInstructionAhead) || xInstructionAhead.labels.Contains((Label)instruction.operand))
                                break;

                            // Look for a call to drawRadiusRing
                            if (HarmonyPatchesUtility.CallingInstruction(xInstructionAhead) && xInstructionAhead.operand == drawRadiusRingInfo)
                            {
                                yield return instruction; // num < x or num > x
                                yield return new CodeInstruction(OpCodes.Ldarg_0); // this
                                yield return instructionList[i - 2].Clone(); // num
                                yield return new CodeInstruction(OpCodes.Call, tryDrawFiringConeInfo); // TryDrawFiringCone(this, num)
                                instruction = new CodeInstruction(OpCodes.Brtrue, instruction.operand);
                                radRingsFound++;
                                break;
                            }

                            j++;
                        }
                    }

                    yield return instruction;
                }
            }

            private static bool TryDrawFiringCone(Verb verb)
            {
                if (verb.caster is Building_Turret turret)
                    return TurretExtensionsUtility.TryDrawFiringCone(turret, verb.verbProps.EffectiveMinRange(true)) && TurretExtensionsUtility.TryDrawFiringCone(turret, verb.verbProps.range);
                return false;
            }

        }

    }

}

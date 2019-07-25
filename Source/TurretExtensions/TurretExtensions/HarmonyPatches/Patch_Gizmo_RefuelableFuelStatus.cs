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

    public static class Patch_Gizmo_RefuelableFuelStatus
    {

        public static class ManualPatch_GizmoOnGUI_Delegate
        {

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var instructionList = instructions.ToList();

                bool ldflda = false;
                var adjustedFuelCapacity = AccessTools.Method(typeof(HarmonyPatchesUtility), nameof(HarmonyPatchesUtility.AdjustedFuelCapacity));

                for (int i = 0; i < instructionList.Count; i++)
                {
                    var instruction = instructionList[i];

                    if (instruction.opcode == OpCodes.Ldflda && instruction.operand == AccessTools.Field(typeof(CompProperties_Refuelable), nameof(CompProperties_Refuelable.fuelCapacity)))
                    {
                        instruction.opcode = OpCodes.Ldfld;
                        ldflda = true;
                    }

                    if (instruction.IsFuelCapacityInstruction())
                    {
                        yield return instruction;
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(instructionList[i - 3]);
                        yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Gizmo_RefuelableFuelStatus), nameof(Gizmo_RefuelableFuelStatus.refuelable)));
                        yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(CompRefuelable), nameof(CompRefuelable.parent)));
                        if (ldflda)
                        {
                            yield return new CodeInstruction(OpCodes.Call, adjustedFuelCapacity);
                            yield return new CodeInstruction(OpCodes.Stloc_S, 7);
                        }
                        instruction = (ldflda) ? new CodeInstruction(OpCodes.Ldloca_S, 7) : new CodeInstruction(OpCodes.Call, adjustedFuelCapacity);
                        ldflda = false;
                    }

                    yield return instruction;
                }
            }

        }

    }

}

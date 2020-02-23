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

    public static class Patch_Gizmo_RefuelableFuelStatus
    {

        public static class manual_GizmoOnGUI_Delegate
        {

            public static Type delegateType;

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method, ILGenerator ilGen)
            {
                var instructionList = instructions.ToList();

                // Add local
                var fuelCapacityLocal = ilGen.DeclareLocal(typeof(float));

                var fuelCapacityInfo = AccessTools.Field(typeof(CompProperties_Refuelable), nameof(CompProperties_Refuelable.fuelCapacity));

                var thisInfo = AccessTools.Field(delegateType, "<>4__this");

                var adjustedFuelCapacityInfo = AccessTools.Method(typeof(manual_GizmoOnGUI_Delegate), nameof(AdjustedFuelCapacity));

                for (int i = 0; i < instructionList.Count; i++)
                {
                    var instruction = instructionList[i];

                    // Adjust all calls to fuel capacity to factor in upgraded status
                    if (instruction.operand == fuelCapacityInfo)
                    {
                        bool addr = false;
                        if (instruction.opcode == OpCodes.Ldflda)
                        {
                            instruction.opcode = OpCodes.Ldfld;
                            addr = true;
                        }
                        yield return instruction; // this.$this.refuelable.Props.fuelCapacity
                        yield return new CodeInstruction(OpCodes.Ldarg_0); // this
                        yield return new CodeInstruction(OpCodes.Ldfld, thisInfo); // this.$this
                        var callAdjustedFuelCapacity = new CodeInstruction(OpCodes.Call, adjustedFuelCapacityInfo); // AdjustedFuelCapacity(this.$this.refuelable.Props.fuelCapacity, this.$this)
                        if (addr)
                        {
                            yield return callAdjustedFuelCapacity;
                            yield return new CodeInstruction(OpCodes.Stloc_S, fuelCapacityLocal.LocalIndex);
                            instruction = new CodeInstruction(OpCodes.Ldloca_S, fuelCapacityLocal.LocalIndex);
                        }
                        else
                            instruction = callAdjustedFuelCapacity;
                    }

                    yield return instruction;
                }
            }

            private static float AdjustedFuelCapacity(float original, Gizmo_RefuelableFuelStatus instance)
            {
                return HarmonyPatchesUtility.AdjustedFuelCapacity(original, instance.refuelable.parent);
            }

        }

    }

}

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

    public static class Patch_CompRefuelable
    {

        #region Shared Transpiler
        // Probably my most favourite transpiler ever
        // As of 25th July 2019, still haven't been able to top this one in terms of copypasta potential
        public static IEnumerable<CodeInstruction> FuelCapacityTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var instructionList = instructions.ToList();

            var adjustedFuelCapacity = AccessTools.Method(typeof(HarmonyPatchesUtility), nameof(HarmonyPatchesUtility.AdjustedFuelCapacity));

            for (int i = 0; i < instructionList.Count; i++)
            {
                var instruction = instructionList[i];

                if (instruction.IsFuelCapacityInstruction())
                {
                    yield return instruction;
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(CompRefuelable), nameof(CompRefuelable.parent)));
                    instruction = new CodeInstruction(OpCodes.Call, adjustedFuelCapacity);
                }

                yield return instruction;
            }
        }


        #endregion

        [HarmonyPatch(typeof(CompRefuelable))]
        [HarmonyPatch(nameof(CompRefuelable.Refuel))]
        [HarmonyPatch(new Type[] { typeof(float) })]
        public static class Patch_Refuel
        {

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return FuelCapacityTranspiler(instructions);
            }

        }

        [HarmonyPatch(typeof(CompRefuelable))]
        [HarmonyPatch(nameof(CompRefuelable.CompInspectStringExtra))]
        public static class Patch_CompInspectStringExtra
        {

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return FuelCapacityTranspiler(instructions);
            }

        }

        [HarmonyPatch(typeof(CompRefuelable))]
        [HarmonyPatch(nameof(CompRefuelable.TargetFuelLevel), MethodType.Getter)]
        public static class Patch_TargetFuelLevel_Getter
        {

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return FuelCapacityTranspiler(instructions);
            }

        }

        [HarmonyPatch(typeof(CompRefuelable))]
        [HarmonyPatch(nameof(CompRefuelable.TargetFuelLevel), MethodType.Setter)]
        public static class Patch_TargetFuelLevel_Setter
        {

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return FuelCapacityTranspiler(instructions);
            }

        }

        [HarmonyPatch(typeof(CompRefuelable))]
        [HarmonyPatch(nameof(CompRefuelable.GetFuelCountToFullyRefuel))]
        public static class Patch_GetFuelCountToFullyRefuel
        {

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var instructionList = instructions.ToList();

                var adjustedFuelCapacity = AccessTools.Method(typeof(HarmonyPatchesUtility), nameof(HarmonyPatchesUtility.AdjustedFuelCapacity));
                var adjustedFuelCount = AccessTools.Method(typeof(Patch_GetFuelCountToFullyRefuel), nameof(AdjustedFuelCount));

                for (int i = 0; i < instructionList.Count; i++)
                {
                    CodeInstruction instruction = instructionList[i];

                    if (instruction.IsFuelCapacityInstruction())
                    {
                        yield return instruction;
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(CompRefuelable), nameof(CompRefuelable.parent)));
                        instruction = new CodeInstruction(OpCodes.Call, adjustedFuelCapacity);
                    }

                    if (instruction.opcode == OpCodes.Stloc_0)
                    {
                        yield return instruction;
                        yield return new CodeInstruction(OpCodes.Ldloc_0);
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(CompRefuelable), nameof(CompRefuelable.parent)));
                        yield return new CodeInstruction(OpCodes.Call, adjustedFuelCount);
                        instruction = new CodeInstruction(OpCodes.Stloc_0);
                    }

                    yield return instruction;
                }
            }

            private static float AdjustedFuelCount(float currentFuelCount, Thing thing) =>
                currentFuelCount / ((thing.IsUpgradedTurret(out CompUpgradable uC)) ? uC.Props.effectiveBarrelDurabilityFactor * uC.Props.barrelDurabilityFactor : 1f);

        }

    }

}

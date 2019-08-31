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

    public static class HarmonyPatchesUtility
    {

        public static bool IsFuelCapacityInstruction(this CodeInstruction instruction) =>
            instruction.opcode == OpCodes.Ldfld && instruction.operand == AccessTools.Field(typeof(CompProperties_Refuelable), nameof(CompProperties_Refuelable.fuelCapacity));

        public static float AdjustedFuelCapacity(float baseFuelCapacity, Thing t)
        {
            if (t.IsUpgraded(out CompUpgradable upgradableComp))
                return baseFuelCapacity * upgradableComp.Props.fuelCapacityFactor;
            return baseFuelCapacity;
        }

        public static bool CallingInstruction(CodeInstruction instruction)
        {
            return instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt;
        }

        public static bool BranchingInstruction(CodeInstruction instruction)
        {
            return instruction.opcode == OpCodes.Bge_Un || instruction.opcode == OpCodes.Ble_Un;
        }

    }

}

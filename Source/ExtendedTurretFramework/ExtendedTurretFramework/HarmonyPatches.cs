using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using RimWorld;
using Verse;
using Harmony;
using UnityEngine;

namespace ExtendedTurretFramework
{
    [StaticConstructorOnStartup]
    static class HarmonyPatches
    {

        static readonly Type patchType = typeof(HarmonyPatches);

        static HarmonyPatches()
        {
            HarmonyInstance h = HarmonyInstance.Create("XeoNovaDan.ExtendedTurretFramework");

            //HarmonyInstance.DEBUG = true;

            h.Patch(AccessTools.Method(typeof(ShotReport), "HitReportFor"), null, null,
                new HarmonyMethod(patchType, "TranspileTurretAccuracy"));

            h.Patch(AccessTools.Method(typeof(Building_TurretGun), "TryStartShootSomething"), null,
                new HarmonyMethod(patchType, "PostfixTryStartShootSomething"));

            h.Patch(AccessTools.Property(typeof(Building_TurretGun), "CanSetForcedTarget").GetGetMethod(true),
                null, new HarmonyMethod(patchType, "PostfixCanSetForcedTarget"));

        }

        public static IEnumerable<CodeInstruction> TranspileTurretAccuracy(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionList = instructions.ToList();
            MethodInfo accGetterInfo = AccessTools.Method(patchType, nameof(GetTurretAccuracy));

            bool foundInstruction = false;
            bool done = false;
            
            for (int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];

                if (foundInstruction && !done)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, accGetterInfo);
                    done = true;
                }

                if (instruction.opcode == OpCodes.Ldc_R4 && !done && !foundInstruction)
                {
                    instruction.opcode = OpCodes.Nop;
                    foundInstruction = true;
                }

                yield return instruction;
            }
        }

        private static float GetTurretAccuracy(Thing turret)
        {
            var extensionValues = turret.def.GetModExtension<TurretFrameworkExtension>() ?? TurretFrameworkExtension.defaultValues;
            CompMannable mannableComp = turret.TryGetComp<CompMannable>();
            string turretDefName = turret.def.defName;

            if (extensionValues.useMannerShootingAccuracy)
            {
                if (mannableComp != null)
                {
                    Pawn manningPawn = mannableComp.ManningPawn;
                    if (manningPawn != null)
                    {
                        return manningPawn.GetStatValue(StatDefOf.ShootingAccuracy);
                    }
                }
                else
                {
                    Log.Warning(String.Format("Turret (defName={0}) has useMannerShootingAccuracy set to true but doesn't have CompMannable.", turretDefName));
                }
            }

            if (extensionValues.shootingAccuracy < 0 || extensionValues.shootingAccuracy > 1)
            {
                if (extensionValues.shootingAccuracy < 0)
                {
                    Log.ErrorOnce(String.Format("Turret (defName={0}) has a shootingAccuracy value of less than 0. Defaulting to 0.96...", turretDefName), 614927384);
                }
                else
                {
                    Log.ErrorOnce(String.Format("Turret (defName={0}) has a shootingAccuracy value of greater than 1. Defaulting to 0.96...", turretDefName), 614927385);
                }
                return 0.96f;
            }

            return extensionValues.shootingAccuracy;
        }

        public static void PostfixTryStartShootSomething(Building_TurretGun __instance)
        {
            var extensionValues = __instance.def.GetModExtension<TurretFrameworkExtension>() ?? TurretFrameworkExtension.defaultValues;
            string turretDefName = __instance.def.defName;

            if (extensionValues.useMannerAimingDelayFactor)
            {
                CompMannable mannableComp = __instance.TryGetComp<CompMannable>();
                if (mannableComp != null)
                {
                    Pawn manner = mannableComp.ManningPawn;
                    if (manner != null)
                    {
                        int burstWarmupTicksLeft = Traverse.Create(__instance).Field("burstWarmupTicksLeft").GetValue<int>();
                        float mannerAimingDelayFactor = manner.GetStatValue(StatDefOf.AimingDelayFactor);
                        burstWarmupTicksLeft = (int)Math.Round(burstWarmupTicksLeft * mannerAimingDelayFactor);
                        Traverse.Create(__instance).Field("burstWarmupTicksLeft").SetValue(burstWarmupTicksLeft);
                    }
                }
                else
                {
                    Log.Warning(String.Format("Turret (defName={0}) has useMannerAimingDelayFactor set to true but doesn't have CompMannable.", turretDefName));
                }
            }
        }

        public static void PostfixCanSetForcedTarget(Building_TurretGun __instance, ref bool __result)
        {
            var extensionValues = __instance.def.GetModExtension<TurretFrameworkExtension>() ?? TurretFrameworkExtension.defaultValues;
            string turretDefName = __instance.def.defName;

            if (extensionValues.canForceAttack)
            {
                CompMannable mannableComp = __instance.TryGetComp<CompMannable>();
                if (mannableComp == null)
                {
                    if (__instance.Faction == Faction.OfPlayer)
                    {
                        __result = true;
                    }
                }
                else
                {
                    Log.Warning(String.Format("Turret (defName={0}) has canForceAttack set to true and CompMannable. canForceAttack is redundant in this case.", turretDefName));
                }
            }
        }

    }
}

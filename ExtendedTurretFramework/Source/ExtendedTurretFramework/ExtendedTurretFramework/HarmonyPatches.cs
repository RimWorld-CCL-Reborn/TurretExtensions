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

            if (mannableComp != null && extensionValues.useMannerShootingAccuracy)
            {
                Pawn manningPawn = mannableComp.ManningPawn;
                if (manningPawn != null)
                {
                    return manningPawn.GetStatValue(StatDefOf.ShootingAccuracy);
                }
            }

            return extensionValues.shootingAccuracy;
        }

        public static void PostfixTryStartShootSomething(Building_TurretGun __instance)
        {
            var extensionValues = __instance.def.GetModExtension<TurretFrameworkExtension>() ?? TurretFrameworkExtension.defaultValues;
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
            }
        }

    }
}

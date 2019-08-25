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

    public static class Patch_Building_TurretGun
    {

        [HarmonyPatch(typeof(Building_TurretGun), nameof(Building_TurretGun.Tick))]
        public static class Tick
        {

            public static void Postfix(Building_TurretGun __instance, LocalTargetInfo ___forcedTarget)
            {
                // If the turret has CompSmartForcedTarget and is attacking a pawn that just got downed, automatically make it target something else
                var smartTargetComp = __instance.TryGetComp<CompSmartForcedTarget>();
                if (smartTargetComp != null && ___forcedTarget.Thing is Pawn pawn)
                {
                    if (!pawn.Downed && !smartTargetComp.attackingNonDownedPawn && (!smartTargetComp.Props.onlyApplyWhenUpgraded || __instance.IsUpgraded(out CompUpgradable upgradableComp)))
                        smartTargetComp.attackingNonDownedPawn = true;

                    else if (pawn.Downed && smartTargetComp.attackingNonDownedPawn)
                    {
                        smartTargetComp.attackingNonDownedPawn = false;
                        NonPublicMethods.Building_TurretGun_ResetForcedTarget(__instance);
                    }
                }
            }

        }

        [HarmonyPatch(typeof(Building_TurretGun), nameof(Building_TurretGun.SpawnSetup))]
        public static class Patch_SpawnSetup
        {

            public static void Postfix(Building_TurretGun __instance, TurretTop ___top)
            {
                // Determine which way the turret initially faces when spawned
                var turretFrameworkExtension = TurretFrameworkExtension.Get(__instance.def);
                switch (turretFrameworkExtension.gunFaceDirectionOnSpawn)
                {
                    case TurretGunFaceDirection.North:
                        NonPublicProperties.TurretTop_set_CurRotation(___top, Rot4.North.AsAngle);
                        break;
                    case TurretGunFaceDirection.East:
                        NonPublicProperties.TurretTop_set_CurRotation(___top, Rot4.East.AsAngle);
                        break;
                    case TurretGunFaceDirection.South:
                        NonPublicProperties.TurretTop_set_CurRotation(___top, Rot4.South.AsAngle);
                        break;
                    case TurretGunFaceDirection.West:
                        NonPublicProperties.TurretTop_set_CurRotation(___top, Rot4.West.AsAngle);
                        break;
                    default:
                        NonPublicProperties.TurretTop_set_CurRotation(___top, __instance.Rotation.AsAngle);
                        break;
                }
            }

        }

        [HarmonyPatch(typeof(Building_TurretGun), "BurstCooldownTime")]
        public static class BurstCooldownTime
        {

            public static void Postfix(Building_TurretGun __instance, ref float __result)
            {
                if (__instance.IsUpgraded(out CompUpgradable upgradableComp))
                {
                    __result *= upgradableComp.Props.turretBurstCooldownTimeFactor;
                }
            }

        }

        [HarmonyPatch(typeof(Building_TurretGun), "TryStartShootSomething")]
        public static class TryStartShootSomething
        {

            public static void Postfix(Building_TurretGun __instance, ref int ___burstWarmupTicksLeft)
            {
                var extensionValues = TurretFrameworkExtension.Get(__instance.def);

                // Multiply the burstWarmupTicksLeft by the manning pawn's aiming delay factor if applicable
                if (extensionValues.useMannerAimingDelayFactor)
                {
                    var mannableComp = __instance.TryGetComp<CompMannable>();
                    if (mannableComp != null)
                    {
                        var manner = mannableComp.ManningPawn;
                        if (manner != null)
                        {
                            float mannerAimingDelayFactor = manner.GetStatValue(StatDefOf.AimingDelayFactor);
                            ___burstWarmupTicksLeft = Mathf.RoundToInt(___burstWarmupTicksLeft * mannerAimingDelayFactor);
                        }
                    }
                }

                // Multiply based on upgrade
                if (__instance.IsUpgraded(out CompUpgradable upgradableComp))
                    ___burstWarmupTicksLeft = Mathf.RoundToInt(___burstWarmupTicksLeft * upgradableComp.Props.turretBurstWarmupTimeFactor);
            }

        }

        [HarmonyPatch(typeof(Building_TurretGun), "CanSetForcedTarget", MethodType.Getter)]
        public static class CanSetForcedTarget
        {

            public static void Postfix(Building_TurretGun __instance, ref bool __result)
            {

                // If the turret isn't mannable, is player-controlled and is set to be able to force target, do so
                if (__instance.Faction == Faction.OfPlayer)
                {
                    var extensionValues = TurretFrameworkExtension.Get(__instance.def);
                    var upgradableComp = __instance.TryGetComp<CompUpgradable>();

                    if (extensionValues.canForceAttack || (upgradableComp != null && upgradableComp.upgraded && upgradableComp.Props.canForceAttack))
                    {
                        if (!__instance.def.HasComp(typeof(CompMannable)))
                            __result = true;
                        else
                            Log.Warning($"Turret (defName={__instance.def.defName}) has canForceAttack set to true and CompMannable; canForceAttack is redundant in this case.");
                    }
                }
                
            }

        }

        [HarmonyPatch(typeof(Building_TurretGun), "GetInspectString")]
        public static class Patch_GetInspectString
        {

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var instructionList = instructions.ToList();

                for (int i = 0; i < instructionList.Count; i++)
                {
                    var instruction = instructionList[i];

                    if (instruction.opcode == OpCodes.Ldc_R4 && (float)instruction.operand == 5)
                        instruction.operand = 0;

                    yield return instruction;
                }
            }

        }

    }

}

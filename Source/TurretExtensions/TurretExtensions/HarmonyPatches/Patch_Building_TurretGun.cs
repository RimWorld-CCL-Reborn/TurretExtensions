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

        [HarmonyPatch(typeof(Building_TurretGun))]
        [HarmonyPatch(nameof(Building_TurretGun.Tick))]
        public static class Patch_Tick
        {

            public static void Prefix(Building_TurretGun __instance, LocalTargetInfo ___forcedTarget)
            {
                // If the turret has CompSmartForcedTarget and is attacking a pawn that just got downed, automatically make it target something else
                var smartTargetComp = __instance.TryGetComp<CompSmartForcedTarget>();
                if (smartTargetComp != null && ___forcedTarget.Thing is Pawn pawn)
                {
                    var upgradableComp = __instance.TryGetComp<CompUpgradable>();
                    if (!pawn.Downed && ((upgradableComp != null && upgradableComp.upgraded) || !smartTargetComp.Props.onlyApplyWhenUpgraded) && !smartTargetComp.attackingNonDownedPawn)
                        smartTargetComp.attackingNonDownedPawn = true;

                    else if (pawn.Downed && smartTargetComp.attackingNonDownedPawn)
                    {
                        smartTargetComp.attackingNonDownedPawn = false;
                        AccessTools.Method(typeof(Building_TurretGun), "ResetForcedTarget").Invoke(__instance, null);
                    }
                }
            }

        }

        [HarmonyPatch(typeof(Building_TurretGun))]
        [HarmonyPatch(nameof(Building_TurretGun.SpawnSetup))]
        public static class Patch_SpawnSetup
        {

            public static void Postfix(Building_TurretGun __instance, TurretTop ___top)
            {
                // Determine which way the turret initially faces when spawned
                var turretFrameworkExtension = __instance.def.GetModExtension<TurretFrameworkExtension>() ?? TurretFrameworkExtension.defaultValues;
                switch (turretFrameworkExtension.gunFaceDirectionOnSpawn)
                {
                    case TurretGunFaceDirection.North:
                        Traverse.Create(___top).Property("CurRotation").SetValue(Rot4.North.AsAngle);
                        break;
                    case TurretGunFaceDirection.East:
                        Traverse.Create(___top).Property("CurRotation").SetValue(Rot4.East.AsAngle);
                        break;
                    case TurretGunFaceDirection.South:
                        Traverse.Create(___top).Property("CurRotation").SetValue(Rot4.South.AsAngle);
                        break;
                    case TurretGunFaceDirection.West:
                        Traverse.Create(___top).Property("CurRotation").SetValue(Rot4.West.AsAngle);
                        break;
                    default:
                        Traverse.Create(___top).Property("CurRotation").SetValue(__instance.Rotation.AsAngle);
                        break;
                }
            }

        }

        [HarmonyPatch(typeof(Building_TurretGun))]
        [HarmonyPatch("BurstCooldownTime")]
        public static class Patch_BurstCooldownTime
        {

            public static void Postfix(Building_TurretGun __instance, ref float __result)
            {
                if (__instance.IsUpgradedTurret(out CompUpgradable upgradableComp))
                {
                    __result *= upgradableComp.Props.turretBurstCooldownTimeFactor;
                }
            }

        }

        [HarmonyPatch(typeof(Building_TurretGun))]
        [HarmonyPatch("TryStartShootSomething")]
        public static class Patch_TryStartShootSomething
        {

            public static void Postfix(Building_TurretGun __instance, ref int ___burstWarmupTicksLeft)
            {
                var extensionValues = __instance.def.GetModExtension<TurretFrameworkExtension>() ?? TurretFrameworkExtension.defaultValues;;

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
                    else
                    {
                        Log.Warning($"Turret (defName={__instance.def.defName}) has useMannerAimingDelayFactor set to true but doesn't have CompMannable.");
                    }
                        
                }

                // Multiply based on upgrade
                if (__instance.IsUpgradedTurret(out CompUpgradable upgradableComp))
                    ___burstWarmupTicksLeft = Mathf.RoundToInt(___burstWarmupTicksLeft * upgradableComp.Props.turretBurstWarmupTimeFactor);
            }

        }

        [HarmonyPatch(typeof(Building_TurretGun))]
        [HarmonyPatch("CanSetForcedTarget", MethodType.Getter)]
        public static class Patch_CanSetForcedTarget
        {

            public static void Postfix(Building_TurretGun __instance, ref bool __result)
            {

                // If the turret isn't mannable, is player-controlled and is set to be able to force target, do so
                if (__instance.Faction == Faction.OfPlayer)
                {
                    var extensionValues = __instance.def.GetModExtension<TurretFrameworkExtension>() ?? TurretFrameworkExtension.defaultValues;
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

        [HarmonyPatch(typeof(Building_TurretGun))]
        [HarmonyPatch("GetInspectString")]
        public static class Patch_GetInspectString
        {

            public static void Postfix(Building_TurretGun __instance, ref bool __result)
            {

                // If the turret isn't mannable, is player-controlled and is set to be able to force target, do so
                if (__instance.Faction == Faction.OfPlayer)
                {
                    var extensionValues = __instance.def.GetModExtension<TurretFrameworkExtension>() ?? TurretFrameworkExtension.defaultValues;
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

    }

}

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

    public static class Patch_VerbProperties
    {

        [HarmonyPatch(typeof(VerbProperties), nameof(VerbProperties.AdjustedFullCycleTime))]
        public static class AdjustedFullCycleTime
        {

            public static bool Prefix(VerbProperties __instance, Verb ownerVerb, Pawn attacker, ref float __result)
            {
                // Vanilla gun turret
                if (ownerVerb.Caster is Building_TurretGun gunTurret)
                {
                    __result = TurretExtensionsUtility.AdjustedTurretBurstWarmupTicks(gunTurret.def.building.turretBurstWarmupTime.SecondsToTicks(), gunTurret).TicksToSeconds() + 
                        NonPublicMethods.Building_TurretGun_BurstCooldownTime(gunTurret) + ((__instance.burstShotCount - 1) * __instance.ticksBetweenBurstShots).TicksToSeconds();
                    return false;
                }

                return true;
            }

        }

    }

}

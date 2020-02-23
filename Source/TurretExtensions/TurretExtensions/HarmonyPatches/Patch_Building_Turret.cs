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

    public static class Patch_Building_Turret
    {

        [HarmonyPatch(typeof(Building_Turret), nameof(Building_Turret.TargetPriorityFactor), MethodType.Getter)]
        public static class get_TargetPriorityFactor
        {

            public static void Postfix(Building_Turret __instance, ref float __result)
            {
                // Set to 0 if turret is manned
                if (__instance.TryGetComp<CompMannable>() is var mannableComp && mannableComp.MannedNow)
                    __result = 0;
            }

        }

    }

}

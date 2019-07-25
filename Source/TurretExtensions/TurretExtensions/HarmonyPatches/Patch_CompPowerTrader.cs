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

    public static class Patch_CompPowerTrader
    {

        [HarmonyPatch(typeof(CompPowerTrader))]
        [HarmonyPatch(nameof(CompPowerTrader.SetUpPowerVars))]
        public static class Patch_SetUpPowerVars
        {

            public static void Postfix(CompPowerTrader __instance)
            {
                // If the turret has been upgraded, multiply its power consumption by the upgrade props' power consumption factor
                if (__instance.parent.IsUpgradedTurret(out CompUpgradable upgradableComp))
                    __instance.PowerOutput *= upgradableComp.Props.basePowerConsumptionFactor;
            }

        }

    }

}

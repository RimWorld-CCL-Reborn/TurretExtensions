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

    public static class Patch_CompPowerTrader
    {

        [HarmonyPatch(typeof(CompPowerTrader), nameof(CompPowerTrader.SetUpPowerVars))]
        public static class SetUpPowerVars
        {

            public static void Postfix(CompPowerTrader __instance)
            {
                // If the turret has been upgraded, multiply its power consumption by the upgrade props' power consumption factor
                if (__instance.parent.IsUpgraded(out CompUpgradable upgradableComp))
                    __instance.PowerOutput *= upgradableComp.Props.basePowerConsumptionFactor;
            }

        }

    }

}

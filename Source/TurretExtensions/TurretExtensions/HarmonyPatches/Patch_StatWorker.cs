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

    public static class Patch_StatWorker
    {

        [HarmonyPatch(typeof(StatWorker), nameof(StatWorker.GetValueUnfinalized))]
        public static class GetValueUnfinalized
        {

            public static void Postfix(StatWorker __instance, StatRequest req, ref float __result, StatDef ___stat)
            {
                // Update stats if the turret has been upgraded
                if (req.Thing.IsUpgraded(out CompUpgradable uC))
                {
                    CompProperties_Upgradable props = uC.Props;
                    if (props.statOffsets != null)
                        __result += props.statOffsets.GetStatOffsetFromList(___stat);
                    if (props.statFactors != null)
                        __result *= props.statFactors.GetStatFactorFromList(___stat);
                }
            }

        }

        [HarmonyPatch(typeof(StatWorker), nameof(StatWorker.GetExplanationUnfinalized))]
        public static class GetExplanationUnfinalized
        {
            // Update the explanation string if the turret has been upgraded
            public static void Postfix(StatWorker __instance, StatRequest req, ref string __result, StatDef ___stat)
            {
                if (req.Thing.IsUpgraded(out CompUpgradable uC))
                {
                    var props = uC.Props;
                    float? offset = props.statOffsets?.GetStatOffsetFromList(___stat);
                    float? factor = props.statFactors?.GetStatFactorFromList(___stat);
                    if (props.statOffsets != null && offset != 0)
                        __result += "\n\n" + "TurretExtensions.TurretUpgradedText".Translate().CapitalizeFirst() + ": " +
                            ((float)offset).ToStringByStyle(___stat.toStringStyle, ToStringNumberSense.Offset);
                    if (props.statFactors != null && factor != 1)
                        __result += "\n\n" + "TurretExtensions.TurretUpgradedText".Translate().CapitalizeFirst() + ": " +
                            ((float)factor).ToStringByStyle(___stat.toStringStyle, ToStringNumberSense.Factor);
                }
            }

        }

    }

}

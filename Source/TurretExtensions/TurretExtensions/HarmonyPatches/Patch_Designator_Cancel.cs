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

    public static class Patch_Designator_Cancel
    {

        [HarmonyPatch(typeof(Designator_Cancel))]
        [HarmonyPatch("DesignateThing")]
        public static class Patch_DesignateThing
        {

            public static void Prefix(Thing t)
            {
                // Cancelling a turret upgrade drops materials just like when cancelling a construction project
                if (t.TryGetComp<CompUpgradable>() is CompUpgradable upgradableComp)
                {
                    var upgradeDes = t.Map.designationManager.DesignationOn(t, TE_DesignationDefOf.UpgradeTurret);
                    if (upgradeDes != null)
                    {
                        upgradableComp.upgradeWorkTotal = -1f;
                        upgradableComp.innerContainer.TryDropAll(t.Position, t.Map, ThingPlaceMode.Near);
                    }
                }
            }

        }

    }

}

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

    public static class Patch_Designator_Cancel
    {

        [HarmonyPatch(typeof(Designator_Cancel), "DesignateThing")]
        public static class DesignateThing
        {

            public static void Postfix(Thing t)
            {
                // Cancelling a turret upgrade drops materials just like when cancelling a construction project
                if (t.IsUpgradable(out CompUpgradable upgradableComp) && !upgradableComp.upgraded)
                {
                    upgradableComp.Cancel();
                }
            }

        }

    }

}

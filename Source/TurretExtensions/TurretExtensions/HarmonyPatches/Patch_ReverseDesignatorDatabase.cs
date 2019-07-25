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

    public static class Patch_ReverseDesignatorDatabase
    {

        [HarmonyPatch(typeof(ReverseDesignatorDatabase))]
        [HarmonyPatch("InitDesignators")]
        public static class Patch_InitDesignators
        {

            public static void Postfix(ReverseDesignatorDatabase __instance, ref List<Designator> ___desList)
            {
                // Add upgrade turret designator to the list of reverse designators
                ___desList.Add(new Designator_UpgradeTurret());
            }

        }

    }

}

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

    public static class Patch_Thing
    {

        [HarmonyPatch(typeof(Thing), nameof(Thing.Graphic), MethodType.Getter)]
        public static class get_Graphic
        {

            public static void Postfix(Thing __instance, ref Graphic __result)
            {
                // Replace the graphic with the upgraded graphic if applicable
                if (__instance.IsUpgraded(out CompUpgradable uC) && uC.UpgradedGraphic != null)
                    __result = uC.UpgradedGraphic;
            }

        }

    }

}

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

    public static class Patch_PlaceWorker_ShowTurretRadius
    {

        [HarmonyPatch(typeof(PlaceWorker_ShowTurretRadius), nameof(PlaceWorker_ShowTurretRadius.AllowsPlacing))]
        public static class get_Graphic
        {

            public static bool Prefix(BuildableDef checkingDef)
            {
                var extension = TurretFrameworkExtension.Get(checkingDef);
                if (extension != null && extension.firingAngle > -1)
                {
                    return false;
                }
                return true;
            }

        }

    }

}

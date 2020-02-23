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

            public static bool Prefix(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, ref AcceptanceReport __result)
            {
                // Draw cones instead of circles if firing arc is limited
                float firingArc = TurretFrameworkExtension.Get(checkingDef).FiringArc;
                if (firingArc < 360)
                {
                    var verbProps = ((ThingDef)checkingDef).building.turretGunDef.Verbs.Find((VerbProperties v) => v.verbClass == typeof(Verb_Shoot));
                    if (verbProps.range > 0)
                    {
                        TurretExtensionsUtility.TryDrawFiringCone(loc, rot, verbProps.range, firingArc);
                    }
                    if (verbProps.minRange > 0)
                    {
                        TurretExtensionsUtility.TryDrawFiringCone(loc, rot, verbProps.minRange, firingArc);
                    }
                    __result = true;
                    return false;
                }
                return true;
            }

        }

    }

}

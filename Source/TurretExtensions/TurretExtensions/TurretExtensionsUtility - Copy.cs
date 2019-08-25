using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;

namespace TurretExtensions
{

    [StaticConstructorOnStartup]
    public static class StaticConstructorClass
    {

        static StaticConstructorClass()
        {
            foreach (var tDef in DefDatabase<ThingDef>.AllDefs)
            {
                // Make sure that all turrets have accuracy readouts by defining ShootingAccuracyTurret
                if (tDef.building != null && tDef.building.IsTurret && (tDef.statBases == null || !tDef.statBases.Any(s => s.stat == StatDefOf.ShootingAccuracyTurret)))
                {
                    if (tDef.statBases == null)
                        tDef.statBases = new List<StatModifier>();
                    tDef.statBases.Add(new StatModifier() { stat = StatDefOf.ShootingAccuracyTurret, value = StatDefOf.ShootingAccuracyTurret.defaultBaseValue });
                }
            }
        }

    }

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using Verse;
using RimWorld;
using HarmonyLib;

namespace TurretExtensions
{

    [StaticConstructorOnStartup]
    public static class NonPublicMethods
    {

        public static Func<Building_TurretGun, float> Building_TurretGun_BurstCooldownTime =
                (Func<Building_TurretGun, float>)Delegate.CreateDelegate(typeof(Func<Building_TurretGun, float>), null, AccessTools.Method(typeof(Building_TurretGun), "BurstCooldownTime"));
        public static Action<Building_TurretGun> Building_TurretGun_ResetForcedTarget =
                (Action<Building_TurretGun>)Delegate.CreateDelegate(typeof(Action<Building_TurretGun>), null, AccessTools.Method(typeof(Building_TurretGun), "ResetForcedTarget"));
        public static Action<Building_TurretGun> Building_TurretGun_UpdateGunVerbs =
                (Action<Building_TurretGun>)Delegate.CreateDelegate(typeof(Action<Building_TurretGun>), null, AccessTools.Method(typeof(Building_TurretGun), "UpdateGunVerbs"));

        public static Func<Def, ThingDef, IEnumerable<StatDrawEntry>> StatsReportUtility_StatsToDraw_def_stuff =
                (Func<Def, ThingDef, IEnumerable<StatDrawEntry>>)Delegate.CreateDelegate(typeof(Func<Def, ThingDef, IEnumerable<StatDrawEntry>>), null,
                AccessTools.Method(typeof(StatsReportUtility), "StatsToDraw", new[] { typeof(Def), typeof(ThingDef) }));
        public static Func<Thing, IEnumerable<StatDrawEntry>> StatsReportUtility_StatsToDraw_thing =
                (Func<Thing, IEnumerable<StatDrawEntry>>)Delegate.CreateDelegate(typeof(Func<Thing, IEnumerable<StatDrawEntry>>), null,
                AccessTools.Method(typeof(StatsReportUtility), "StatsToDraw", new[] { typeof(Thing) }));

    }

}

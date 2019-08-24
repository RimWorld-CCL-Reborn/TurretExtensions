using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using Verse;
using RimWorld;
using Harmony;

namespace TurretExtensions
{

    [StaticConstructorOnStartup]
    public static class NonPublicMethods
    {

        public static Action<Building_TurretGun> Building_TurretGun_ResetForcedTarget = (Action<Building_TurretGun>)
            Delegate.CreateDelegate(typeof(Action<Building_TurretGun>), null, AccessTools.Method(typeof(Building_TurretGun), "ResetForcedTarget"));
        public static Action<Building_TurretGun> Building_TurretGun_UpdateGunVerbs = (Action<Building_TurretGun>)
            Delegate.CreateDelegate(typeof(Action<Building_TurretGun>), null, AccessTools.Method(typeof(Building_TurretGun), "UpdateGunVerbs"));

    }

}

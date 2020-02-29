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
    public static class NonPublicProperties
    {

        public static Action<TurretTop, float> TurretTop_set_CurRotation =
            (Action<TurretTop, float>)Delegate.CreateDelegate(typeof(Action<TurretTop, float>), null, AccessTools.Property(typeof(TurretTop), "CurRotation").GetSetMethod(true));

    }

}

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
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {

        static HarmonyPatches()
        {
            //Harmony.DEBUG = true;
            TurretExtensions.harmonyInstance.PatchAll();

            // Gizmo_RefuelableFuelStatus delegate
            var delegateType = typeof(Gizmo_RefuelableFuelStatus).GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Instance).First();
            Patch_Gizmo_RefuelableFuelStatus.manual_GizmoOnGUI_Delegate.delegateType = delegateType;
            TurretExtensions.harmonyInstance.Patch(delegateType.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).First(),
                transpiler: new HarmonyMethod(typeof(Patch_Gizmo_RefuelableFuelStatus.manual_GizmoOnGUI_Delegate), "Transpiler"));

            // ThingDef.SpecialDisplayStats MoveNext method
            var specialDisplayStatsEnumeratorType = typeof(ThingDef).GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Instance).First(t => t.Name.Contains("SpecialDisplayStats"));
            Patch_ThingDef.manual_SpecialDisplayStats.enumeratorType = specialDisplayStatsEnumeratorType;
            TurretExtensions.harmonyInstance.Patch(specialDisplayStatsEnumeratorType.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).First(m => m.Name == "MoveNext"),
                transpiler: new HarmonyMethod(typeof(Patch_ThingDef.manual_SpecialDisplayStats), "Transpiler"));
        }


    }

}

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
            #if DEBUG
                Harmony.DEBUG = true;
            #endif

            TurretExtensions.harmonyInstance.PatchAll();

            // Gizmo_RefuelableFuelStatus delegate
            var delegateType = typeof(Gizmo_RefuelableFuelStatus).GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Instance).First();
            Patch_Gizmo_RefuelableFuelStatus.manual_GizmoOnGUI_Delegate.delegateType = delegateType;
            TurretExtensions.harmonyInstance.Patch(delegateType.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).First(),
                transpiler: new HarmonyMethod(typeof(Patch_Gizmo_RefuelableFuelStatus.manual_GizmoOnGUI_Delegate), "Transpiler"));

            // ThingDef.SpecialDisplayStats MoveNext method
            var thingDefEnumeratorType = typeof(ThingDef).GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Instance).First(t => t.Name.Contains("SpecialDisplayStats"));
            Patch_ThingDef.manual_SpecialDisplayStats.enumeratorType = thingDefEnumeratorType;
            TurretExtensions.harmonyInstance.Patch(thingDefEnumeratorType.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).First(m => m.Name == "MoveNext"),
                transpiler: new HarmonyMethod(typeof(Patch_ThingDef.manual_SpecialDisplayStats), "Transpiler"));

            // Fully refuel devmode gizmo
            TurretExtensions.harmonyInstance.Patch(typeof(CompRefuelable).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).Last(m => m.Name.Contains("CompGetGizmosExtra")),
                transpiler: new HarmonyMethod(typeof(Patch_CompRefuelable), nameof(Patch_CompRefuelable.FuelCapacityTranspiler)));
        }


    }

}

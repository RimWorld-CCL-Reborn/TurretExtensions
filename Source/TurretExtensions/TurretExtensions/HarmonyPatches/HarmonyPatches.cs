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
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {

        static HarmonyPatches()
        {
            //HarmonyInstance.DEBUG = true;
            TurretExtensions.harmonyInstance.PatchAll();

            // Gizmo_RefuelableFuelStatus delegate
            var delegateType = typeof(Gizmo_RefuelableFuelStatus).GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Instance).First();
            Patch_Gizmo_RefuelableFuelStatus.manual_GizmoOnGUI_Delegate.delegateType = delegateType;
            TurretExtensions.harmonyInstance.Patch(delegateType.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).First(), transpiler: new HarmonyMethod(typeof(Patch_Gizmo_RefuelableFuelStatus.manual_GizmoOnGUI_Delegate), "Transpiler"));

        }


    }

}

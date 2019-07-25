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
            HarmonyInstance h = HarmonyInstance.Create("XeoNovaDan.TurretExtensions");
            h.PatchAll();

            //HarmonyInstance.DEBUG = true;

            h.Patch(typeof(Gizmo_RefuelableFuelStatus).GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Instance).First().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).First(),
                transpiler: new HarmonyMethod(typeof(Patch_Gizmo_RefuelableFuelStatus.ManualPatch_GizmoOnGUI_Delegate), "Transpiler"));

        }


    }

}

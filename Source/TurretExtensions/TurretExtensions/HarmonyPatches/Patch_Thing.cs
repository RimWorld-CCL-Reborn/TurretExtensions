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

    public static class Patch_Thing
    {

        [HarmonyPatch(typeof(Thing))]
        [HarmonyPatch(nameof(Thing.Graphic), MethodType.Getter)]
        public static class Patch_Graphic_Getter
        {

            public static void Postfix(Thing __instance, ref Graphic __result)
            {
                // Replace the graphic with the upgraded graphic if applicable
                if (__instance.IsUpgradedTurret(out CompUpgradable uC) && uC.UpgradedGraphic != null)
                    __result = uC.UpgradedGraphic;
            }

        }

        [HarmonyPatch(typeof(Thing))]
        [HarmonyPatch(nameof(Thing.DrawExtraSelectionOverlays))]
        public static class Patch_DrawExtraSelectionOverlays
        {

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var instructionList = instructions.ToList();

                var specialDisplayRadiusInfo = AccessTools.Field(typeof(BuildableDef), nameof(BuildableDef.specialDisplayRadius));
                var adjustedSpecialDisplayRadiusInfo = AccessTools.Method(typeof(Patch_DrawExtraSelectionOverlays), nameof(AdjustedSpecialDisplayRadius));

                for (int i = 0; i < instructionList.Count; i++)
                {
                    var instruction = instructionList[i];

                    // Look for each time the method tries to get 'special display radius'; add a call to our helper method
                    if (instruction.opcode == OpCodes.Ldfld && instruction.operand == specialDisplayRadiusInfo)
                    {
                        yield return instruction; // this.def.specialDisplayRadius
                        yield return new CodeInstruction(OpCodes.Ldarg_0); // this
                        instruction = new CodeInstruction(OpCodes.Call, adjustedSpecialDisplayRadiusInfo); // AdjustedSpecialDisplayRadius(this.def.specialDisplayRadius, this)
                    }

                    yield return instruction;
                }
            }

            public static float AdjustedSpecialDisplayRadius(float originalRadius, Thing instance)
            {
                if (instance is Building_TurretGun turret)
                {
                    return turret.gun.def.Verbs[0].range;
                }
                return originalRadius;
            }

        }

    }

}

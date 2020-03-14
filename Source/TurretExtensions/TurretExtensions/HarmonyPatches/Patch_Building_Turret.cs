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

    public static class Patch_Building_Turret
    {

        [HarmonyPatch(typeof(Building_Turret), nameof(Building_Turret.TargetPriorityFactor), MethodType.Getter)]
        public static class get_TargetPriorityFactor
        {

            public static void Postfix(Building_Turret __instance, ref float __result)
            {
                // Set to 0 if turret is manned
                if (__instance.TryGetComp<CompMannable>() is CompMannable mannableComp && mannableComp.MannedNow)
                    __result = 0;
            }

        }

        [HarmonyPatch(typeof(Building_Turret), nameof(Building_Turret.PreApplyDamage))]
        public static class PreApplyDamage
        {

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                #if DEBUG
                    Log.Message("Transpiler start: Building_Turret.PreApplyDamage (1 match)");
                #endif

                var instructionList = instructions.ToList();

                for (int i = 0; i < instructionList.Count; i++)
                {
                    var instruction = instructionList[i];

                    var notifyDamageAppliedInfo = AccessTools.Method(typeof(StunHandler), nameof(StunHandler.Notify_DamageApplied), new Type[] { typeof(DamageInfo), typeof(bool) } );
                    var affectedByEMPInfo = AccessTools.Method(typeof(PreApplyDamage), nameof(TurretExtensionsUtility.AffectedByEMP), new Type[] { typeof(DamageInfo), typeof(bool) } );

                    // Look for the 'true' parameter that is passed to calls to Notify_DamageApplied
                    if (instruction.opcode == OpCodes.Ldc_I4_1)
                    {
                        var nextInstruction = instructionList[i + 1];
                        if (nextInstruction.opcode == OpCodes.Callvirt && nextInstruction.OperandIs(notifyDamageAppliedInfo))
                        {
                            #if DEBUG
                                Log.Message("Building_Turret.PreApplyDamage match 1 of 1");
                            #endif

                            yield return new CodeInstruction(OpCodes.Ldarg_0); // this
                            yield return instruction.Clone(); // true
                            instruction = new CodeInstruction(OpCodes.Call, affectedByEMPInfo); // AffectedByEMP(this, true)
                        }
                    }

                    yield return instruction;
                }
            }

            private static bool AffectedByEMP(Building_Turret instance, bool dummyParam)
            {
                // Only taking the original bool as a parameter so that other transpilers may hook
                return instance.AffectedByEMP();
            }

        }

    }

}

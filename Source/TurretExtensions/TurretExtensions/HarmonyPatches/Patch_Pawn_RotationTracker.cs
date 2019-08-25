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

    public static class Patch_Pawn_RotationTracker
    {

        [HarmonyPatch(typeof(Pawn_RotationTracker), nameof(Pawn_RotationTracker.UpdateRotation))]
        public static class UpdateRotation
        {

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var instructionList = instructions.ToList();
                var draftedGetterInfo = AccessTools.Property(typeof(Pawn), nameof(Pawn.Drafted)).GetGetMethod();
                var canRotateDraftedPawnInfo = AccessTools.Method(typeof(UpdateRotation), nameof(CanRotateDraftedPawn));

                for (int i = 0; i < instructionList.Count; i++)
                {
                    var instruction = instructionList[i];

                    // Look for all calls for pawn.Drafted
                    if (instruction.opcode == OpCodes.Callvirt && instruction.operand == draftedGetterInfo)
                    {
                        yield return instruction; // this.pawn.Drafted;
                        yield return new CodeInstruction(OpCodes.Ldarg_0); // this
                        yield return new CodeInstruction(OpCodes.Ldfld, instructionList[i - 1].operand); // this.pawn
                        instruction = new CodeInstruction(OpCodes.Call, canRotateDraftedPawnInfo); // CanRotateDraftedPawn(this.pawn.Drafted, this.pawn)
                    }

                    yield return instruction;
                }
            }

            public static bool CanRotateDraftedPawn(bool drafted, Pawn pawn)
            {
                if (pawn.MannedThing() != null)
                    return false;
                return drafted;
            }

        }

    }

}

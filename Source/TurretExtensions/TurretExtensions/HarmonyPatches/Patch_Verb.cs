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

    public static class Patch_Verb
    {

        [HarmonyPatch(typeof(Verb), nameof(Verb.DrawHighlight))]
        public static class DrawHighlight
        {

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var instructionList = instructions.ToList();
                var drawRadiusRingInfo = AccessTools.Method(typeof(VerbProperties), nameof(VerbProperties.DrawRadiusRing));
                var tryDrawFiringConeInfo = AccessTools.Method(typeof(DrawHighlight), nameof(DrawHighlight.TryDrawFiringCone));

                var instructionToBranchTo = instructionList[instructionList.FirstIndexOf(i => i.operand == drawRadiusRingInfo) + 1];
                var branchLabel = new Label();
                instructionToBranchTo.labels.Add(branchLabel);

                yield return new CodeInstruction(OpCodes.Ldarg_0); // this
                yield return new CodeInstruction(OpCodes.Call, tryDrawFiringConeInfo); // ShouldDrawFiringCone(this)
                yield return new CodeInstruction(OpCodes.Brtrue_S, branchLabel);

                /*
                 
                if (!ShouldDrawFiringCone(this))
                    this.verbProps.DrawRadiusRing(this.caster.Position);
                 
                */

                for (int i = 0; i < instructionList.Count; i++)
                    yield return instructionList[i];
            }

            private static bool TryDrawFiringCone(Verb instance)
            {
                if (instance.Caster is Building_Turret turret && TurretExtensionsUtility.FiringArcFor(turret) < 360)
                {
                    TurretExtensionsUtility.TryDrawFiringCone(turret, instance.verbProps.range);
                    return true;
                }
                return false;
            }

        }

    }

}

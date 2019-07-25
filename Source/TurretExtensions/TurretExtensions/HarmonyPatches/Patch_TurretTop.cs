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

    public static class Patch_TurretTop
    {

        [HarmonyPatch(typeof(TurretTop))]
        [HarmonyPatch(nameof(TurretTop.DrawTurret))]
        public static class Patch_DrawTurret
        {

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var instructionList = instructions.ToList();

                var turretTopOffsetToUse = AccessTools.Method(typeof(Patch_DrawTurret), nameof(TurretTopOffsetToUse));
                var turretTopDrawSizeToUse = AccessTools.Method(typeof(Patch_DrawTurret), nameof(TurretTopDrawSizeToUse));
                var turretTopMatToUse = AccessTools.Method(typeof(Patch_DrawTurret), nameof(TurretTopMatToUse));

                for (int i = 0; i < instructionList.Count; i++)
                {
                    var instruction = instructionList[i];

                    if (instruction.opcode == OpCodes.Ldflda && instruction.operand == AccessTools.Field(typeof(BuildingProperties), nameof(BuildingProperties.turretTopOffset)))
                    {
                        instruction.opcode = OpCodes.Ldfld;
                        yield return instruction;
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TurretTop), "parentTurret"));
                        instruction = new CodeInstruction(OpCodes.Call, turretTopOffsetToUse);

                    }

                    if (instruction.opcode == OpCodes.Ldfld)
                    {
                        if (instruction.operand == AccessTools.Field(typeof(BuildingProperties), nameof(BuildingProperties.turretTopDrawSize)))
                        {
                            yield return instruction;
                            yield return new CodeInstruction(OpCodes.Ldarg_0);
                            yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TurretTop), "parentTurret"));
                            instruction = new CodeInstruction(OpCodes.Call, turretTopDrawSizeToUse);
                        }
                        if (instruction.operand == AccessTools.Field(typeof(BuildingProperties), nameof(BuildingProperties.turretTopMat)))
                        {
                            yield return instruction;
                            yield return new CodeInstruction(OpCodes.Ldarg_0);
                            yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TurretTop), "parentTurret"));
                            instruction = new CodeInstruction(OpCodes.Call, turretTopMatToUse);
                        }
                    }

                    yield return instruction;
                }
            }

            private static Vector2 TurretTopOffsetToUse(Vector2 ttO, Building_Turret turret) =>
            (turret.IsUpgradedTurret(out CompUpgradable uC) && uC.Props.turretTopOffset != null) ? uC.Props.turretTopOffset : ttO;

            private static float TurretTopDrawSizeToUse(float tTDS, Building_Turret turret) =>
                (turret.IsUpgradedTurret(out CompUpgradable uC)) ? uC.Props.turretTopDrawSize : tTDS;

            private static Material TurretTopMatToUse(Material ttM, Building_Turret turret) =>
                (turret.IsUpgradedTurret(out CompUpgradable uC) && !uC.Props.turretTopGraphicPath.NullOrEmpty()) ?
                MaterialPool.MatFrom(uC.Props.turretTopGraphicPath) : ttM;

        }

    }

}

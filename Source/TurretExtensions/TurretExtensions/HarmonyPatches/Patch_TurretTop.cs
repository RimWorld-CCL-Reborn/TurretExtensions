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

        [HarmonyPatch(typeof(TurretTop), nameof(TurretTop.DrawTurret))]
        public static class DrawTurret
        {

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var instructionList = instructions.ToList();

                var turretTopOffsetToUse = AccessTools.Method(typeof(DrawTurret), nameof(TurretTopOffsetToUse));
                var turretTopDrawSizeToUse = AccessTools.Method(typeof(DrawTurret), nameof(TurretTopDrawSizeToUse));
                var turretTopMatToUse = AccessTools.Method(typeof(DrawTurret), nameof(TurretTopMatToUse));

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

            private static Vector2 TurretTopOffsetToUse(Vector2 original, Building_Turret turret)
            {
                if (turret.IsUpgraded(out CompUpgradable uC) && uC.Props.turretTopOffset != null)
                    return uC.Props.turretTopOffset;
                return original;
            }

            private static float TurretTopDrawSizeToUse(float original, Building_Turret turret)
            {
                if (turret.IsUpgraded(out CompUpgradable upgradableComp) && upgradableComp.Props.turretTopDrawSize != -1)
                    return upgradableComp.Props.turretTopDrawSize;
                return original;
            }

            private static Material TurretTopMatToUse(Material original, Building_Turret turret)
            {
                if (turret.IsUpgraded(out CompUpgradable upgradableComp) && !upgradableComp.Props.turretTopGraphicPath.NullOrEmpty())
                    return MaterialPool.MatFrom(upgradableComp.Props.turretTopGraphicPath);
                return original;
            }
                

        }

    }

}

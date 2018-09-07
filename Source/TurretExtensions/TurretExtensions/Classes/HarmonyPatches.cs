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
    static class HarmonyPatches
    {

        static readonly Type patchType = typeof(HarmonyPatches);

        static HarmonyPatches()
        {
            HarmonyInstance h = HarmonyInstance.Create("XeoNovaDan.TurretExtensions");

            //HarmonyInstance.DEBUG = true;

            #region CompRefuelable
            h.Patch(AccessTools.Method(typeof(Building_TurretGun), "GetInspectString"),
                transpiler: new HarmonyMethod(patchType, nameof(TranspileGetInspectString)));

            h.Patch(AccessTools.Method(typeof(CompRefuelable), nameof(CompRefuelable.Refuel), new[] { typeof(float) }),
                transpiler: new HarmonyMethod(patchType, nameof(CompRefuelable_FuelCapacityTranspiler)));

            h.Patch(AccessTools.Method(typeof(CompRefuelable), nameof(CompRefuelable.CompInspectStringExtra)),
                transpiler: new HarmonyMethod(patchType, nameof(CompRefuelable_FuelCapacityTranspiler)));

            h.Patch(AccessTools.Property(typeof(CompRefuelable), nameof(CompRefuelable.TargetFuelLevel)).GetGetMethod(),
                transpiler: new HarmonyMethod(patchType, nameof(CompRefuelable_FuelCapacityTranspiler)));

            h.Patch(AccessTools.Property(typeof(CompRefuelable), nameof(CompRefuelable.TargetFuelLevel)).GetSetMethod(),
                transpiler: new HarmonyMethod(patchType, nameof(CompRefuelable_FuelCapacityTranspiler)));

            h.Patch(AccessTools.Method(typeof(CompRefuelable), nameof(CompRefuelable.GetFuelCountToFullyRefuel)),
                transpiler: new HarmonyMethod(patchType, nameof(TranspileGetFuelCountToFullyRefuel)));
            #endregion

            h.Patch(typeof(Gizmo_RefuelableFuelStatus).GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Instance).First().
            GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).First(),
                transpiler: new HarmonyMethod(patchType, nameof(TranspileGizmoOnGUIDelegate)));

            h.Patch(AccessTools.Method(typeof(Designator_Cancel), "DesignateThing"),
                new HarmonyMethod(patchType, nameof(PrefixDesignateThing)));

            h.Patch(AccessTools.Method(typeof(Building_TurretGun), nameof(Building_TurretGun.Tick)),
                new HarmonyMethod(patchType, nameof(PrefixTick)));

            //h.Patch(AccessTools.Method(typeof(Building_TurretGun), "BeginBurst"),
            //    new HarmonyMethod(patchType, nameof(PrefixBeginBurst)));

            h.Patch(AccessTools.Method(typeof(Building_TurretGun), nameof(Building_TurretGun.SpawnSetup)),
                postfix: new HarmonyMethod(patchType, nameof(PostfixSpawnSetup)));

            h.Patch(AccessTools.Method(typeof(Building_TurretGun), "BurstCooldownTime"),
                postfix: new HarmonyMethod(patchType, nameof(PostfixBurstCooldownTime)));

            h.Patch(AccessTools.Method(typeof(Building_TurretGun), "TryStartShootSomething"),
                postfix: new HarmonyMethod(patchType, nameof(PostfixTryStartShootSomething)));

            h.Patch(AccessTools.Property(typeof(Building_TurretGun), "CanSetForcedTarget").GetGetMethod(true),
                postfix: new HarmonyMethod(patchType, nameof(PostfixCanSetForcedTarget)));

            h.Patch(AccessTools.Property(typeof(Thing), nameof(Thing.Graphic)).GetGetMethod(),
                postfix: new HarmonyMethod(patchType, nameof(PostfixGraphic)));

            h.Patch(AccessTools.Method(typeof(TurretTop), nameof(TurretTop.DrawTurret)),
                transpiler: new HarmonyMethod(patchType, nameof(TranspileDrawTurret)));

            h.Patch(AccessTools.Method(typeof(ThingDef), nameof(ThingDef.SpecialDisplayStats)),
                postfix: new HarmonyMethod(patchType, nameof(ThingDef_PostfixSpecialDisplayStats)));

            h.Patch(AccessTools.Method(typeof(StatsReportUtility), "StatsToDraw", new[] { typeof(Def), typeof(ThingDef) }),
                postfix: new HarmonyMethod(patchType, nameof(PostfixStatsToDraw_ThingDef)));

            h.Patch(AccessTools.Method(typeof(StatsReportUtility), "StatsToDraw", new[] { typeof(Thing) }),
                postfix: new HarmonyMethod(patchType, nameof(PostfixStatsToDraw_Thing)));

            h.Patch(AccessTools.Method(typeof(ReverseDesignatorDatabase), "InitDesignators"),
                postfix: new HarmonyMethod(patchType, nameof(PostfixInitDesignators)));

            h.Patch(AccessTools.Method(typeof(CompPowerTrader), nameof(CompPowerTrader.SetUpPowerVars)),
                postfix: new HarmonyMethod(patchType, nameof(PostfixSetUpPowerVars)));

            h.Patch(AccessTools.Method(typeof(StatWorker), nameof(StatWorker.GetValueUnfinalized)),
                postfix: new HarmonyMethod(patchType, nameof(PostfixGetValueUnfinalized)));

            h.Patch(AccessTools.Method(typeof(StatWorker), nameof(StatWorker.GetExplanationUnfinalized)),
                postfix: new HarmonyMethod(patchType, nameof(PostfixGetExplanationUnfinalized)));

            // Thanks erdelf!
            Log.Message(text: $"Turret Extensions successfully completed {h.GetPatchedMethods().Select(selector: mb => h.GetPatchInfo(method: mb)).SelectMany(selector: p => p.Prefixes.Concat(second: p.Postfixes).Concat(second: p.Transpilers)).Count(predicate: p => p.owner == h.Id)} patches with harmony.");

        }

        #region Shared
        private static bool IsFuelCapacityInstruction(this CodeInstruction instruction) =>
            instruction.opcode == OpCodes.Ldfld && instruction.operand == AccessTools.Field(typeof(CompProperties_Refuelable), nameof(CompProperties_Refuelable.fuelCapacity));

        // Probably my most favourite transpiler ever
        public static IEnumerable<CodeInstruction> CompRefuelable_FuelCapacityTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionList = instructions.ToList();

            MethodInfo adjustedFuelCapacity = AccessTools.Method(patchType, nameof(AdjustedFuelCapacity));

            for (int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];

                if (instruction.IsFuelCapacityInstruction())
                {
                    yield return instruction;
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(CompRefuelable), nameof(CompRefuelable.parent)));
                    instruction = new CodeInstruction(OpCodes.Call, adjustedFuelCapacity);
                }

                yield return instruction;
            }
        }

        private static float AdjustedFuelCapacity(float baseFuelCapacity, Thing t) =>
            baseFuelCapacity * ((t.IsUpgradedTurret(out CompUpgradable uC)) ? uC.Props.effectiveBarrelDurabilityFactor * uC.Props.barrelDurabilityFactor : 1f);
        #endregion

        #region TranspileGetInspectString
        public static IEnumerable<CodeInstruction> TranspileGetInspectString(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionList = instructions.ToList();

            for (int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];

                if (instruction.opcode == OpCodes.Ldc_R4 && (float)instruction.operand == 5)
                    instruction.operand = 0;

                yield return instruction;
            }
        }
        #endregion

        #region TranspileGetFuelCountToFullyRefuel
        public static IEnumerable<CodeInstruction> TranspileGetFuelCountToFullyRefuel(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionList = instructions.ToList();

            MethodInfo adjustedFuelCapacity = AccessTools.Method(patchType, nameof(AdjustedFuelCapacity));
            MethodInfo adjustedFuelCount = AccessTools.Method(patchType, nameof(AdjustedFuelCount));

            for (int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];

                if (instruction.IsFuelCapacityInstruction())
                {
                    yield return instruction;
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(CompRefuelable), nameof(CompRefuelable.parent)));
                    instruction = new CodeInstruction(OpCodes.Call, adjustedFuelCapacity);
                }

                if (instruction.opcode == OpCodes.Stloc_0)
                {
                    yield return instruction;
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(CompRefuelable), nameof(CompRefuelable.parent)));
                    yield return new CodeInstruction(OpCodes.Call, adjustedFuelCount);
                    instruction = new CodeInstruction(OpCodes.Stloc_0);
                }

                yield return instruction;
            }
        }

        private static float AdjustedFuelCount(float currentFuelCount, Thing thing) =>
            currentFuelCount / ((thing.IsUpgradedTurret(out CompUpgradable uC)) ? uC.Props.effectiveBarrelDurabilityFactor * uC.Props.barrelDurabilityFactor : 1f);
        #endregion

        #region TranspileGizmoOnGUIDelegate
        // It was all erdelf, I swear!
        public static IEnumerable<CodeInstruction> TranspileGizmoOnGUIDelegate(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionList = instructions.ToList();

            bool ldflda = false;
            MethodInfo adjustedFuelCapacity = AccessTools.Method(patchType, nameof(AdjustedFuelCapacity));

            for (int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];

                if (instruction.opcode == OpCodes.Ldflda && instruction.operand == AccessTools.Field(typeof(CompProperties_Refuelable), nameof(CompProperties_Refuelable.fuelCapacity)))
                {
                    instruction.opcode = OpCodes.Ldfld;
                    ldflda = true;
                }

                if (instruction.IsFuelCapacityInstruction())
                {
                    yield return instruction;
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(instructionList[i - 3]);
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Gizmo_RefuelableFuelStatus), nameof(Gizmo_RefuelableFuelStatus.refuelable)));
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(CompRefuelable), nameof(CompRefuelable.parent)));
                    if (ldflda)
                    {
                        yield return new CodeInstruction(OpCodes.Call, adjustedFuelCapacity);
                        yield return new CodeInstruction(OpCodes.Stloc_S, 7);
                    }
                    instruction = (ldflda) ? new CodeInstruction(OpCodes.Ldloca_S, 7) : new CodeInstruction(OpCodes.Call, adjustedFuelCapacity);
                    ldflda = false;
                }

                yield return instruction;
            }
        }
        #endregion  

        #region PrefixDesignateThing
        public static void PrefixDesignateThing(Thing t)
        {
            if (t.TryGetComp<CompUpgradable>() is CompUpgradable upgradableComp)
            {
                var upgradeDes = t.Map.designationManager.DesignationOn(t, TE_DesignationDefOf.UpgradeTurret);
                if (upgradeDes != null)
                {
                    upgradableComp.upgradeWorkTotal = -1f;
                    upgradableComp.innerContainer.TryDropAll(t.Position, t.Map, ThingPlaceMode.Near);
                }
            }
        }
        #endregion

        #region PostfixPostMake
        public static void PostfixSpawnSetup(Building_TurretGun __instance, TurretTop ___top)
        {
            switch ((__instance.def.GetModExtension<TurretFrameworkExtension>() ?? TurretFrameworkExtension.defaultValues).gunFaceDirectionOnSpawn)
            {
                case TurretGunFaceDirection.North:
                    Traverse.Create(___top).Property("CurRotation").SetValue(Rot4.North.AsAngle);
                    break;
                case TurretGunFaceDirection.East:
                    Traverse.Create(___top).Property("CurRotation").SetValue(Rot4.East.AsAngle);
                    break;
                case TurretGunFaceDirection.South:
                    Traverse.Create(___top).Property("CurRotation").SetValue(Rot4.South.AsAngle);
                    break;
                case TurretGunFaceDirection.West:
                    Traverse.Create(___top).Property("CurRotation").SetValue(Rot4.West.AsAngle);
                    break;
                default:
                    Traverse.Create(___top).Property("CurRotation").SetValue(__instance.Rotation.AsAngle);
                    break;
            }
        }
        #endregion

        #region PrefixTick
        public static void PrefixTick(Building_TurretGun __instance, LocalTargetInfo ___forcedTarget)
        {
            CompSmartForcedTarget comp = __instance.TryGetComp<CompSmartForcedTarget>();
            bool? upgraded = __instance.TryGetComp<CompUpgradable>()?.upgraded;
            if (comp != null && ___forcedTarget.Thing is Pawn pawn)
            {
                if (!pawn.Downed && (upgraded == true || !comp.Props.onlyApplyWhenUpgraded) && !comp.attackingNonDownedPawn)
                    comp.attackingNonDownedPawn = true;
                if (pawn.Downed && comp.attackingNonDownedPawn)
                {
                    comp.attackingNonDownedPawn = false;
                    AccessTools.Method(typeof(Building_TurretGun), "ResetForcedTarget").Invoke(__instance, null);
                }
            }
        }
        #endregion

        #region PostfixBurstCooldownTime
        public static void PostfixBurstCooldownTime(Building_TurretGun __instance, ref float __result)
        {
            if (__instance.IsUpgradedTurret(out CompUpgradable upgradableComp))
            {
                __result *= upgradableComp.Props.turretBurstCooldownTimeFactor;
            }
        }
        #endregion

        #region PostfixTryStartShootSomething
        public static void PostfixTryStartShootSomething(Building_TurretGun __instance, ref int ___burstWarmupTicksLeft)
        {
            var extensionValues = __instance.def.GetModExtension<TurretFrameworkExtension>() ?? TurretFrameworkExtension.defaultValues;
            string turretDefName = __instance.def.defName;

            if (extensionValues.useMannerAimingDelayFactor)
            {
                CompMannable mannableComp = __instance.TryGetComp<CompMannable>();
                if (mannableComp != null)
                {
                    Pawn manner = mannableComp.ManningPawn;
                    if (manner != null)
                    {
                        float mannerAimingDelayFactor = manner.GetStatValue(StatDefOf.AimingDelayFactor);
                        ___burstWarmupTicksLeft = (int)Math.Round(___burstWarmupTicksLeft * mannerAimingDelayFactor);
                    }
                }
                else
                    Log.Warning(String.Format("Turret (defName={0}) has useMannerAimingDelayFactor set to true but doesn't have CompMannable.", turretDefName));
            }
            if (__instance.IsUpgradedTurret(out CompUpgradable upgradableComp))
                ___burstWarmupTicksLeft = (int)Math.Round(___burstWarmupTicksLeft * upgradableComp.Props.turretBurstWarmupTimeFactor);
        }
        #endregion

        #region PostfixCanSetForcedTarget
        public static void PostfixCanSetForcedTarget(Building_TurretGun __instance, ref bool __result)
        {
            var extensionValues = __instance.def.GetModExtension<TurretFrameworkExtension>() ?? TurretFrameworkExtension.defaultValues;
            string turretDefName = __instance.def.defName;
            var upgradableComp = __instance.TryGetComp<CompUpgradable>();

            if (extensionValues.canForceAttack || upgradableComp != null && upgradableComp.upgraded && upgradableComp.Props.canForceAttack)
            {
                CompMannable mannableComp = __instance.TryGetComp<CompMannable>();
                if (mannableComp == null && __instance.Faction == Faction.OfPlayer)
                    __result = true;
                else
                    Log.Warning(String.Format("Turret (defName={0}) has canForceAttack set to true and CompMannable; canForceAttack is redundant in this case.", turretDefName));
            }
        }
        #endregion

        #region PostfixGraphic
        public static void PostfixGraphic(Thing __instance, ref Graphic __result)
        {
            if (__instance.IsUpgradedTurret(out CompUpgradable uC) && uC.UpgradedGraphic != null)
                __result = uC.UpgradedGraphic;
        }
        #endregion

        #region TranspileDrawTurret
        public static IEnumerable<CodeInstruction> TranspileDrawTurret(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionList = instructions.ToList();

            MethodInfo turretTopOffsetToUse = AccessTools.Method(patchType, nameof(TurretTopOffsetToUse));
            MethodInfo turretTopDrawSizeToUse = AccessTools.Method(patchType, nameof(TurretTopDrawSizeToUse));
            MethodInfo turretTopMatToUse = AccessTools.Method(patchType, nameof(TurretTopMatToUse));

            for (int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];

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
        #endregion

        #region ThingDef_PostfixSpecialDisplayStats
        public static void ThingDef_PostfixSpecialDisplayStats(ThingDef __instance, ref IEnumerable<StatDrawEntry> __result)
        {
            if (__instance.IsShell)
            {
                Thing shellThing = ThingMaker.MakeThing(__instance.projectileWhenLoaded);
                ProjectileProperties shellProps = __instance.projectileWhenLoaded.projectile;
                int shellDamage = shellProps.GetDamageAmount(shellThing);
                float shellArmorPenetration = shellProps.GetArmorPenetration(shellThing);
                float shellStoppingPower = shellProps.StoppingPower;
                string shellDamageDef = shellProps.damageDef.label.CapitalizeFirst();
                float shellExplosionRadius = shellProps.explosionRadius;

                __result = __result.Add(new StatDrawEntry(TE_StatCategoryDefOf.TurretAmmo, "Damage".Translate(), shellDamage.ToString(), 20));
                __result = __result.Add(new StatDrawEntry(TE_StatCategoryDefOf.TurretAmmo, "ShellDamageType".Translate(), shellDamageDef, 19));
                __result = __result.Add(new StatDrawEntry(TE_StatCategoryDefOf.TurretAmmo, "ArmorPenetration".Translate(), shellArmorPenetration.ToStringPercent(), 18, "ArmorPenetrationExplanation".Translate()));
                __result = __result.Add(new StatDrawEntry(TE_StatCategoryDefOf.TurretAmmo, "StoppingPower".Translate(), shellStoppingPower.ToString(), 17, "StoppingPowerExplanation".Translate()));

                if (shellExplosionRadius > 0f)
                    __result = __result.Add(new StatDrawEntry(TE_StatCategoryDefOf.TurretAmmo, "ShellExplosionRadius".Translate(), shellExplosionRadius.ToString(), 16));
            }
        }
        #endregion

        // This lazy copypasta and adaptation job from PostfixStatsToDraw_Thing will probably need to be tidied up... soon™
        // Not tidying it up for v1.2 though; probably a v2.0 thing
        #region PostfixStatsToDraw_ThingDef
        public static void PostfixStatsToDraw_ThingDef(Def def, ThingDef stuff, ref IEnumerable<StatDrawEntry> __result)
        {
            if (def is ThingDef tdef && tdef.building?.IsTurret == true)
            {
                var extensionValues = def.GetModExtension<TurretFrameworkExtension>() ?? TurretFrameworkExtension.defaultValues;

                // Upgradability
                bool upgradable = tdef.IsUpgradableTurret(out CompProperties_Upgradable uCP);
                string turretUpgradableString = ((upgradable) ? "YesClickForDetails" : "No").Translate();
                StatDrawEntry upgradabilitySDE = new StatDrawEntry(TE_StatCategoryDefOf.Turret, "Upgradable".Translate(), turretUpgradableString, 50, GetTurretUpgradeBenefits(tdef, stuff, uCP, extensionValues));
                __result = __result.Add(upgradabilitySDE);

                // Building stats
                float turretBurstWarmupTime = tdef.building.turretBurstWarmupTime;
                float turretBurstCooldownTime = tdef.building.turretBurstCooldownTime;
                bool turretUsesMannerShootingAccuracy = extensionValues.useMannerShootingAccuracy;

                // Turret gun stats
                ThingDef turretGunDef = tdef.building.turretGunDef;
                Thing turretGunThing = ThingMaker.MakeThing(turretGunDef);
                float turretGunAccuracyTouch = turretGunDef.statBases.GetStatValueFromList(StatDefOf.AccuracyTouch, StatDefOf.AccuracyTouch.defaultBaseValue);
                float turretGunAccuracyShort = turretGunDef.statBases.GetStatValueFromList(StatDefOf.AccuracyShort, StatDefOf.AccuracyShort.defaultBaseValue);
                float turretGunAccuracyMedium = turretGunDef.statBases.GetStatValueFromList(StatDefOf.AccuracyMedium, StatDefOf.AccuracyMedium.defaultBaseValue);
                float turretGunAccuracyLong = turretGunDef.statBases.GetStatValueFromList(StatDefOf.AccuracyLong, StatDefOf.AccuracyLong.defaultBaseValue);

                // Verb stats
                VerbProperties turretGunVerbProps = turretGunDef.Verbs[0];
                float turretGunMissRadius = turretGunVerbProps.forcedMissRadius;
                float turretGunMinRange = turretGunVerbProps.minRange;
                float turretGunRange = turretGunVerbProps.range;
                int turretBurstShotCount = turretGunVerbProps.burstShotCount;
                float turretBurstShotFireRate = 60f / turretGunVerbProps.ticksBetweenBurstShots.TicksToSeconds();

                if (turretBurstWarmupTime > 0)
                    __result = __result.Add(new StatDrawEntry(TE_StatCategoryDefOf.Turret, "WarmupTime".Translate(), turretBurstWarmupTime.ToString("0.##") + " s", 40));
                if (turretBurstCooldownTime > 0)
                    __result = __result.Add(new StatDrawEntry(TE_StatCategoryDefOf.Turret, "CooldownTime".Translate(), turretBurstCooldownTime.ToString("0.##") + " s", 40));
                if (turretGunMinRange > 0f)
                    __result = __result.Add(new StatDrawEntry(TE_StatCategoryDefOf.Turret, "MinimumRange".Translate(), turretGunMinRange.ToString("0"), 10));
                __result = __result.Add(new StatDrawEntry(TE_StatCategoryDefOf.Turret, "Range".Translate(), turretGunRange.ToString("0"), 9));

                if (turretGunMissRadius == 0f)
                {
                    // Whether or not the turret uses the manning pawn's shooting accuracy
                    if (tdef.HasComp(typeof(CompMannable)))
                    {
                        string shootingAccExplanation = "MannedTurretUsesShooterAccuracyExplanation".Translate();
                        if (turretUsesMannerShootingAccuracy)
                        {
                            shootingAccExplanation += "\n\n";
                            if (extensionValues.mannerShootingAccuracyOffset != 0f)
                                shootingAccExplanation += ((extensionValues.mannerShootingAccuracyOffset > 0f) ? "MannedTurretUsesShooterAccuracyBonus".Translate(new object[] { extensionValues.mannerShootingAccuracyOffset.ToString("0.#"), def.label }) : "MannedTurretUsesShooterAccuracyPenalty".Translate(new object[] { extensionValues.mannerShootingAccuracyOffset.ToString("0.#"), def.label }));
                            else
                                shootingAccExplanation += "MannedTurretUsesShooterAccuracyNoChange".Translate(def.label);
                        }
                        __result = __result.Add(new StatDrawEntry(TE_StatCategoryDefOf.Turret, "MannedTurretUsesShooterAccuracy".Translate(),
                            (extensionValues.useMannerShootingAccuracy) ? "Yes".Translate() : "No".Translate(), 15, shootingAccExplanation));
                    }

                    // Accuracy for weapon
                    if (turretGunAccuracyTouch != StatDefOf.AccuracyTouch.defaultBaseValue || turretGunAccuracyShort != StatDefOf.AccuracyShort.defaultBaseValue ||
                    turretGunAccuracyMedium != StatDefOf.AccuracyMedium.defaultBaseValue || turretGunAccuracyLong != StatDefOf.AccuracyLong.defaultBaseValue)
                    {
                        __result = __result.Add(new StatDrawEntry(TE_StatCategoryDefOf.Turret, StatDefOf.AccuracyTouch.label, turretGunAccuracyTouch.ToStringPercent(), 14, StatDefOf.AccuracyTouch.description));
                        __result = __result.Add(new StatDrawEntry(TE_StatCategoryDefOf.Turret, StatDefOf.AccuracyShort.label, turretGunAccuracyShort.ToStringPercent(), 13, StatDefOf.AccuracyShort.description));
                        __result = __result.Add(new StatDrawEntry(TE_StatCategoryDefOf.Turret, StatDefOf.AccuracyMedium.label, turretGunAccuracyMedium.ToStringPercent(), 12, StatDefOf.AccuracyMedium.description));
                        __result = __result.Add(new StatDrawEntry(TE_StatCategoryDefOf.Turret, StatDefOf.AccuracyLong.label, turretGunAccuracyLong.ToStringPercent(), 11, StatDefOf.AccuracyLong.description));
                    }
                }
                else if (turretGunMissRadius > 0f)
                {
                    float turretGunDirectHitChance = (1f / GenRadial.NumCellsInRadius(turretGunMissRadius));
                    __result = __result.Add(new StatDrawEntry(TE_StatCategoryDefOf.Turret, "MissRadius".Translate(), turretGunMissRadius.ToString("0.#"), 14));
                    __result = __result.Add(new StatDrawEntry(TE_StatCategoryDefOf.Turret, "DirectHitChance".Translate(), turretGunDirectHitChance.ToStringPercent(), 13));
                }

                if (turretBurstShotCount > 1)
                {
                    __result = __result.Add(new StatDrawEntry(TE_StatCategoryDefOf.Turret, "BurstShotFireRate".Translate(), turretBurstShotFireRate.ToString("0.##") + " rpm", 19));
                    __result = __result.Add(new StatDrawEntry(TE_StatCategoryDefOf.Turret, "BurstShotCount".Translate(), turretBurstShotCount.ToString(), 20));
                }

                // Projectile stats
                ThingDef turretGunProjectile = turretGunVerbProps.defaultProjectile;
                string damage = (turretGunProjectile != null) ? turretGunProjectile.projectile.GetDamageAmount(turretGunThing).ToString() : "MortarShellDependent".Translate();
                string armorPenetration = (turretGunProjectile != null) ? turretGunProjectile.projectile.GetArmorPenetration(turretGunThing).ToStringPercent() : "MortarShellDependent".Translate();
                string stoppingPower = (turretGunProjectile != null) ? turretGunProjectile.projectile.StoppingPower.ToString() : "MortarShellDependent".Translate();

                __result = __result.Add(new StatDrawEntry(TE_StatCategoryDefOf.Turret, "Damage".Translate(), damage, 23));
                __result = __result.Add(new StatDrawEntry(TE_StatCategoryDefOf.Turret, "ArmorPenetration".Translate(), armorPenetration, 22, "ArmorPenetrationExplanation".Translate()));

                if (stoppingPower == "MortarShellDependent".Translate() || float.Parse(stoppingPower, CultureInfo.InvariantCulture.NumberFormat) != 0f)
                    __result = __result.Add(new StatDrawEntry(TE_StatCategoryDefOf.Turret, "StoppingPower".Translate(), stoppingPower, 21, "StoppingPowerExplanation".Translate()));

                turretGunThing.Destroy();

            }
        }

        private static string GetTurretUpgradeBenefits(ThingDef def, ThingDef stuff, CompProperties_Upgradable upgradeProps, TurretFrameworkExtension extensionValues)
        {
            StringBuilder upgradabilityExplanation = new StringBuilder();
            upgradabilityExplanation.AppendLine("TurretUpgradeBenefitsMain".Translate());
            upgradabilityExplanation.AppendLine();
            if (upgradeProps != null)
            {
                var defaultValues = CompProperties_Upgradable.defaultValues;
                var upgradedGunDef = upgradeProps.turretGunDef;
                var upgradedGunVerb = upgradedGunDef?.Verbs[0];
                var upgradedGunProjectile = upgradedGunVerb?.defaultProjectile;

                upgradabilityExplanation.AppendLine("Description".Translate() + ": " + upgradeProps.description);
                upgradabilityExplanation.AppendLine();

                if ((def.MadeFromStuff && upgradeProps.costStuffCount > 0) || upgradeProps.costList != null)
                {
                    List<string> itemReqs = new List<string>();

                    if (def.MadeFromStuff && upgradeProps.costStuffCount > 0)
                        itemReqs.Add(upgradeProps.costStuffCount.ToString() + "x " + "StatsReport_Material".Translate().UncapitalizeFirst());

                    if (upgradeProps.costList != null)
                        foreach (ThingDefCountClass item in upgradeProps.costList)
                            itemReqs.Add(item.count.ToString() + "x " + item.thingDef.label);

                    upgradabilityExplanation.AppendLine("TurretResourceRequirements".Translate() + ": " + GenText.ToCommaList(itemReqs).CapitalizeFirst());
                    upgradabilityExplanation.AppendLine();
                }

                if (upgradeProps.constructionSkillPrerequisite > 0)
                {
                    upgradabilityExplanation.AppendLine("TurretMinimumUpgradeSkill".Translate() + ": " + upgradeProps.constructionSkillPrerequisite.ToString());
                    upgradabilityExplanation.AppendLine();
                }

                if (upgradeProps.researchPrerequisites is List<ResearchProjectDef> researchRequirements)
                {
                    List<string> researchReqs = new List<string>();
                    foreach (ResearchProjectDef research in researchRequirements)
                        researchReqs.Add(research.label);
                    upgradabilityExplanation.AppendLine("TurretResearchRequirements".Translate() + ": " + GenText.ToCommaList(researchReqs).CapitalizeFirst());
                    upgradabilityExplanation.AppendLine();
                }

                upgradabilityExplanation.AppendLine("TurretUpgradeBenefitsUpgradable".Translate() + ":");
                upgradabilityExplanation.AppendLine();

                // General Stats
                if (!upgradeProps.statOffsets.NullOrEmpty() || !upgradeProps.statFactors.NullOrEmpty())
                {
                    List<StatDef> modifiedStats = GetUpgradeModifiedStats(def, upgradeProps.statOffsets, upgradeProps.statFactors);
                    modifiedStats.SortBy(s => s.LabelCap);
                    foreach (StatDef stat in modifiedStats)
                    {
                        float value = def.GetStatValueAbstract(stat, stuff);
                        if (upgradeProps.statOffsets?.StatListContains(stat) == true)
                            value += upgradeProps.statOffsets.GetStatOffsetFromList(stat);
                        if (upgradeProps.statFactors?.StatListContains(stat) == true)
                            value *= upgradeProps.statFactors.GetStatFactorFromList(stat);
                        upgradabilityExplanation.AppendLine(stat.LabelCap + ": " + value.ToStringByStyle(stat.toStringStyle));
                    }
                }

                // Max Hit Points -- LEGACY
                if (def.useHitPoints && upgradeProps.MaxHitPointsFactor != 1f)
                {
                    float maxHealthFactor = upgradeProps.MaxHitPointsFactor;
                    upgradabilityExplanation.AppendLine(StatDefOf.MaxHitPoints.label.CapitalizeFirst() + ": x" + maxHealthFactor.ToStringPercent());
                }

                // Flammability -- LEGACY
                if (def.GetStatValueAbstract(StatDefOf.Flammability) > 0f && upgradeProps.FlammabilityFactor != 1f)
                {
                    float flammabilityFactor = upgradeProps.FlammabilityFactor;
                    upgradabilityExplanation.AppendLine(StatDefOf.Flammability.label.CapitalizeFirst() + ": x" + flammabilityFactor.ToStringPercent());
                }

                // Power Consumption
                if (def.HasComp(typeof(CompPowerTrader)) && upgradeProps.basePowerConsumptionFactor != 1f)
                {
                    float newPowerConsumption = def.GetCompProperties<CompProperties_Power>().basePowerConsumption * upgradeProps.basePowerConsumptionFactor;
                    upgradabilityExplanation.AppendLine("PowerNeeded".Translate() + ": " + newPowerConsumption.ToString("#####0") + " W");
                }

                // Effective barrel durability -- LEGACY
                if ((upgradeProps.effectiveBarrelDurabilityFactor != defaultValues.effectiveBarrelDurabilityFactor || upgradeProps.barrelDurabilityFactor != defaultValues.barrelDurabilityFactor)
                    && def.HasComp(typeof(CompRefuelable)))
                {
                    float effDurability = Mathf.Ceil(def.GetCompProperties<CompProperties_Refuelable>().fuelCapacity *
                        upgradeProps.barrelDurabilityFactor * upgradeProps.effectiveBarrelDurabilityFactor);
                    upgradabilityExplanation.AppendLine(def.GetCompProperties<CompProperties_Refuelable>().fuelGizmoLabel.CapitalizeFirst() + ": " + effDurability.ToString());
                }

                // Cooldown time
                if (def.building.turretBurstCooldownTime > -1f && upgradeProps.turretBurstCooldownTimeFactor != defaultValues.turretBurstCooldownTimeFactor)
                {
                    float newCooldown = def.building.turretBurstCooldownTime * upgradeProps.turretBurstCooldownTimeFactor;
                    upgradabilityExplanation.AppendLine("CooldownTime".Translate() + ": " + newCooldown.ToString("F2") + " s");
                }

                // Warmup time
                if (def.building.turretBurstWarmupTime > 0f && upgradeProps.turretBurstWarmupTimeFactor != defaultValues.turretBurstWarmupTimeFactor)
                {
                    float newWarmup = def.building.turretBurstWarmupTime * upgradeProps.turretBurstWarmupTimeFactor;
                    upgradabilityExplanation.AppendLine("WarmupTime".Translate() + ": " + Math.Round(newWarmup, 2).ToString() + " s");
                }

                // Damage, AP, Stopping Power, Burst Count and Burst Ticks
                if (upgradedGunDef != null)
                {
                    Thing upgradedGunThing = ThingMaker.MakeThing(upgradedGunDef);
                    string newDamage = (upgradedGunProjectile != null) ? upgradedGunProjectile.projectile.GetDamageAmount(upgradedGunThing).ToString() : "MortarShellDependent".Translate();
                    string newArmorPenetration = (upgradedGunProjectile != null) ? upgradedGunProjectile.projectile.GetArmorPenetration(upgradedGunThing).ToStringPercent() : "MortarShellDependent".Translate();
                    string newStoppingPower = (upgradedGunProjectile != null) ? upgradedGunProjectile.projectile.StoppingPower.ToString() : "MortarShellDependent".Translate();
                    upgradabilityExplanation.AppendLine("Damage".Translate() + ": " + newDamage);
                    upgradabilityExplanation.AppendLine("ArmorPenetration".Translate() + ": " + newArmorPenetration);
                    if (newStoppingPower == "MortarShellDependent".Translate() || float.Parse(newStoppingPower, CultureInfo.InvariantCulture.NumberFormat) != 0f)
                        upgradabilityExplanation.AppendLine("StoppingPower".Translate() + ": " + newStoppingPower);

                    int newBurstCount = upgradedGunVerb.burstShotCount;
                    if (newBurstCount > 1)
                    {
                        float newBurstFireRate = 60f / upgradedGunVerb.ticksBetweenBurstShots.TicksToSeconds();
                        upgradabilityExplanation.AppendLine("BurstShotCount".Translate() + ": " + newBurstCount.ToString());
                        upgradabilityExplanation.AppendLine("BurstShotFireRate".Translate() + ": " + newBurstFireRate.ToString("0.##") + " rpm");
                    }
                    upgradedGunThing.Destroy();
                }

                // Shooting accuracy  -- LEGACY
                if (extensionValues.useMannerShootingAccuracy && upgradeProps.mannerShootingAccuracyOffsetOffset != defaultValues.mannerShootingAccuracyOffsetOffset
                    && def.HasComp(typeof(CompMannable)))
                {
                    float newShootAcc = extensionValues.mannerShootingAccuracyOffset + upgradeProps.mannerShootingAccuracyOffsetOffset;
                    if (newShootAcc >= 0f) upgradabilityExplanation.AppendLine("UserShootingAccuracy".Translate() + ": +" + newShootAcc.ToString("0.#"));
                    else upgradabilityExplanation.AppendLine("UserShootingAccuracy".Translate() + ": " + newShootAcc.ToString("0.#"));
                }
                else if (upgradeProps.ShootingAccuracyTurretOffset != defaultValues.ShootingAccuracyTurretOffset)
                {
                    float newShootAcc = Mathf.Clamp01(def.GetStatValueAbstract(StatDefOf.ShootingAccuracyTurret) + upgradeProps.ShootingAccuracyTurretOffset);
                    upgradabilityExplanation.AppendLine(StatDefOf.ShootingAccuracyTurret.label.CapitalizeFirst() + ": " + newShootAcc.ToStringPercent("F2"));
                }

                // Accuracy (touch, short, medium and long) and Range
                if (upgradedGunDef != null)
                {
                    float newMissRadius = upgradedGunVerb.forcedMissRadius;
                    if (newMissRadius > 0f)
                    {
                        float newDirHitChance = (1f / GenRadial.NumCellsInRadius(newMissRadius));
                        upgradabilityExplanation.AppendLine("MissRadius".Translate() + ": " + newMissRadius.ToString("0.#"));
                        upgradabilityExplanation.AppendLine("DirectHitChance".Translate() + ": " + newDirHitChance.ToStringPercent());
                    }
                    else
                    {
                        float newTouchAcc = upgradedGunDef.statBases.GetStatValueFromList(StatDefOf.AccuracyTouch, StatDefOf.AccuracyTouch.defaultBaseValue);
                        float newShortAcc = upgradedGunDef.statBases.GetStatValueFromList(StatDefOf.AccuracyShort, StatDefOf.AccuracyShort.defaultBaseValue);
                        float newMediumAcc = upgradedGunDef.statBases.GetStatValueFromList(StatDefOf.AccuracyMedium, StatDefOf.AccuracyMedium.defaultBaseValue);
                        float newLongAcc = upgradedGunDef.statBases.GetStatValueFromList(StatDefOf.AccuracyLong, StatDefOf.AccuracyLong.defaultBaseValue);
                        upgradabilityExplanation.AppendLine(StatDefOf.AccuracyTouch.LabelCap + ": " + newTouchAcc.ToStringPercent());
                        upgradabilityExplanation.AppendLine(StatDefOf.AccuracyShort.LabelCap + ": " + newShortAcc.ToStringPercent());
                        upgradabilityExplanation.AppendLine(StatDefOf.AccuracyMedium.LabelCap + ": " + newMediumAcc.ToStringPercent());
                        upgradabilityExplanation.AppendLine(StatDefOf.AccuracyLong.LabelCap + ": " + newLongAcc.ToStringPercent());
                    }

                    float newMinRange = upgradedGunVerb.minRange;
                    float newRange = upgradedGunVerb.range;
                    if (newMinRange > 0f)
                        upgradabilityExplanation.AppendLine("MinimumRange".Translate() + ": " + newMinRange.ToString("0"));
                    upgradabilityExplanation.AppendLine("Range".Translate() + ": " + newRange.ToString("0"));
                }

                // Manual aiming
                if (!def.HasComp(typeof(CompMannable)) && !extensionValues.canForceAttack && upgradeProps.canForceAttack)
                    upgradabilityExplanation.AppendLine("TurretManuallyAimable".Translate());

            }

            else
                upgradabilityExplanation.AppendLine("TurretUpgradeBenefitsNotUpgradable".Translate());

            return upgradabilityExplanation.ToString();
        }

        private static List<StatDef> GetUpgradeModifiedStats(ThingDef turret, List<StatModifier> statOffsets, List<StatModifier> statFactors)
        {
            List<StatDef> resultList = new List<StatDef>();
            if (!statOffsets.NullOrEmpty())
                foreach (StatModifier offset in statOffsets)
                    resultList.Add(offset.stat);
            if (!statFactors.NullOrEmpty())
                foreach (StatModifier factor in statFactors)
                    if (!resultList.Contains(factor.stat))
                        resultList.Add(factor.stat);
            return resultList;
        }
        #endregion

        #region PostfixStatsToDraw_Thing
        public static void PostfixStatsToDraw_Thing(Thing thing, ref IEnumerable<StatDrawEntry> __result)
        {
            if (thing.def.building?.IsTurret == true)
            {
                ThingDef def = thing.def;
                var extensionValues = def.GetModExtension<TurretFrameworkExtension>() ?? TurretFrameworkExtension.defaultValues;

                // Upgradability
                bool upgradable = thing.IsUpgradableTurret(out CompUpgradable uC);
                bool upgraded = upgradable && uC.upgraded;
                string turretUpgradableString = ((upgradable) ? ((uC.upgraded) ? "NoAlreadyUpgraded" : "YesClickForDetails") : "No").Translate();
                StatDrawEntry upgradabilitySDE = new StatDrawEntry(TE_StatCategoryDefOf.Turret, "Upgradable".Translate(), turretUpgradableString, 50, GetTurretUpgradeBenefits(thing, uC, extensionValues));
                __result = __result.Add(upgradabilitySDE);

                // Building stats
                float turretBurstWarmupTime = def.building.turretBurstWarmupTime * ((upgraded) ? uC.Props.turretBurstWarmupTimeFactor : 1f);
                float turretBurstCooldownTime = def.building.turretBurstCooldownTime * ((upgraded) ? uC.Props.turretBurstCooldownTimeFactor : 1f);
                bool turretUsesMannerShootingAccuracy = extensionValues.useMannerShootingAccuracy;

                // Turret gun stats
                ThingDef turretGunDef = (upgraded && uC.Props.turretGunDef != null) ? uC.Props.turretGunDef : def.building.turretGunDef;
                Thing turretGunThing = ThingMaker.MakeThing(turretGunDef);
                float turretGunAccuracyTouch = turretGunDef.statBases.GetStatValueFromList(StatDefOf.AccuracyTouch, StatDefOf.AccuracyTouch.defaultBaseValue);
                float turretGunAccuracyShort = turretGunDef.statBases.GetStatValueFromList(StatDefOf.AccuracyShort, StatDefOf.AccuracyShort.defaultBaseValue);
                float turretGunAccuracyMedium = turretGunDef.statBases.GetStatValueFromList(StatDefOf.AccuracyMedium, StatDefOf.AccuracyMedium.defaultBaseValue);
                float turretGunAccuracyLong = turretGunDef.statBases.GetStatValueFromList(StatDefOf.AccuracyLong, StatDefOf.AccuracyLong.defaultBaseValue);

                // Verb stats
                VerbProperties turretGunVerbProps = turretGunDef.Verbs[0];
                float turretGunMissRadius = turretGunVerbProps.forcedMissRadius;
                float turretGunMinRange = turretGunVerbProps.minRange;
                float turretGunRange = turretGunVerbProps.range;
                int turretBurstShotCount = turretGunVerbProps.burstShotCount;
                float turretBurstShotFireRate = 60f / turretGunVerbProps.ticksBetweenBurstShots.TicksToSeconds();

                if (turretBurstWarmupTime > 0)
                    __result = __result.Add(new StatDrawEntry(TE_StatCategoryDefOf.Turret, "WarmupTime".Translate(), turretBurstWarmupTime.ToString("0.##") + " s", 40));
                if (turretBurstCooldownTime > 0)
                    __result = __result.Add(new StatDrawEntry(TE_StatCategoryDefOf.Turret, "CooldownTime".Translate(), turretBurstCooldownTime.ToString("0.##") + " s", 40));
                if (turretGunMinRange > 0f)
                    __result = __result.Add(new StatDrawEntry(TE_StatCategoryDefOf.Turret, "MinimumRange".Translate(), turretGunMinRange.ToString("0"), 10));
                __result = __result.Add(new StatDrawEntry(TE_StatCategoryDefOf.Turret, "Range".Translate(), turretGunRange.ToString("0"), 9));

                if (turretGunMissRadius == 0f)
                {
                    // Whether or not the turret uses the manning pawn's shooting accuracy
                    if (def.HasComp(typeof(CompMannable)))
                    {
                        string shootingAccExplanation = "MannedTurretUsesShooterAccuracyExplanation".Translate();
                        if (turretUsesMannerShootingAccuracy)
                        {
                            shootingAccExplanation += "\n\n";
                            if (extensionValues.mannerShootingAccuracyOffset != 0f)
                                shootingAccExplanation += ((extensionValues.mannerShootingAccuracyOffset > 0f) ? "MannedTurretUsesShooterAccuracyBonus".Translate(new object[] { extensionValues.mannerShootingAccuracyOffset.ToString("0.#"), def.label }) : "MannedTurretUsesShooterAccuracyPenalty".Translate(new object[] { extensionValues.mannerShootingAccuracyOffset.ToString("0.#"), def.label }));
                            else
                                shootingAccExplanation += "MannedTurretUsesShooterAccuracyNoChange".Translate(def.label);
                        }
                        __result = __result.Add(new StatDrawEntry(TE_StatCategoryDefOf.Turret, "MannedTurretUsesShooterAccuracy".Translate(),
                            (extensionValues.useMannerShootingAccuracy) ? "Yes".Translate() : "No".Translate(), 15, shootingAccExplanation));
                    }

                    // Accuracy for weapon
                    if (turretGunAccuracyTouch != StatDefOf.AccuracyTouch.defaultBaseValue || turretGunAccuracyShort != StatDefOf.AccuracyShort.defaultBaseValue ||
                    turretGunAccuracyMedium != StatDefOf.AccuracyMedium.defaultBaseValue || turretGunAccuracyLong != StatDefOf.AccuracyLong.defaultBaseValue)
                    {
                        __result = __result.Add(new StatDrawEntry(TE_StatCategoryDefOf.Turret, StatDefOf.AccuracyTouch.label, turretGunAccuracyTouch.ToStringPercent(), 14, StatDefOf.AccuracyTouch.description));
                        __result = __result.Add(new StatDrawEntry(TE_StatCategoryDefOf.Turret, StatDefOf.AccuracyShort.label, turretGunAccuracyShort.ToStringPercent(), 13, StatDefOf.AccuracyShort.description));
                        __result = __result.Add(new StatDrawEntry(TE_StatCategoryDefOf.Turret, StatDefOf.AccuracyMedium.label, turretGunAccuracyMedium.ToStringPercent(), 12, StatDefOf.AccuracyMedium.description));
                        __result = __result.Add(new StatDrawEntry(TE_StatCategoryDefOf.Turret, StatDefOf.AccuracyLong.label, turretGunAccuracyLong.ToStringPercent(), 11, StatDefOf.AccuracyLong.description));
                    }
                }
                else if (turretGunMissRadius > 0f)
                {
                    float turretGunDirectHitChance = (1f / GenRadial.NumCellsInRadius(turretGunMissRadius));
                    __result = __result.Add(new StatDrawEntry(TE_StatCategoryDefOf.Turret, "MissRadius".Translate(), turretGunMissRadius.ToString("0.#"), 14));
                    __result = __result.Add(new StatDrawEntry(TE_StatCategoryDefOf.Turret, "DirectHitChance".Translate(), turretGunDirectHitChance.ToStringPercent(), 13));
                }

                if (turretBurstShotCount > 1)
                {
                    __result = __result.Add(new StatDrawEntry(TE_StatCategoryDefOf.Turret, "BurstShotFireRate".Translate(), turretBurstShotFireRate.ToString("0.##") + " rpm", 19));
                    __result = __result.Add(new StatDrawEntry(TE_StatCategoryDefOf.Turret, "BurstShotCount".Translate(), turretBurstShotCount.ToString(), 20));
                }

                // Projectile stats
                ThingDef turretGunProjectile = turretGunVerbProps.defaultProjectile;
                string damage = (turretGunProjectile != null) ? turretGunProjectile.projectile.GetDamageAmount(turretGunThing).ToString() : "MortarShellDependent".Translate();
                string armorPenetration = (turretGunProjectile != null) ? turretGunProjectile.projectile.GetArmorPenetration(turretGunThing).ToStringPercent() : "MortarShellDependent".Translate();
                string stoppingPower = (turretGunProjectile != null) ? turretGunProjectile.projectile.StoppingPower.ToString() : "MortarShellDependent".Translate();

                __result = __result.Add(new StatDrawEntry(TE_StatCategoryDefOf.Turret, "Damage".Translate(), damage, 23));
                __result = __result.Add(new StatDrawEntry(TE_StatCategoryDefOf.Turret, "ArmorPenetration".Translate(), armorPenetration, 22, "ArmorPenetrationExplanation".Translate()));

                if (stoppingPower == "MortarShellDependent".Translate() || float.Parse(stoppingPower, CultureInfo.InvariantCulture.NumberFormat) != 0f)
                    __result = __result.Add(new StatDrawEntry(TE_StatCategoryDefOf.Turret, "StoppingPower".Translate(), stoppingPower, 21, "StoppingPowerExplanation".Translate()));

                turretGunThing.Destroy();

            }
        }

        private static string GetTurretUpgradeBenefits(Thing turret, CompUpgradable comp, TurretFrameworkExtension extensionValues)
        {
            StringBuilder upgradabilityExplanation = new StringBuilder();
            upgradabilityExplanation.AppendLine("TurretUpgradeBenefitsMain".Translate());
            upgradabilityExplanation.AppendLine();
            if (comp != null && !comp.upgraded)
            {
                ThingDef def = turret.def;

                var upgradeProps = def.GetCompProperties<CompProperties_Upgradable>();
                var defaultValues = CompProperties_Upgradable.defaultValues;
                var upgradedGunDef = upgradeProps.turretGunDef;
                var upgradedGunVerb = upgradedGunDef?.Verbs[0];
                var upgradedGunProjectile = upgradedGunVerb?.defaultProjectile;

                upgradabilityExplanation.AppendLine("Description".Translate() + ": " + upgradeProps.description);
                upgradabilityExplanation.AppendLine();

                if ((def.MadeFromStuff && upgradeProps.costStuffCount > 0) || upgradeProps.costList != null)
                {
                    List<string> itemReqs = new List<string>();

                    if (def.MadeFromStuff && upgradeProps.costStuffCount > 0)
                        itemReqs.Add(upgradeProps.costStuffCount.ToString() + "x " + "StatsReport_Material".Translate().UncapitalizeFirst());

                    if (upgradeProps.costList != null)
                        foreach (ThingDefCountClass item in upgradeProps.costList)
                            itemReqs.Add(item.count.ToString() + "x " + item.thingDef.label);

                    upgradabilityExplanation.AppendLine("TurretResourceRequirements".Translate() + ": " + GenText.ToCommaList(itemReqs).CapitalizeFirst());
                    upgradabilityExplanation.AppendLine();
                }

                if (upgradeProps.constructionSkillPrerequisite > 0)
                {
                    upgradabilityExplanation.AppendLine("TurretMinimumUpgradeSkill".Translate() + ": " + upgradeProps.constructionSkillPrerequisite.ToString());
                    upgradabilityExplanation.AppendLine();
                }

                if (upgradeProps.researchPrerequisites is List<ResearchProjectDef> researchRequirements)
                {
                    List<string> researchReqs = new List<string>();
                    foreach (ResearchProjectDef research in researchRequirements)
                        researchReqs.Add(research.label);
                    upgradabilityExplanation.AppendLine("TurretResearchRequirements".Translate() + ": " + GenText.ToCommaList(researchReqs).CapitalizeFirst());
                    upgradabilityExplanation.AppendLine();
                }

                upgradabilityExplanation.AppendLine("TurretUpgradeBenefitsUpgradable".Translate() + ":");
                upgradabilityExplanation.AppendLine();

                // General Stats
                if (!upgradeProps.statOffsets.NullOrEmpty() || !upgradeProps.statFactors.NullOrEmpty())
                {
                    List<StatDef> modifiedStats = GetUpgradeModifiedStats(turret, upgradeProps.statOffsets, upgradeProps.statFactors);
                    modifiedStats.SortBy(s => s.LabelCap);
                    foreach (StatDef stat in modifiedStats)
                    {
                        float value = turret.GetStatValue(stat);
                        if (upgradeProps.statOffsets?.StatListContains(stat) == true)
                            value += upgradeProps.statOffsets.GetStatOffsetFromList(stat);
                        if (upgradeProps.statFactors?.StatListContains(stat) == true)
                            value *= upgradeProps.statFactors.GetStatFactorFromList(stat);
                        upgradabilityExplanation.AppendLine(stat.LabelCap + ": " + value.ToStringByStyle(stat.toStringStyle));
                    }
                }

                // Max Hit Points -- LEGACY
                if (def.useHitPoints && upgradeProps.MaxHitPointsFactor != 1f)
                {
                    float maxHealthFactor = upgradeProps.MaxHitPointsFactor;
                    upgradabilityExplanation.AppendLine(StatDefOf.MaxHitPoints.label.CapitalizeFirst() + ": x" + maxHealthFactor.ToStringPercent());
                }

                // Flammability -- LEGACY
                if (def.GetStatValueAbstract(StatDefOf.Flammability) > 0f && upgradeProps.FlammabilityFactor != 1f)
                {
                    float flammabilityFactor = upgradeProps.FlammabilityFactor;
                    upgradabilityExplanation.AppendLine(StatDefOf.Flammability.label.CapitalizeFirst() + ": x" + flammabilityFactor.ToStringPercent());
                }

                // Power Consumption
                if (def.HasComp(typeof(CompPowerTrader)) && upgradeProps.basePowerConsumptionFactor != 1f)
                {
                    float newPowerConsumption = def.GetCompProperties<CompProperties_Power>().basePowerConsumption * upgradeProps.basePowerConsumptionFactor;
                    upgradabilityExplanation.AppendLine("PowerNeeded".Translate() + ": " + newPowerConsumption.ToString("#####0") + " W");
                }

                // Effective barrel durability -- LEGACY
                if ((upgradeProps.effectiveBarrelDurabilityFactor != defaultValues.effectiveBarrelDurabilityFactor || upgradeProps.barrelDurabilityFactor != defaultValues.barrelDurabilityFactor)
                    && def.HasComp(typeof(CompRefuelable)))
                {
                    float effDurability = Mathf.Ceil(def.GetCompProperties<CompProperties_Refuelable>().fuelCapacity *
                        upgradeProps.barrelDurabilityFactor * upgradeProps.effectiveBarrelDurabilityFactor);
                    upgradabilityExplanation.AppendLine(def.GetCompProperties<CompProperties_Refuelable>().fuelGizmoLabel.CapitalizeFirst() + ": " + effDurability.ToString());
                }

                // Cooldown time
                if (def.building.turretBurstCooldownTime > -1f && upgradeProps.turretBurstCooldownTimeFactor != defaultValues.turretBurstCooldownTimeFactor)
                {
                    float newCooldown = def.building.turretBurstCooldownTime * upgradeProps.turretBurstCooldownTimeFactor;
                    upgradabilityExplanation.AppendLine("CooldownTime".Translate() + ": " + newCooldown.ToString("F2") + " s");
                }

                // Warmup time
                if (def.building.turretBurstWarmupTime > 0f && upgradeProps.turretBurstWarmupTimeFactor != defaultValues.turretBurstWarmupTimeFactor)
                {
                    float newWarmup = def.building.turretBurstWarmupTime * upgradeProps.turretBurstWarmupTimeFactor;
                    upgradabilityExplanation.AppendLine("WarmupTime".Translate() + ": " + Math.Round(newWarmup, 2).ToString() + " s");
                }

                // Damage, AP, Stopping Power, Burst Count and Burst Ticks
                if (upgradedGunDef != null)
                {
                    Thing upgradedGunThing = ThingMaker.MakeThing(upgradedGunDef);
                    string newDamage = (upgradedGunProjectile != null) ? upgradedGunProjectile.projectile.GetDamageAmount(upgradedGunThing).ToString() : "MortarShellDependent".Translate();
                    string newArmorPenetration = (upgradedGunProjectile != null) ? upgradedGunProjectile.projectile.GetArmorPenetration(upgradedGunThing).ToStringPercent() : "MortarShellDependent".Translate();
                    string newStoppingPower = (upgradedGunProjectile != null) ? upgradedGunProjectile.projectile.StoppingPower.ToString() : "MortarShellDependent".Translate();
                    upgradabilityExplanation.AppendLine("Damage".Translate() + ": " + newDamage);
                    upgradabilityExplanation.AppendLine("ArmorPenetration".Translate() + ": " + newArmorPenetration);
                    if (newStoppingPower == "MortarShellDependent".Translate() || float.Parse(newStoppingPower, CultureInfo.InvariantCulture.NumberFormat) != 0f)
                        upgradabilityExplanation.AppendLine("StoppingPower".Translate() + ": " + newStoppingPower);

                    int newBurstCount = upgradedGunVerb.burstShotCount;
                    if (newBurstCount > 1)
                    {
                        float newBurstFireRate = 60f / upgradedGunVerb.ticksBetweenBurstShots.TicksToSeconds();
                        upgradabilityExplanation.AppendLine("BurstShotCount".Translate() + ": " + newBurstCount.ToString());
                        upgradabilityExplanation.AppendLine("BurstShotFireRate".Translate() + ": " + newBurstFireRate.ToString("0.##") + " rpm");
                    }
                    upgradedGunThing.Destroy();
                }

                // Shooting accuracy  -- LEGACY
                if (extensionValues.useMannerShootingAccuracy && upgradeProps.mannerShootingAccuracyOffsetOffset != defaultValues.mannerShootingAccuracyOffsetOffset
                    && def.HasComp(typeof(CompMannable)))
                {
                    float newShootAcc = extensionValues.mannerShootingAccuracyOffset + upgradeProps.mannerShootingAccuracyOffsetOffset;
                    if (newShootAcc >= 0f) upgradabilityExplanation.AppendLine("UserShootingAccuracy".Translate() + ": +" + newShootAcc.ToString("0.#"));
                    else upgradabilityExplanation.AppendLine("UserShootingAccuracy".Translate() + ": " + newShootAcc.ToString("0.#"));
                }
                else if (upgradeProps.ShootingAccuracyTurretOffset != defaultValues.ShootingAccuracyTurretOffset)
                {
                    float newShootAcc = Mathf.Clamp01(def.GetStatValueAbstract(StatDefOf.ShootingAccuracyTurret) + upgradeProps.ShootingAccuracyTurretOffset);
                    upgradabilityExplanation.AppendLine(StatDefOf.ShootingAccuracyTurret.label.CapitalizeFirst() + ": " + newShootAcc.ToStringPercent("F2"));
                }

                // Accuracy (touch, short, medium and long) and Range
                if (upgradedGunDef != null)
                {
                    float newMissRadius = upgradedGunVerb.forcedMissRadius;
                    if (newMissRadius > 0f)
                    {
                        float newDirHitChance = (1f / GenRadial.NumCellsInRadius(newMissRadius));
                        upgradabilityExplanation.AppendLine("MissRadius".Translate() + ": " + newMissRadius.ToString("0.#"));
                        upgradabilityExplanation.AppendLine("DirectHitChance".Translate() + ": " + newDirHitChance.ToStringPercent());
                    }
                    else
                    {
                        float newTouchAcc = upgradedGunDef.statBases.GetStatValueFromList(StatDefOf.AccuracyTouch, StatDefOf.AccuracyTouch.defaultBaseValue);
                        float newShortAcc = upgradedGunDef.statBases.GetStatValueFromList(StatDefOf.AccuracyShort, StatDefOf.AccuracyShort.defaultBaseValue);
                        float newMediumAcc = upgradedGunDef.statBases.GetStatValueFromList(StatDefOf.AccuracyMedium, StatDefOf.AccuracyMedium.defaultBaseValue);
                        float newLongAcc = upgradedGunDef.statBases.GetStatValueFromList(StatDefOf.AccuracyLong, StatDefOf.AccuracyLong.defaultBaseValue);
                        upgradabilityExplanation.AppendLine(StatDefOf.AccuracyTouch.LabelCap + ": " + newTouchAcc.ToStringPercent());
                        upgradabilityExplanation.AppendLine(StatDefOf.AccuracyShort.LabelCap + ": " + newShortAcc.ToStringPercent());
                        upgradabilityExplanation.AppendLine(StatDefOf.AccuracyMedium.LabelCap + ": " + newMediumAcc.ToStringPercent());
                        upgradabilityExplanation.AppendLine(StatDefOf.AccuracyLong.LabelCap + ": " + newLongAcc.ToStringPercent());
                    }

                    float newMinRange = upgradedGunVerb.minRange;
                    float newRange = upgradedGunVerb.range;
                    if (newMinRange > 0f)
                        upgradabilityExplanation.AppendLine("MinimumRange".Translate() + ": " + newMinRange.ToString("0"));
                    upgradabilityExplanation.AppendLine("Range".Translate() + ": " + newRange.ToString("0"));
                }

                // Manual aiming
                if (!def.HasComp(typeof(CompMannable)) && !extensionValues.canForceAttack && upgradeProps.canForceAttack)
                    upgradabilityExplanation.AppendLine("TurretManuallyAimable".Translate());

            }

            else
                upgradabilityExplanation.AppendLine("TurretUpgradeBenefitsNotUpgradable".Translate());

            return upgradabilityExplanation.ToString();
        }

        private static List<StatDef> GetUpgradeModifiedStats(Thing turret, List<StatModifier> statOffsets, List<StatModifier> statFactors)
        {
            List<StatDef> resultList = new List<StatDef>();
            if (!statOffsets.NullOrEmpty())
                foreach (StatModifier offset in statOffsets)
                    resultList.Add(offset.stat);
            if (!statFactors.NullOrEmpty())
                foreach (StatModifier factor in statFactors)
                    if (!resultList.Contains(factor.stat))
                        resultList.Add(factor.stat);
            return resultList;
        }
        #endregion

        #region PostfixInitDesignators
        public static void PostfixInitDesignators(ReverseDesignatorDatabase __instance, ref List<Designator> ___desList)
        {
            ___desList.Add(new Designator_UpgradeTurret());
        }
        #endregion

        #region PostfixSetUpPowerVars
        public static void PostfixSetUpPowerVars(CompPowerTrader __instance)
        {
            if (__instance.parent.IsUpgradedTurret(out CompUpgradable upgradableComp))
                __instance.PowerOutput *= upgradableComp.Props.basePowerConsumptionFactor;
        }
        #endregion

        #region PostfixGetValueUnfinalized
        public static void PostfixGetValueUnfinalized(StatWorker __instance, StatRequest req, ref float __result, StatDef ___stat)
        {
            if (req.Thing.IsUpgradedTurret(out CompUpgradable uC))
            {
                CompProperties_Upgradable props = uC.Props;
                if (props.statOffsets != null)
                    __result += props.statOffsets.GetStatOffsetFromList(___stat);
                if (props.statFactors != null)
                    __result *= props.statFactors.GetStatFactorFromList(___stat);
            }
        }
        #endregion

        #region PostfixGetExplanationUnfinalized
        public static void PostfixGetExplanationUnfinalized(StatWorker __instance, StatRequest req, ref string __result, StatDef ___stat)
        {
            if (req.Thing.IsUpgradedTurret(out CompUpgradable uC))
            {
                CompProperties_Upgradable props = uC.Props;
                float? offset = props.statOffsets?.GetStatOffsetFromList(___stat);
                float? factor = props.statFactors.GetStatFactorFromList(___stat);
                if (props.statOffsets != null && offset != 0f)
                    __result += "\n\n" + "TurretUpgradedText".Translate().CapitalizeFirst() + ": " +
                        ((float)offset).ToStringByStyle(___stat.toStringStyle, ToStringNumberSense.Offset);
                if (props.statFactors != null && factor != 1f)
                    __result += "\n\n" + "TurretUpgradedText".Translate().CapitalizeFirst() + ": " +
                        ((float)factor).ToStringByStyle(___stat.toStringStyle, ToStringNumberSense.Factor);
            }
        }
        #endregion


    }

}

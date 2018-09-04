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

            h.Patch(AccessTools.Method(typeof(Building_TurretGun), "GetInspectString"),
                transpiler: new HarmonyMethod(patchType, nameof(TranspileGetInspectString)));

            #region grossly overused transpiler
            h.Patch(AccessTools.Method(typeof(CompRefuelable), nameof(CompRefuelable.Refuel), new[]{ typeof(float)}),
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

            h.Patch(AccessTools.Method(typeof(Building_TurretGun), "BurstCooldownTime"),
                postfix: new HarmonyMethod(patchType, nameof(PostfixBurstCooldownTime)));

            h.Patch(AccessTools.Method(typeof(Building_TurretGun), "TryStartShootSomething"),
                postfix: new HarmonyMethod(patchType, nameof(PostfixTryStartShootSomething)));

            h.Patch(AccessTools.Property(typeof(Building_TurretGun), "CanSetForcedTarget").GetGetMethod(true),
                postfix: new HarmonyMethod(patchType, nameof(PostfixCanSetForcedTarget)));

            h.Patch(AccessTools.Method(typeof(ThingDef), "SpecialDisplayStats"),
                postfix: new HarmonyMethod(patchType, nameof(PostfixSpecialDisplayStats)));

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

        #region PatchRefuel
        public static void PrefixRefuel(CompRefuelable __instance, ref float amount)
        {
            if (__instance.parent.IsUpgradedTurret(out CompUpgradable upgradableComp))
                amount *= upgradableComp.Props.effectiveBarrelDurabilityFactor;
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
                    Log.Warning(String.Format("Turret (defName={0}) has canForceAttack set to true and CompMannable. canForceAttack is redundant in this case.", turretDefName));
            }
        }
        #endregion

        #region PostfixSpecialDisplayStats
        public static void PostfixSpecialDisplayStats(ThingDef __instance, ref IEnumerable<StatDrawEntry> __result)
        {
            if (__instance.building != null && __instance.building.IsTurret)
            {
                var extensionValues = __instance.GetModExtension<TurretFrameworkExtension>() ?? TurretFrameworkExtension.defaultValues;

                // Upgradability
                bool upgradable = __instance.IsUpgradableTurret();
                string turretUpgradableString = ((upgradable) ? "YesClickForDetails" : "No").Translate();
                StatDrawEntry upgradabilitySDE = new StatDrawEntry(TE_StatCategoryDefOf.Turret, "Upgradable".Translate(), turretUpgradableString, 50, GetTurretUpgradeBenefits(__instance, upgradable, extensionValues));
                __result = __result.Add(upgradabilitySDE);

                // Building stats
                float turretBurstWarmupTime = __instance.building.turretBurstWarmupTime;
                float turretBurstCooldownTime = __instance.building.turretBurstCooldownTime;
                float turretShootingAccuracy = __instance.GetStatValueAbstract(StatDefOf.ShootingAccuracyTurret);
                bool turretUsesMannerShootingAccuracy = extensionValues.useMannerShootingAccuracy;

                // Turret gun stats
                ThingDef turretGunDef = __instance.building.turretGunDef;
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
                    if (__instance.HasComp(typeof(CompMannable)))
                    {
                        string shootingAccExplanation = "MannedTurretUsesShooterAccuracyExplanation".Translate();
                        if (turretUsesMannerShootingAccuracy)
                        {
                            shootingAccExplanation += "\n\n";
                            if (extensionValues.mannerShootingAccuracyOffset != 0f)
                                shootingAccExplanation += ((extensionValues.mannerShootingAccuracyOffset > 0f) ? "MannedTurretUsesShooterAccuracyBonus".Translate(new object[] { extensionValues.mannerShootingAccuracyOffset.ToString("0.#"), __instance.label }) : "MannedTurretUsesShooterAccuracyPenalty".Translate(new object[] { extensionValues.mannerShootingAccuracyOffset.ToString("0.#"), __instance.label }));
                            else
                                shootingAccExplanation += "MannedTurretUsesShooterAccuracyNoChange".Translate(__instance.label);
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

        private static string GetTurretUpgradeBenefits(ThingDef def, bool upgradable, TurretFrameworkExtension extensionValues)
        {
            StringBuilder upgradabilityExplanation = new StringBuilder();
            upgradabilityExplanation.AppendLine("TurretUpgradeBenefitsMain".Translate());
            upgradabilityExplanation.AppendLine();
            if (upgradable)
            {
                var upgradeProps = def.GetCompProperties<CompProperties_Upgradable>();
                var defaultValues = CompProperties_Upgradable.defaultValues;
                var upgradedGunDef = upgradeProps.turretGunDef;
                var upgradedGunVerb = upgradedGunDef?.Verbs[0];
                var upgradedGunProjectile = upgradedGunVerb?.defaultProjectile;

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

                // Max Hit Points
                if (def.useHitPoints && upgradeProps.MaxHitPointsFactor != 1f)
                {
                    float maxHealthFactor = upgradeProps.MaxHitPointsFactor;
                    upgradabilityExplanation.AppendLine(StatDefOf.MaxHitPoints.label.CapitalizeFirst() + ": x" + maxHealthFactor.ToStringPercent());
                }

                // Flammability
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

                // Effective barrel durability
                if (upgradeProps.effectiveBarrelDurabilityFactor != defaultValues.effectiveBarrelDurabilityFactor && def.HasComp(typeof(CompRefuelable)))
                {
                    float effDurability = Mathf.Ceil(def.GetCompProperties<CompProperties_Refuelable>().fuelCapacity * upgradeProps.effectiveBarrelDurabilityFactor);
                    upgradabilityExplanation.AppendLine("Effective".Translate() + " " + def.GetCompProperties<CompProperties_Refuelable>().fuelGizmoLabel.UncapitalizeFirst() + ": " + effDurability.ToString());
                }

                // Cooldown time
                if (def.building.turretBurstCooldownTime > -1f && upgradeProps.turretBurstCooldownTimeFactor != defaultValues.turretBurstCooldownTimeFactor)
                {
                    float newCooldown = def.building.turretBurstCooldownTime * upgradeProps.turretBurstCooldownTimeFactor;
                    upgradabilityExplanation.AppendLine("CooldownTime".Translate() + ": " + newCooldown.ToString() + " s");
                }

                // Warmup time
                if (def.building.turretBurstWarmupTime > 0f && upgradeProps.turretBurstWarmupTimeFactor != defaultValues.turretBurstWarmupTimeFactor)
                {
                    float newWarmup = def.building.turretBurstWarmupTime * upgradeProps.turretBurstWarmupTimeFactor;
                    upgradabilityExplanation.AppendLine("WarmupTime".Translate() + ": " + newWarmup.ToString() + " s");
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

                // Shooting accuracy
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
                    upgradabilityExplanation.AppendLine(StatDefOf.ShootingAccuracyTurret.label.CapitalizeFirst() + ": " + newShootAcc.ToStringPercent("0.##"));
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
            else upgradabilityExplanation.AppendLine("TurretUpgradeBenefitsNotUpgradable".Translate());

            return upgradabilityExplanation.ToString();
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

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

    public static class Patch_StatsReportUtility
    {

        // I frankly wouldn't wish the following code on my worst enemy

        [HarmonyPatch(typeof(StatsReportUtility))]
        [HarmonyPatch("StatsToDraw")]
        [HarmonyPatch(new Type[] { typeof(Def), typeof(ThingDef) })]
        public static class Patch_StatsToDraw_Def_ThingDef
        {

            public static void Postfix(Def def, ThingDef stuff, ref IEnumerable<StatDrawEntry> __result)
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

            #region Upgrade Spaghetti (ThingDef edition)
            public static string GetTurretUpgradeBenefits(ThingDef def, ThingDef stuff, CompProperties_Upgradable upgradeProps, TurretFrameworkExtension extensionValues)
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
                        upgradabilityExplanation.AppendLine("PowerConsumption".Translate() + ": " + newPowerConsumption.ToString("#####0") + " W");
                    }

                    // Effective barrel durability -- LEGACY
                    if ((upgradeProps.effectiveBarrelDurabilityFactor != defaultValues.effectiveBarrelDurabilityFactor || upgradeProps.barrelDurabilityFactor != defaultValues.barrelDurabilityFactor)
                        && def.HasComp(typeof(CompRefuelable)))
                    {
                        float effDurability = Mathf.Ceil(def.GetCompProperties<CompProperties_Refuelable>().fuelCapacity *
                            upgradeProps.barrelDurabilityFactor * upgradeProps.effectiveBarrelDurabilityFactor);
                        upgradabilityExplanation.AppendLine("ShotsBeforeRearm".Translate() + ": " + effDurability.ToString());
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

            public static List<StatDef> GetUpgradeModifiedStats(ThingDef turret, List<StatModifier> statOffsets, List<StatModifier> statFactors)
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

        }

        [HarmonyPatch(typeof(StatsReportUtility))]
        [HarmonyPatch("StatsToDraw")]
        [HarmonyPatch(new Type[] { typeof(Thing) })]
        public static class Patch_StatsToDraw_Thing
        {

            public static void Postfix(Thing thing, ref IEnumerable<StatDrawEntry> __result)
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

            #region Upgrade Spaghetti (Thing edition)
            public static string GetTurretUpgradeBenefits(Thing turret, CompUpgradable comp, TurretFrameworkExtension extensionValues)
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

            public static List<StatDef> GetUpgradeModifiedStats(Thing turret, List<StatModifier> statOffsets, List<StatModifier> statFactors)
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

        }

        [HarmonyPatch(typeof(StatsReportUtility))]
        [HarmonyPatch("DescriptionEntry")]
        [HarmonyPatch(new Type[] { typeof(Thing) })]
        public static class Patch_DescriptionEntry_Thing
        {

            public static void Postfix(Thing thing, ref StatDrawEntry __result)
            {
                if (thing is Building_Turret turret && turret.IsUpgradedTurret(out CompUpgradable uC) && uC.Props.upgradedTurretDescription != null)
                {
                    __result.overrideReportText = uC.Props.upgradedTurretDescription;
                    if (ModLister.HasActiveModWithName("ShowModDesignators"))
                    {
                        ModContentPack mcp = LoadedModManager.RunningModsListForReading.Last(m => m.AllDefs.Contains(thing.def));
                        __result.overrideReportText += $"\n({mcp.Name})";
                    }
                }
            }

        }

    }

}

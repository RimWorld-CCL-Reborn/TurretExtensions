using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
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

            // HarmonyInstance.DEBUG = true;

            h.Patch(AccessTools.Method(typeof(Building_TurretGun), "GetInspectString"), null, null,
                new HarmonyMethod(patchType, "TranspileGetInspectString"));

            h.Patch(AccessTools.Method(typeof(CompRefuelable), "ConsumeFuel"),
                new HarmonyMethod(patchType, "PrefixConsumeFuel"), null);

            h.Patch(AccessTools.Method(typeof(Designator_Cancel), "DesignateThing"),
                new HarmonyMethod(patchType, "PrefixDesignateThing"), null);

            h.Patch(AccessTools.Method(typeof(Building_TurretGun), "BurstCooldownTime"), null,
                new HarmonyMethod(patchType, "PostfixBurstCooldownTime"));

            h.Patch(AccessTools.Method(typeof(Building_TurretGun), "TryStartShootSomething"), null,
                new HarmonyMethod(patchType, "PostfixTryStartShootSomething"));

            h.Patch(AccessTools.Property(typeof(Building_TurretGun), "CanSetForcedTarget").GetGetMethod(true), null,
                new HarmonyMethod(patchType, "PostfixCanSetForcedTarget"));

            h.Patch(AccessTools.Method(typeof(ThingDef), "SpecialDisplayStats"), null,
                new HarmonyMethod(patchType, "PostfixSpecialDisplayStats"));

            h.Patch(AccessTools.Method(typeof(ReverseDesignatorDatabase), "InitDesignators"), null,
                new HarmonyMethod(patchType, "PostfixInitDesignators"));

            h.Patch(AccessTools.Property(typeof(CompPowerTrader), "PowerOutput").GetGetMethod(), null,
                new HarmonyMethod(patchType, "PostfixPowerOutput"));

            // Thanks erdelf!
            Log.Message(text: $"Turret Extensions successfully completed {h.GetPatchedMethods().Select(selector: mb => h.GetPatchInfo(method: mb)).SelectMany(selector: p => p.Prefixes.Concat(second: p.Postfixes).Concat(second: p.Transpilers)).Count(predicate: p => p.owner == h.Id)} patches with harmony.");

        }

        #region TranspileGetInspectString
        public static IEnumerable<CodeInstruction> TranspileGetInspectString(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionList = instructions.ToList();

            for (int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];

                if (instruction.opcode == OpCodes.Ldc_R4 && (float)instruction.operand == 5)
                {
                    instruction.operand = 0;
                }

                yield return instruction;
            }
        }
        #endregion

        #region PrefixConsumeFuel
        public static void PrefixConsumeFuel(CompRefuelable __instance, ref float amount)
        {
            CompUpgradable upgradableComp = __instance.parent.TryGetComp<CompUpgradable>();
            if (upgradableComp != null && upgradableComp.upgraded)
            {
                amount /= upgradableComp.Props.effectiveBarrelDurabilityFactor;
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
            CompUpgradable upgradableComp = __instance.TryGetComp<CompUpgradable>();
            if (upgradableComp != null && upgradableComp.upgraded)
            {
                __result *= upgradableComp.Props.turretBurstCooldownTimeFactor;
            }
        }
        #endregion

        #region PostfixTryStartShootSomething
        public static void PostfixTryStartShootSomething(Building_TurretGun __instance, ref int ___burstWarmupTicksLeft)
        {
            var extensionValues = __instance.def.GetModExtension<TurretFrameworkExtension>() ?? TurretFrameworkExtension.defaultValues;
            CompUpgradable upgradableComp = __instance.TryGetComp<CompUpgradable>();
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
                {
                    Log.Warning(String.Format("Turret (defName={0}) has useMannerAimingDelayFactor set to true but doesn't have CompMannable.", turretDefName));
                }
            }
            if (upgradableComp != null && upgradableComp.upgraded)
            {
                ___burstWarmupTicksLeft = (int)Math.Round(___burstWarmupTicksLeft * upgradableComp.Props.turretBurstWarmupTimeFactor);
            }
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
                if (mannableComp == null)
                {
                    if (__instance.Faction == Faction.OfPlayer)
                    {
                        __result = true;
                    }
                }
                else
                {
                    Log.Warning(String.Format("Turret (defName={0}) has canForceAttack set to true and CompMannable. canForceAttack is redundant in this case.", turretDefName));
                }
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
                bool turretIsUpgradable = __instance.HasComp(typeof(CompUpgradable));
                string turretUpgradableString = "No".Translate();
                if (turretIsUpgradable) turretUpgradableString = "Yes".Translate();
                StatDrawEntry upgradabilitySDE = new StatDrawEntry(TE_StatCategoryDefOf.Turret, "Upgradable".Translate(), turretUpgradableString, 50, GetTurretUpgradeBenefits(__instance, turretIsUpgradable, extensionValues));
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
                {
                    StatDrawEntry warmupSDE = new StatDrawEntry(TE_StatCategoryDefOf.Turret, "WarmupTime".Translate(), turretBurstWarmupTime.ToString("0.##") + " s", 40);
                    __result = __result.Add(warmupSDE);
                }
                if (turretBurstCooldownTime > 0)
                {
                    StatDrawEntry cooldownSDE = new StatDrawEntry(TE_StatCategoryDefOf.Turret, "CooldownTime".Translate(), turretBurstCooldownTime.ToString("0.##") + " s", 40);
                    __result = __result.Add(cooldownSDE);
                }
                if (turretGunMinRange > 0f)
                {
                    StatDrawEntry minRangeSDE = new StatDrawEntry(TE_StatCategoryDefOf.Turret, "MinimumRange".Translate(), turretGunMinRange.ToString("0"), 10);
                    __result = __result.Add(minRangeSDE);
                }
                StatDrawEntry rangeSDE = new StatDrawEntry(TE_StatCategoryDefOf.Turret, "Range".Translate(), turretGunRange.ToString("0"), 9);
                __result = __result.Add(rangeSDE);

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
                            StatDrawEntry turrShootingAccSDE = new StatDrawEntry(TE_StatCategoryDefOf.Turret, "MannedTurretUsesShooterAccuracy".Translate(),
                            (extensionValues.useMannerShootingAccuracy) ? "Yes".Translate() : "No".Translate(), 15, shootingAccExplanation);
                        __result = __result.Add(turrShootingAccSDE);
                    }

                    // Accuracy for weapon
                    if (turretGunAccuracyTouch != StatDefOf.AccuracyTouch.defaultBaseValue || turretGunAccuracyShort != StatDefOf.AccuracyShort.defaultBaseValue ||
                    turretGunAccuracyMedium != StatDefOf.AccuracyMedium.defaultBaseValue || turretGunAccuracyLong != StatDefOf.AccuracyLong.defaultBaseValue)
                    {
                        StatDrawEntry accTouchSDE = new StatDrawEntry(TE_StatCategoryDefOf.Turret, StatDefOf.AccuracyTouch.label, turretGunAccuracyTouch.ToStringPercent(), 14, StatDefOf.AccuracyTouch.description);
                        StatDrawEntry accShortSDE = new StatDrawEntry(TE_StatCategoryDefOf.Turret, StatDefOf.AccuracyShort.label, turretGunAccuracyShort.ToStringPercent(), 13, StatDefOf.AccuracyShort.description);
                        StatDrawEntry accMediumSDE = new StatDrawEntry(TE_StatCategoryDefOf.Turret, StatDefOf.AccuracyMedium.label, turretGunAccuracyMedium.ToStringPercent(), 12, StatDefOf.AccuracyMedium.description);
                        StatDrawEntry accLongSDE = new StatDrawEntry(TE_StatCategoryDefOf.Turret, StatDefOf.AccuracyLong.label, turretGunAccuracyLong.ToStringPercent(), 11, StatDefOf.AccuracyLong.description);
                        __result = __result.Add(accTouchSDE);
                        __result = __result.Add(accShortSDE);
                        __result = __result.Add(accMediumSDE);
                        __result = __result.Add(accLongSDE);
                    }
                }
                else if (turretGunMissRadius > 0f)
                {
                    float turretGunDirectHitChance = (1f / GenRadial.NumCellsInRadius(turretGunMissRadius));
                    StatDrawEntry missRadiusSDE = new StatDrawEntry(TE_StatCategoryDefOf.Turret, "MissRadius".Translate(), turretGunMissRadius.ToString("0.#"), 14);
                    StatDrawEntry dirHitSDE = new StatDrawEntry(TE_StatCategoryDefOf.Turret, "DirectHitChance".Translate(), turretGunDirectHitChance.ToStringPercent(), 13);
                    __result = __result.Add(missRadiusSDE);
                    __result = __result.Add(dirHitSDE);
                }

                if (turretBurstShotCount > 1)
                {
                    StatDrawEntry burstShotFireRateSDE = new StatDrawEntry(TE_StatCategoryDefOf.Turret, "BurstShotFireRate".Translate(), turretBurstShotFireRate.ToString("0.##") + " rpm", 19);
                    StatDrawEntry burstShotCountSDE = new StatDrawEntry(TE_StatCategoryDefOf.Turret, "BurstShotCount".Translate(), turretBurstShotCount.ToString(), 20);
                    __result = __result.Add(burstShotFireRateSDE);
                    __result = __result.Add(burstShotCountSDE);
                }

                // Projectile stats
                ThingDef turretGunProjectile = turretGunVerbProps.defaultProjectile;
                string damage = (turretGunProjectile != null) ? turretGunProjectile.projectile.GetDamageAmount(turretGunThing).ToString() : "MortarShellDependent".Translate();
                string armorPenetration = (turretGunProjectile != null) ? turretGunProjectile.projectile.GetArmorPenetration(turretGunThing).ToStringPercent() : "MortarShellDependent".Translate();
                string stoppingPower = (turretGunProjectile != null) ? turretGunProjectile.projectile.StoppingPower.ToString() : "MortarShellDependent".Translate();

                StatDrawEntry damageSDE = new StatDrawEntry(TE_StatCategoryDefOf.Turret, "Damage".Translate(), damage, 23);
                StatDrawEntry armorPenetrationSDE = new StatDrawEntry(TE_StatCategoryDefOf.Turret, "ArmorPenetration".Translate(), armorPenetration, 22, "ArmorPenetrationExplanation".Translate());
                __result = __result.Add(damageSDE);
                __result = __result.Add(armorPenetrationSDE);

                if (stoppingPower == "MortarShellDependent".Translate() || float.Parse(stoppingPower, CultureInfo.InvariantCulture.NumberFormat) != 0f)
                {
                    StatDrawEntry stoppingPowerSDE = new StatDrawEntry(TE_StatCategoryDefOf.Turret, "StoppingPower".Translate(), stoppingPower, 21, "StoppingPowerExplanation".Translate());
                    __result = __result.Add(stoppingPowerSDE);
                }
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

                StatDrawEntry damageSDE = new StatDrawEntry(TE_StatCategoryDefOf.TurretAmmo, "Damage".Translate(), shellDamage.ToString(), 20);
                StatDrawEntry damageDefSDE = new StatDrawEntry(TE_StatCategoryDefOf.TurretAmmo, "ShellDamageType".Translate(), shellDamageDef, 19);
                StatDrawEntry penetrationSDE = new StatDrawEntry(TE_StatCategoryDefOf.TurretAmmo, "ArmorPenetration".Translate(), shellArmorPenetration.ToStringPercent(), 18, "ArmorPenetrationExplanation".Translate());
                StatDrawEntry stoppingSDE = new StatDrawEntry(TE_StatCategoryDefOf.TurretAmmo, "StoppingPower".Translate(), shellStoppingPower.ToString(), 17, "StoppingPowerExplanation".Translate());
                __result = __result.Add(damageSDE);
                __result = __result.Add(damageDefSDE);
                __result = __result.Add(penetrationSDE);
                __result = __result.Add(stoppingSDE);

                if (shellExplosionRadius > 0f)
                {
                    StatDrawEntry explosionRadSDE = new StatDrawEntry(TE_StatCategoryDefOf.TurretAmmo, "ShellExplosionRadius".Translate(), shellExplosionRadius.ToString(), 16);
                    __result = __result.Add(explosionRadSDE);
                }
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
                    upgradabilityExplanation.AppendLine("EffectiveBarrelDurability".Translate() + ": " + effDurability.ToString());
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
                if(upgradedGunDef != null)
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
                {
                    upgradabilityExplanation.AppendLine("TurretManuallyAimable".Translate());
                }

            }
            else upgradabilityExplanation.AppendLine("TurretUpgradeBenefitsNotUpgradable".Translate()); 

            return upgradabilityExplanation.ToString();
        }
        #endregion

        #region PostfixInitDesignators
        public static void PostfixInitDesignators(ReverseDesignatorDatabase __instance, List<Designator> ___desList)
        {
            ___desList.Add(new Designator_UpgradeTurret());
        }
        #endregion

        #region PostfixPowerOutput
        public static void PostfixPowerOutput(CompPowerTrader __instance, ref float __result)
        {
            if (__instance.parent.TryGetComp<CompUpgradable>() is CompUpgradable upgradableComp && upgradableComp.upgraded)
            {
                __result *= upgradableComp.Props.basePowerConsumptionFactor;
            }
        }
        #endregion

    }

}

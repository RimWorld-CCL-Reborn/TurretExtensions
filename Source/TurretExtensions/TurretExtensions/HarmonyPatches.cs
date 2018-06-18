using System;
using System.Collections.Generic;
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
            HarmonyInstance h = HarmonyInstance.Create("XeoNovaDan.ExtendedTurretFramework");

            // HarmonyInstance.DEBUG = true;

            h.Patch(AccessTools.Method(typeof(ShotReport), "HitReportFor"), null, null,
                new HarmonyMethod(patchType, "TranspileTurretAccuracy"));

            h.Patch(AccessTools.Method(typeof(Building_TurretGun), "GetInspectString"), null, null,
                new HarmonyMethod(patchType, "TranspileGetInspectString"));

            h.Patch(AccessTools.Method(typeof(Building_TurretGun), "TryStartShootSomething"), null,
                new HarmonyMethod(patchType, "PostfixTryStartShootSomething"));

            h.Patch(AccessTools.Property(typeof(Building_TurretGun), "CanSetForcedTarget").GetGetMethod(true), null,
                new HarmonyMethod(patchType, "PostfixCanSetForcedTarget"));

            h.Patch(AccessTools.Method(typeof(ThingDef), "SpecialDisplayStats"), null,
                new HarmonyMethod(patchType, "PostfixSpecialDisplayStats"));

        }

        public static IEnumerable<CodeInstruction> TranspileTurretAccuracy(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionList = instructions.ToList();
            MethodInfo accGetterInfo = AccessTools.Method(patchType, nameof(GetTurretAccuracy));

            bool foundInstruction = false;
            bool done = false;
            
            for (int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];

                if (foundInstruction && !done)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, accGetterInfo);
                    done = true;
                }

                if (instruction.opcode == OpCodes.Ldc_R4 && !done && !foundInstruction)
                {
                    instruction.opcode = OpCodes.Nop;
                    foundInstruction = true;
                }

                yield return instruction;
            }
        }

        private static float GetTurretAccuracy(Thing turret)
        {
            var extensionValues = turret.def.GetModExtension<TurretFrameworkExtension>() ?? TurretFrameworkExtension.defaultValues;
            CompMannable mannableComp = turret.TryGetComp<CompMannable>();
            string turretDefName = turret.def.defName;

            if (extensionValues.useMannerShootingAccuracy)
            {
                if (mannableComp != null)
                {
                    Pawn manningPawn = mannableComp.ManningPawn;
                    if (manningPawn != null)
                    {
                        return manningPawn.GetStatValue(StatDefOf.ShootingAccuracy);
                    }
                }
                else
                {
                    Log.Warning(String.Format("Turret (defName={0}) has useMannerShootingAccuracy set to true but doesn't have CompMannable.", turretDefName));
                }
            }

            if (extensionValues.shootingAccuracy < 0 || extensionValues.shootingAccuracy > 1)
            {
                if (extensionValues.shootingAccuracy < 0)
                {
                    Log.ErrorOnce(String.Format("Turret (defName={0}) has a shootingAccuracy value of less than 0. Defaulting to 0.96...", turretDefName), 614927384);
                }
                else
                {
                    Log.ErrorOnce(String.Format("Turret (defName={0}) has a shootingAccuracy value of greater than 1. Defaulting to 0.96...", turretDefName), 614927385);
                }
                return 0.96f;
            }

            return extensionValues.shootingAccuracy;
        }

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

        public static void PostfixTryStartShootSomething(Building_TurretGun __instance)
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
                        int burstWarmupTicksLeft = Traverse.Create(__instance).Field("burstWarmupTicksLeft").GetValue<int>();
                        float mannerAimingDelayFactor = manner.GetStatValue(StatDefOf.AimingDelayFactor);
                        burstWarmupTicksLeft = (int)Math.Round(burstWarmupTicksLeft * mannerAimingDelayFactor);
                        Traverse.Create(__instance).Field("burstWarmupTicksLeft").SetValue(burstWarmupTicksLeft);
                    }
                }
                else
                {
                    Log.Warning(String.Format("Turret (defName={0}) has useMannerAimingDelayFactor set to true but doesn't have CompMannable.", turretDefName));
                }
            }
        }

        public static void PostfixCanSetForcedTarget(Building_TurretGun __instance, ref bool __result)
        {
            var extensionValues = __instance.def.GetModExtension<TurretFrameworkExtension>() ?? TurretFrameworkExtension.defaultValues;
            string turretDefName = __instance.def.defName;

            if (extensionValues.canForceAttack)
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

        public static void PostfixSpecialDisplayStats(ThingDef __instance, ref IEnumerable<StatDrawEntry> __result)
        {
            if (__instance.building != null && __instance.building.IsTurret)
            {
                var extensionValues = __instance.GetModExtension<TurretFrameworkExtension>() ?? TurretFrameworkExtension.defaultValues;

                // Building stats
                float turretBurstWarmupTime = __instance.building.turretBurstWarmupTime;
                float turretBurstCooldownTime = __instance.building.turretBurstCooldownTime;
                float turretShootingAccuracy = extensionValues.shootingAccuracy;
                bool turretUsesMannerShootingAccuracy = extensionValues.useMannerShootingAccuracy;
                string turretShootingAccuracyString = turretShootingAccuracy.ToStringPercent();
                if (turretUsesMannerShootingAccuracy)
                {
                    turretShootingAccuracyString = "TurretUserDependent".Translate();
                }
                StringBuilder turretSAExplanationBuilder = new StringBuilder();
                turretSAExplanationBuilder.AppendLine(StatDefOf.ShootingAccuracy.description);
                if (!turretUsesMannerShootingAccuracy)
                {
                    turretSAExplanationBuilder.AppendLine();
                    turretSAExplanationBuilder.AppendLine(String.Concat(new string[]
                    {
                        StatDefOf.ShootingAccuracy.label.CapitalizeFirst(),
                        ": ",
                        turretShootingAccuracy.ToStringPercent()
                    }));
                    turretSAExplanationBuilder.AppendLine();
                    for (int i = 5; i <= 45; i += 5)
                    {
                        turretSAExplanationBuilder.AppendLine(String.Concat(new string[]
                        {
                        "distance".Translate().CapitalizeFirst(),
                        " ",
                        i.ToString(),
                        ": ",
                        Mathf.Pow(turretShootingAccuracy, (float)i).ToStringPercent()
                        }));
                    }
                }
                string turretSAExplanation = turretSAExplanationBuilder.ToString();

                // Turret gun stats
                ThingDef turretGunDef = __instance.building.turretGunDef;
                float turretGunAccuracyTouch = turretGunDef.statBases.GetStatValueFromList(StatDefOf.AccuracyTouch, StatDefOf.AccuracyTouch.defaultBaseValue);
                float turretGunAccuracyShort = turretGunDef.statBases.GetStatValueFromList(StatDefOf.AccuracyShort, StatDefOf.AccuracyShort.defaultBaseValue);
                float turretGunAccuracyMedium = turretGunDef.statBases.GetStatValueFromList(StatDefOf.AccuracyMedium, StatDefOf.AccuracyMedium.defaultBaseValue);
                float turretGunAccuracyLong = turretGunDef.statBases.GetStatValueFromList(StatDefOf.AccuracyLong, StatDefOf.AccuracyLong.defaultBaseValue);

                // Verb stats
                List<VerbProperties> turretGunVerbPropList = turretGunDef.Verbs;
                VerbProperties turretGunVerbProps = turretGunVerbPropList[0];
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
                StatDrawEntry rangeSDE = new StatDrawEntry(TE_StatCategoryDefOf.Turret, "Range".Translate(), turretGunRange.ToString("0"), 10);
                StatDrawEntry turrShootingAccSDE = new StatDrawEntry(TE_StatCategoryDefOf.Turret, StatDefOf.ShootingAccuracy.label, turretShootingAccuracyString, 15, turretSAExplanation);
                __result = __result.Add(rangeSDE);
                __result = __result.Add(turrShootingAccSDE);
                if (turretGunAccuracyTouch != StatDefOf.AccuracyTouch.defaultBaseValue || turretGunAccuracyShort != StatDefOf.AccuracyShort.defaultBaseValue ||
                    turretGunAccuracyMedium != StatDefOf.AccuracyMedium.defaultBaseValue || turretGunAccuracyLong != StatDefOf.AccuracyLong.defaultBaseValue)
                {
                    StatDrawEntry accTouchSDE = new StatDrawEntry(TE_StatCategoryDefOf.Turret, StatDefOf.AccuracyTouch.label, turretGunAccuracyTouch.ToStringPercent(), 14);
                    StatDrawEntry accShortSDE = new StatDrawEntry(TE_StatCategoryDefOf.Turret, StatDefOf.AccuracyShort.label, turretGunAccuracyShort.ToStringPercent(), 13);
                    StatDrawEntry accMediumSDE = new StatDrawEntry(TE_StatCategoryDefOf.Turret, StatDefOf.AccuracyMedium.label, turretGunAccuracyMedium.ToStringPercent(), 12);
                    StatDrawEntry accLongSDE = new StatDrawEntry(TE_StatCategoryDefOf.Turret, StatDefOf.AccuracyLong.label, turretGunAccuracyLong.ToStringPercent(), 11);
                    __result = __result.Add(accTouchSDE);
                    __result = __result.Add(accShortSDE);
                    __result = __result.Add(accMediumSDE);
                    __result = __result.Add(accLongSDE);
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
                string damage = "MortarShellDependent".Translate();
                if (turretGunProjectile != null)
                {
                    damage = turretGunProjectile.projectile.DamageAmount.ToString();
                }

                StatDrawEntry damageSDE = new StatDrawEntry(TE_StatCategoryDefOf.Turret, "Damage".Translate(), damage, 21);
                __result = __result.Add(damageSDE);

            }
            if (__instance.IsShell)
            {
                ProjectileProperties shellProps = __instance.projectileWhenLoaded.projectile;
                int shellDamage = shellProps.DamageAmount;
                string shellDamageDef = shellProps.damageDef.label.CapitalizeFirst();
                float shellExplosionRadius = shellProps.explosionRadius;

                StatDrawEntry damageSDE = new StatDrawEntry(TE_StatCategoryDefOf.TurretAmmo, "Damage".Translate(), shellDamage.ToString(), 20);
                StatDrawEntry damageDefSDE = new StatDrawEntry(TE_StatCategoryDefOf.TurretAmmo, "ShellDamageType".Translate(), shellDamageDef, 19);
                __result = __result.Add(damageSDE);
                __result = __result.Add(damageDefSDE);

                if (shellExplosionRadius > 0f)
                {
                    StatDrawEntry explosionRadSDE = new StatDrawEntry(TE_StatCategoryDefOf.TurretAmmo, "ShellExplosionRadius".Translate(), shellExplosionRadius.ToString(), 18);
                    __result = __result.Add(explosionRadSDE);
                }
            }
        }

    }
}

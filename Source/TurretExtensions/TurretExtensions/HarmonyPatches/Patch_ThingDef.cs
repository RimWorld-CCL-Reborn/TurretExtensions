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

    public static class Patch_ThingDef
    {

        public static class manual_SpecialDisplayStats
        {

            public static Type enumeratorType;

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                #if DEBUG
                    Log.Message("Transpiler start: ThingDef.manual_SpecialDisplayStats (1 match)");
                #endif

                // Runs inside MoveNext()
                var instructionList = instructions.ToList();
                var turretGunDefInfo = AccessTools.Field(typeof(BuildingProperties), nameof(BuildingProperties.turretGunDef));
                var reqInfo = AccessTools.Field(enumeratorType, "req");
                var actualTurretGunDefInfo = AccessTools.Method(typeof(manual_SpecialDisplayStats), nameof(manual_SpecialDisplayStats.ActualTurretGunDef));

                bool done = false;

                for (int i = 0; i < instructionList.Count; i++)
                {
                    var instruction = instructionList[i];

                    // Change the turretGunDef used for stat readouts based on whether or not the turret was upgraded
                    if (!done && instruction.opcode == OpCodes.Ldfld && (FieldInfo)instruction.operand == turretGunDefInfo)
                    {
                        #if DEBUG
                            Log.Message("ThingDef.manual_SpecialDisplayStats match 1 of 1");
                        #endif

                        yield return instruction; // thingDef.building.turretGunDef
                        yield return new CodeInstruction(OpCodes.Ldarg_0); // this
                        yield return new CodeInstruction(OpCodes.Ldfld, reqInfo); // this.req
                        instruction = new CodeInstruction(OpCodes.Call, actualTurretGunDefInfo); // ActualTurretGunDef(this.building.turretGunDef, this.req)
                        done = true;
                    }

                    yield return instruction;
                }
            }

            private static ThingDef ActualTurretGunDef(ThingDef original, StatRequest req)
            {
                return req.HasThing && req.Thing.IsUpgraded(out var upgradableComp) && upgradableComp.Props.turretGunDef != null ? upgradableComp.Props.turretGunDef : original;
            }

        }

        [HarmonyPatch(typeof(ThingDef), nameof(ThingDef.SpecialDisplayStats))]
        public static class SpecialDisplayStats
        {

            public static void Postfix(ThingDef __instance, StatRequest req, ref IEnumerable<StatDrawEntry> __result)
            {
                // Add mortar shell stats to the list of stat draw entries
                if (__instance.IsShell)
                {
                    Thing shellThing = ThingMaker.MakeThing(__instance.projectileWhenLoaded);
                    ProjectileProperties shellProps = __instance.projectileWhenLoaded.projectile;
                    int shellDamage = shellProps.GetDamageAmount(shellThing);
                    float shellArmorPenetration = shellProps.GetArmorPenetration(shellThing);
                    float shellStoppingPower = shellProps.StoppingPower;
                    string shellDamageDef = shellProps.damageDef.label.CapitalizeFirst();
                    float shellExplosionRadius = shellProps.explosionRadius;

                    __result = __result.AddItem(new StatDrawEntry(StatCategoryDefOf.TurretAmmo, "Damage".Translate(), shellDamage.ToString(), "Stat_Thing_Damage_Desc".Translate(), 20));
                    __result = __result.AddItem(new StatDrawEntry(StatCategoryDefOf.TurretAmmo, "TurretExtensions.ShellDamageType".Translate(), shellDamageDef, "TurretExtensions.ShellDamageType_Desc".Translate(), 19));
                    __result = __result.AddItem(new StatDrawEntry(StatCategoryDefOf.TurretAmmo, "ArmorPenetration".Translate(), shellArmorPenetration.ToStringPercent(), "ArmorPenetrationExplanation".Translate(), 18));
                    __result = __result.AddItem(new StatDrawEntry(StatCategoryDefOf.TurretAmmo, "StoppingPower".Translate(), shellStoppingPower.ToString(), "StoppingPowerExplanation".Translate(), 17));

                    if (shellExplosionRadius > 0)
                        __result = __result.AddItem(new StatDrawEntry(StatCategoryDefOf.TurretAmmo, "TurretExtensions.ShellExplosionRadius".Translate(), shellExplosionRadius.ToString(), "TurretExtensions.ShellExplosionRadius_Desc".Translate(), 16));
                }

                // Minimum range
                if (__instance.Verbs.FirstOrDefault(v => v.isPrimary) is VerbProperties verb)
                {
                    var verbStatCategory = (__instance.category != ThingCategory.Pawn) ? RimWorld.StatCategoryDefOf.Weapon : RimWorld.StatCategoryDefOf.PawnCombat;
                    if (verb.LaunchesProjectile)
                    {
                        if (verb.minRange > default(float))
                            __result = __result.AddItem(new StatDrawEntry(verbStatCategory, "MinimumRange".Translate(), verb.minRange.ToString("F0"), "TurretExtensions.MinimumRange_Desc".Translate(), 5385));
                    }
                }

                // Add turret weapons stats to the list of SpecialDisplayStats
                var buildingProps = __instance.building;
                if (buildingProps != null && buildingProps.IsTurret)
                {
                    var gunStatList = new List<StatDrawEntry>();
                    
                    if (req.Def is ThingDef tDef)
                    {
                        // Add upgradability
                        string upgradableString;
                        CompProperties_Upgradable upgradableCompProps;
                        if (req.HasThing && req.Thing.IsUpgradable(out CompUpgradable upgradableComp))
                        {
                            upgradableString = (upgradableComp.upgraded ? "TurretExtensions.NoAlreadyUpgraded" : "TurretExtensions.YesClickForDetails").Translate();
                            upgradableCompProps = upgradableComp.Props;
                        }
                        else
                            upgradableString = (tDef.IsUpgradable(out upgradableCompProps) ? "TurretExtensions.YesClickForDetails" : "No").Translate();

                        var upgradeHyperlinks = new List<Dialog_InfoCard.Hyperlink>();
                        if (upgradableCompProps.turretGunDef != null)
                            upgradeHyperlinks.Add(new Dialog_InfoCard.Hyperlink(upgradableCompProps.turretGunDef));

                        gunStatList.Add(new StatDrawEntry(RimWorld.StatCategoryDefOf.BasicsNonPawn, "TurretExtensions.Upgradable".Translate(), upgradableString,
                            TurretExtensionsUtility.UpgradeReadoutReportText(req), 999, hyperlinks: upgradeHyperlinks));

                        // Add firing arc
                        float firingArc = req.HasThing ? TurretExtensionsUtility.FiringArcFor(req.Thing) : TurretFrameworkExtension.Get(tDef).FiringArc;
                        gunStatList.Add(new StatDrawEntry(RimWorld.StatCategoryDefOf.Weapon, "TurretExtensions.FiringArc".Translate(), firingArc.ToStringDegrees(),
                            "TurretExtensions.FiringArc_Desc".Translate(), 5380));
                    }

                    // Replace cooldown
                    __result = __result.Where(s => s.stat != StatDefOf.RangedWeapon_Cooldown);
                    var cooldownStat = StatDefOf.RangedWeapon_Cooldown;
                    gunStatList.Add(new StatDrawEntry(cooldownStat.category, cooldownStat, TurretCooldown(req, buildingProps), StatRequest.ForEmpty(), cooldownStat.toStringNumberSense));

                    // Replace warmup
                    __result = __result.Where(s => s.LabelCap != "WarmupTime".Translate().CapitalizeFirst());
                    gunStatList.Add(new StatDrawEntry(RimWorld.StatCategoryDefOf.Weapon, "WarmupTime".Translate(), $"{TurretWarmup(req, buildingProps).ToString("0.##")} s",
                        "Stat_Thing_Weapon_MeleeWarmupTime_Desc".Translate(), StatDisplayOrder.Thing_Weapon_MeleeWarmupTime));

                    __result = __result.Concat(gunStatList);
                }

            }

            private static float TurretCooldown(StatRequest req, BuildingProperties buildingProps)
            {
                if (req.Thing is Building_TurretGun gunTurret)
                    return NonPublicMethods.Building_TurretGun_BurstCooldownTime(gunTurret);

                if (buildingProps.turretBurstCooldownTime > 0)
                    return buildingProps.turretBurstCooldownTime;

                return buildingProps.turretGunDef.GetStatValueAbstract(StatDefOf.RangedWeapon_Cooldown);
            }

            private static float TurretWarmup(StatRequest req, BuildingProperties buildingProps)
            {
                if (req.Thing != null && req.Thing.IsUpgraded(out CompUpgradable upgradableComp))
                    return buildingProps.turretBurstWarmupTime * upgradableComp.Props.turretBurstWarmupTimeFactor;
                else
                    return buildingProps.turretBurstWarmupTime;
            }

        }

    }

}

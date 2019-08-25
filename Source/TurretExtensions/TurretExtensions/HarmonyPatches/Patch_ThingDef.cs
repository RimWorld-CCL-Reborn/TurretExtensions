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

    public static class Patch_ThingDef
    {

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

                    __result = __result.Add(new StatDrawEntry(StatCategoryDefOf.TurretAmmo, "Damage".Translate(), shellDamage.ToString(), 20));
                    __result = __result.Add(new StatDrawEntry(StatCategoryDefOf.TurretAmmo, "ShellDamageType".Translate(), shellDamageDef, 19));
                    __result = __result.Add(new StatDrawEntry(StatCategoryDefOf.TurretAmmo, "ArmorPenetration".Translate(), shellArmorPenetration.ToStringPercent(), 18, "ArmorPenetrationExplanation".Translate()));
                    __result = __result.Add(new StatDrawEntry(StatCategoryDefOf.TurretAmmo, "StoppingPower".Translate(), shellStoppingPower.ToString(), 17, "StoppingPowerExplanation".Translate()));

                    if (shellExplosionRadius > 0f)
                        __result = __result.Add(new StatDrawEntry(StatCategoryDefOf.TurretAmmo, "ShellExplosionRadius".Translate(), shellExplosionRadius.ToString(), 16));
                }

                // Minimum range
                if (__instance.Verbs.FirstOrDefault(v => v.isPrimary) is VerbProperties verb)
                {
                    var verbStatCategory = (__instance.category != ThingCategory.Pawn) ? StatCategoryDefOf.Turret : RimWorld.StatCategoryDefOf.PawnCombat;
                    if (verb.LaunchesProjectile)
                    {
                        if (verb.minRange > default(float))
                            __result = __result.Add(new StatDrawEntry(verbStatCategory, "MinimumRange".Translate(), verb.minRange.ToString("F0"), 10, String.Empty));
                    }
                }

                // Add turret weapons stats to the list of SpecialDisplayStats
                var buildingProps = __instance.building;
                if (buildingProps != null && buildingProps.IsTurret)
                {
                    var gunStatList = new List<StatDrawEntry>();
                    if (req.HasThing)
                    {
                        var gun = ((Building_TurretGun)req.Thing).gun;
                        gunStatList.AddRange(gun.def.SpecialDisplayStats(StatRequest.For(gun)));
                        gunStatList.AddRange(NonPublicMethods.StatsReportUtility_StatsToDraw_thing(gun));
                    }
                    else
                    {
                        var defaultStuff = GenStuff.DefaultStuffFor(buildingProps.turretGunDef);
                        gunStatList.AddRange(buildingProps.turretGunDef.SpecialDisplayStats(StatRequest.For(buildingProps.turretGunDef, defaultStuff)));
                        gunStatList.AddRange(NonPublicMethods.StatsReportUtility_StatsToDraw_def_stuff(buildingProps.turretGunDef, defaultStuff));
                    }

                    // Replace gun warmup and cooldown with turret warmup and cooldown
                    var cooldownEntry = gunStatList.FirstOrDefault(s => s.stat == StatDefOf.RangedWeapon_Cooldown);
                    if (cooldownEntry != null)
                        cooldownEntry = new StatDrawEntry(cooldownEntry.category, cooldownEntry.LabelCap, TurretCooldown(req, buildingProps).ToStringByStyle(cooldownEntry.stat.toStringStyle),
                            cooldownEntry.DisplayPriorityWithinCategory, cooldownEntry.overrideReportText);
                    else
                    {
                        var cooldownStat = StatDefOf.RangedWeapon_Cooldown;
                        gunStatList.Add(new StatDrawEntry(cooldownStat.category, cooldownStat, buildingProps.turretBurstCooldownTime, StatRequest.ForEmpty(), cooldownStat.toStringNumberSense));
                    }

                    var warmupEntry = gunStatList.FirstOrDefault(s => s.LabelCap == "WarmupTime".Translate().CapitalizeFirst());
                    if (warmupEntry != null)
                        warmupEntry = new StatDrawEntry(warmupEntry.category, warmupEntry.LabelCap, $"{TurretWarmup(req, buildingProps).ToString("0.##")} s",
                            warmupEntry.DisplayPriorityWithinCategory, warmupEntry.overrideReportText);


                    // Remove entries that shouldn't be shown and change 'Weapon' categories to 'Turret' categories
                    for (int i = 0; i < gunStatList.Count; i++)
                    {
                        var curEntry = gunStatList[i];
                        if ((curEntry.stat != null && !curEntry.stat.showNonAbstract) || curEntry.category != RimWorld.StatCategoryDefOf.Weapon)
                        {
                            gunStatList.Remove(curEntry);
                            i--;
                        }
                        else
                            curEntry.category = StatCategoryDefOf.Turret;
                    }
                    __result = __result.Concat(gunStatList);
                }

            }

            private static float TurretCooldown(StatRequest req, BuildingProperties buildingProps)
            {
                if (req.Thing is Building_TurretGun gunTurret)
                {
                    if (gunTurret.IsUpgraded(out CompUpgradable upgradableComp))
                        return NonPublicMethods.Building_TurretGun_BurstCooldownTime(gunTurret) * upgradableComp.Props.turretBurstCooldownTimeFactor;
                    return NonPublicMethods.Building_TurretGun_BurstCooldownTime(gunTurret);
                }
                else if (buildingProps.turretBurstCooldownTime > 0)
                    return buildingProps.turretBurstCooldownTime;
                else
                    return buildingProps.turretGunDef.GetStatValueAbstract(StatDefOf.RangedWeapon_Cooldown);
            }

            private static float TurretWarmup(StatRequest req, BuildingProperties buildingProps)
            {
                if (req.Thing != null && req.Thing.IsUpgraded(out CompUpgradable upgradableComp))
                    return buildingProps.turretBurstWarmupTime * upgradableComp.Props.turretBurstCooldownTimeFactor;
                else
                    return buildingProps.turretBurstWarmupTime;
            }

        }

    }

}

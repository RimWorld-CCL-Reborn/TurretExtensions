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

        [HarmonyPatch(typeof(ThingDef))]
        [HarmonyPatch(nameof(ThingDef.SpecialDisplayStats))]
        public static class Patch_SpecialDisplayStats
        {

            public static void Postfix(ThingDef __instance, ref IEnumerable<StatDrawEntry> __result)
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

                    __result = __result.Add(new StatDrawEntry(TE_StatCategoryDefOf.TurretAmmo, "Damage".Translate(), shellDamage.ToString(), 20));
                    __result = __result.Add(new StatDrawEntry(TE_StatCategoryDefOf.TurretAmmo, "ShellDamageType".Translate(), shellDamageDef, 19));
                    __result = __result.Add(new StatDrawEntry(TE_StatCategoryDefOf.TurretAmmo, "ArmorPenetration".Translate(), shellArmorPenetration.ToStringPercent(), 18, "ArmorPenetrationExplanation".Translate()));
                    __result = __result.Add(new StatDrawEntry(TE_StatCategoryDefOf.TurretAmmo, "StoppingPower".Translate(), shellStoppingPower.ToString(), 17, "StoppingPowerExplanation".Translate()));

                    if (shellExplosionRadius > 0f)
                        __result = __result.Add(new StatDrawEntry(TE_StatCategoryDefOf.TurretAmmo, "ShellExplosionRadius".Translate(), shellExplosionRadius.ToString(), 16));
                }
            }

        }

    }

}

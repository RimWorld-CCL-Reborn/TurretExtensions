using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;

namespace TurretExtensions
{
    public static class TurretExtensionsUtility
    {

        public static bool IsUpgradable(this ThingDef def, out CompProperties_Upgradable upgradableCompProps)
        {
            upgradableCompProps = def.GetCompProperties<CompProperties_Upgradable>();
            return upgradableCompProps != null;
        }

        public static bool IsUpgradable(this Thing thing, out CompUpgradable upgradableComp)
        {
            upgradableComp = thing.TryGetComp<CompUpgradable>();
            return upgradableComp != null;
        }

        public static bool IsUpgraded(this Thing thing, out CompUpgradable upgradableComp)
        {
            bool upgradable = thing.IsUpgradable(out upgradableComp);
            return upgradable && upgradableComp.upgraded;
        }

    }
}

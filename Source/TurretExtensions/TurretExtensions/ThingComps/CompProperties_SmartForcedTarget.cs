using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;

namespace TurretExtensions
{
    public class CompProperties_SmartForcedTarget : CompProperties
    {

        public CompProperties_SmartForcedTarget()
        {
            compClass = typeof(CompSmartForcedTarget);
        }

        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (string e in base.ConfigErrors(parentDef))
                yield return e;

            if (!upgradesRequired.NullOrEmpty() && !parentDef.HasComp(typeof(CompUpgradable)))
            {
                var upgradableProps = parentDef.GetCompProperties<CompProperties_Upgradable>();
                if (upgradableProps == null)
                    yield return "has upgradesRequired but doesn't have CompUpgradable";
                else
            }
            yield break;
        }

        public List<UpgradeDef> upgradesRequired;

    }
}

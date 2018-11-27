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

            if (onlyApplyWhenUpgraded && !parentDef.HasComp(typeof(CompUpgradable)))
            {
                yield return "has onlyApplyWhenUpgraded set to true but doesn't have CompUpgradable";
                onlyApplyWhenUpgraded = false;
            }
            yield break;
        }

        public bool onlyApplyWhenUpgraded = false;

    }
}

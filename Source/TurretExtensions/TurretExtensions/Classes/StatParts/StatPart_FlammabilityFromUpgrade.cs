using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace TurretExtensions
{
    class StatPart_FlammabilityFromUpgrade : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            if (req.HasThing && req.Thing is Building_Turret turret)
            {
                CompUpgradable upgradableComp = turret.TryGetComp<CompUpgradable>();
                if (upgradableComp != null && upgradableComp.upgraded)
                {
                    val *= upgradableComp.Props.FlammabilityFactor;
                }
            }
        }

        public override string ExplanationPart(StatRequest req)
        {
            if (req.HasThing && req.Thing is Building_Turret turret)
            {
                CompUpgradable upgradableComp = turret.TryGetComp<CompUpgradable>();
                if (upgradableComp != null && upgradableComp.upgraded && upgradableComp.Props.FlammabilityFactor != 1f)
                {
                    return "TurretUpgradedText".Translate().CapitalizeFirst() + ": x" + upgradableComp.Props.FlammabilityFactor.ToStringPercent();
                }
            }
            return null;
        }
    }
}

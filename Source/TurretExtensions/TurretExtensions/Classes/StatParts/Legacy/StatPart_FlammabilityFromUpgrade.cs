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
                if (turret.IsUpgradedTurret(out CompUpgradable uC))
                {
                    val *= uC.Props.FlammabilityFactor;
                }
            }
        }

        public override string ExplanationPart(StatRequest req)
        {
            if (req.HasThing && req.Thing is Building_Turret turret)
            {
                if (turret.IsUpgradedTurret(out CompUpgradable uC) && uC.Props.FlammabilityFactor != 1f)
                {
                    return "TurretUpgradedText".Translate().CapitalizeFirst() + ": x" + uC.Props.FlammabilityFactor.ToStringPercent();
                }
            }
            return null;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace TurretExtensions
{
    class StatPart_HealthFromUpgrade : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            if (req.HasThing && req.Thing is Building_Turret turret && turret.IsUpgradedTurret(out CompUpgradable uC))
                val *= uC.Props.MaxHitPointsFactor;
        }

        public override string ExplanationPart(StatRequest req)
        {
            if (req.HasThing && req.Thing is Building_Turret turret && turret.IsUpgradedTurret(out CompUpgradable uC) && uC.Props.MaxHitPointsFactor != 1f)
            {
                return "TurretUpgradedText".Translate().CapitalizeFirst() + ": x" + uC.Props.MaxHitPointsFactor.ToStringPercent("0.##");
            }
            return null;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace TurretExtensions
{
    class StatPart_AccuracyFromUpgrade : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            if (req.HasThing && req.Thing is Building_TurretGun turret && turret.IsUpgradedTurret(out CompUpgradable uC))
            {
                val += uC.Props.ShootingAccuracyTurretOffset;
            }
        }

        public override string ExplanationPart(StatRequest req)
        {
            if (req.HasThing && req.Thing is Building_TurretGun turret && turret.IsUpgradedTurret(out CompUpgradable uC))
            {
                if (uC.Props.ShootingAccuracyTurretOffset > 0f)
                    return "TurretUpgradedText".Translate().CapitalizeFirst() + ": +" + uC.Props.ShootingAccuracyTurretOffset.ToStringPercent("0.##");
                if (uC.Props.ShootingAccuracyTurretOffset < 0f)
                    return "TurretUpgradedText".Translate().CapitalizeFirst() + ": " + uC.Props.ShootingAccuracyTurretOffset.ToStringPercent("0.##");
            }
            return null;
        }
    }
}

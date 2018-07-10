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
            if (req.HasThing && req.Thing is Building_TurretGun turret && turret.TryGetComp<CompUpgradable>() is CompUpgradable upgradableComp && upgradableComp.upgraded)
            {
                val += upgradableComp.Props.ShootingAccuracyTurretOffset;
            }
        }

        public override string ExplanationPart(StatRequest req)
        {
            if (req.HasThing && req.Thing is Building_TurretGun turret && turret.TryGetComp<CompUpgradable>() is CompUpgradable upgradableComp && upgradableComp.upgraded)
            {
                if (upgradableComp.Props.ShootingAccuracyTurretOffset > 0f)
                    return "TurretUpgradedText".Translate().CapitalizeFirst() + ": +" + upgradableComp.Props.ShootingAccuracyTurretOffset.ToStringPercent("0.##");
                else if (upgradableComp.Props.ShootingAccuracyTurretOffset < 0f)
                    return "TurretUpgradedText".Translate().CapitalizeFirst() + ": " + upgradableComp.Props.ShootingAccuracyTurretOffset.ToStringPercent("0.##");
            }
            return null;
        }
    }
}

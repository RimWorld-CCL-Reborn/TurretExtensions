using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace TurretExtensions
{
    public class StatPart_FromMannableTurret : StatPart
    {

        public override void TransformValue(StatRequest req, ref float val)
        {
            if (ShouldApply(req, out Building_Turret turret))
            {
                var extensionValues = TurretFrameworkExtension.Get(turret.def);
                val += turret.IsUpgraded(out var upgradableComp) ? upgradableComp.Props.manningPawnShootingAccuracyOffset : extensionValues.manningPawnShootingAccuracyOffset;
            }
        }

        public override string ExplanationPart(StatRequest req)
        {
            if (ShouldApply(req, out Building_Turret turret))
            {
                var extensionValues = TurretFrameworkExtension.Get(req.Def);
                float offset = turret.IsUpgraded(out var upgradableComp) ? upgradableComp.Props.manningPawnShootingAccuracyOffset : extensionValues.manningPawnShootingAccuracyOffset;

                if (offset != 0)
                    return $"{turret.def.LabelCap}: {offset.ToStringByStyle(parentStat.ToStringStyleUnfinalized, ToStringNumberSense.Offset)}";
            }
            return null;
        }

        private bool ShouldApply(StatRequest req, out Building_Turret turret)
        {
            turret = null;
            if (req.Thing is Pawn pawn)
                turret = pawn.MannedThing() as Building_Turret;

            return turret != null;
        }

    }
}

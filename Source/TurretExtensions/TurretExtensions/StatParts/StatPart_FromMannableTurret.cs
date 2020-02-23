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
                val += extensionValues.manningPawnShootingAccuracyOffset;
                if (turret.IsUpgraded(out CompUpgradable uC))
                    val += uC.Props.manningPawnShootingAccuracyOffsetBonus;
            }
        }

        public override string ExplanationPart(StatRequest req)
        {
            if (ShouldApply(req, out Building_Turret turret))
            {
                var extensionValues = turret.def.GetModExtension<TurretFrameworkExtension>();

                float totalOffset = 0f;
                if (extensionValues != null)
                    totalOffset += extensionValues.manningPawnShootingAccuracyOffset;
                if (turret.IsUpgraded(out CompUpgradable uC))
                    totalOffset += uC.Props.manningPawnShootingAccuracyOffsetBonus;

                return $"{turret.def.LabelCap}: {totalOffset.ToStringByStyle(parentStat.ToStringStyleUnfinalized, ToStringNumberSense.Offset)}";
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

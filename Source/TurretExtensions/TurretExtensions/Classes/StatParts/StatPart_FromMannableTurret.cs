using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace TurretExtensions
{
    class StatPart_FromMannableTurret : StatPart
    {

        public override void TransformValue(StatRequest req, ref float val)
        {
            if (req.HasThing && req.Thing is Pawn pawn && pawn.MannedThing() is Building_Turret turret)
            {
                var extensionValues = turret.def.GetModExtension<TurretFrameworkExtension>() ?? TurretFrameworkExtension.defaultValues;
                val += extensionValues.mannerShootingAccuracyOffset;
                if (turret.IsUpgradedTurret(out CompUpgradable uC))
                    val += uC.Props.mannerShootingAccuracyOffsetBonus + uC.Props.mannerShootingAccuracyOffsetOffset;
            }
        }

        public override string ExplanationPart(StatRequest req)
        {
            if (req.HasThing && req.Thing is Pawn pawn && pawn.MannedThing() is Building_Turret turret)
            {
                string explanationFirstPart = turret.def.label.CapitalizeFirst() + ": ";

                var extensionValues = turret.def.GetModExtension<TurretFrameworkExtension>();

                float mannerAccuracyOffset = 0f;
                if (extensionValues != null)
                    mannerAccuracyOffset += extensionValues.mannerShootingAccuracyOffset;
                if (turret.IsUpgradedTurret(out CompUpgradable uC))
                    mannerAccuracyOffset += uC.Props.mannerShootingAccuracyOffsetBonus + uC.Props.mannerShootingAccuracyOffsetOffset;

                if (mannerAccuracyOffset > 0f) return explanationFirstPart + "+" + mannerAccuracyOffset.ToString("F1");
                else if (mannerAccuracyOffset < 0f) return explanationFirstPart + mannerAccuracyOffset.ToString("F1");
            }
            return null;
        }

    }
}

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
                CompUpgradable upgradableComp = turret.TryGetComp<CompUpgradable>();
                if (upgradableComp != null && upgradableComp.upgraded) val += upgradableComp.Props.mannerShootingAccuracyOffsetOffset;
            }
        }

        public override string ExplanationPart(StatRequest req)
        {
            if (req.HasThing && req.Thing is Pawn pawn && pawn.MannedThing() is Building_Turret turret)
            {
                string explanationFirstPart = turret.def.label.CapitalizeFirst() + ": ";

                var extensionValues = turret.def.GetModExtension<TurretFrameworkExtension>();
                CompUpgradable upgradableComp = turret.GetComp<CompUpgradable>();

                float mannerAccuracyOffset = 0f;
                if (extensionValues != null) mannerAccuracyOffset += extensionValues.mannerShootingAccuracyOffset;
                if (upgradableComp != null && upgradableComp.upgraded) mannerAccuracyOffset += upgradableComp.Props.mannerShootingAccuracyOffsetOffset;

                if (mannerAccuracyOffset > 0f) return explanationFirstPart + "+" + mannerAccuracyOffset.ToString("0.#");
                else if (mannerAccuracyOffset < 0f) return explanationFirstPart + mannerAccuracyOffset.ToString("0.#");
            }
            return null;
        }

    }
}

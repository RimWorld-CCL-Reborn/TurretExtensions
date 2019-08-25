using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace TurretExtensions
{
    public class StatPart_AccuracyFromCompMannable : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            if (ShouldApply(req, out CompMannable mannableComp))
            {
                var manningPawn = mannableComp.ManningPawn;

                if (manningPawn == null)
                    val = 0;
                else
                    val = manningPawn.GetStatValue(correspondingStat);
            }
        }

        public override string ExplanationPart(StatRequest req)
        {
            if (ShouldApply(req, out CompMannable mannableComp))
            {
                var manningPawn = mannableComp.ManningPawn;

                // Not manned
                if (manningPawn == null)
                    return $"{"MannableTurretNotManned".Translate()}: {0f.ToStringByStyle(parentStat.toStringStyle, parentStat.toStringNumberSense)}";

                // Manning pawn
                return $"{manningPawn.LabelShortCap}: {manningPawn.GetStatValue(correspondingStat).ToStringByStyle(correspondingStat.toStringStyle, correspondingStat.toStringNumberSense)}";
            }
            return null;
        }

        private bool ShouldApply(StatRequest req, out CompMannable mannableComp)
        {
            mannableComp = null;
            if (req.Thing is Building_Turret turret && TurretFrameworkExtension.Get(turret.def).useMannerShootingAccuracy)
                mannableComp = turret.GetComp<CompMannable>();

            return mannableComp != null;
        }

        private StatDef correspondingStat;

    }
}

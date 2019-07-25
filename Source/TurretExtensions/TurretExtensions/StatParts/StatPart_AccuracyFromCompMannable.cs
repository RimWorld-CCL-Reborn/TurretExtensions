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
            if (req.HasThing && req.Thing is Building_Turret turret && turret.TryGetComp<CompMannable>() is CompMannable mannableComp &&
                (turret.def.GetModExtension<TurretFrameworkExtension>() ?? TurretFrameworkExtension.defaultValues).useMannerShootingAccuracy)
            {
                if (mannableComp.ManningPawn == null)
                    val = 0f;
                else
                    val = mannableComp.ManningPawn.GetStatValue(StatDefOf.ShootingAccuracyPawn);
            }
        }

        public override string ExplanationPart(StatRequest req)
        {
            if (req.HasThing && req.Thing is Building_Turret turret && turret.TryGetComp<CompMannable>() is CompMannable mannableComp &&
                (turret.def.GetModExtension<TurretFrameworkExtension>() ?? TurretFrameworkExtension.defaultValues).useMannerShootingAccuracy)
            {
                string explanationString = "MannableTurretAccuracyDepends".Translate() + "\n\n";
                if (mannableComp.ManningPawn == null)
                    explanationString += "MannableTurretNotManned".Translate() + ": 0%";
                else
                    explanationString += "MannableTurretIsManned".Translate() + ": " + mannableComp.ManningPawn.GetStatValue(StatDefOf.ShootingAccuracyPawn).ToStringPercent("0.##");
                return explanationString;
            }
            return null;
        }
    }
}

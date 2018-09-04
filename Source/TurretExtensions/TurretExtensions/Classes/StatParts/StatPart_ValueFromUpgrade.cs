using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace TurretExtensions
{
    class StatPart_ValueFromUpgrade : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            if (req.HasThing && req.Thing is Building_TurretGun turret && turret.IsUpgradableTurret(out CompUpgradable uC))
            {
                if (uC.upgradeCostListFinalized != null)
                    for (int i = 0; i < uC.innerContainer.Count; i++)
                    {
                        Thing thing = uC.innerContainer[i];
                        val += thing.MarketValue * thing.stackCount;
                    }
                val += Math.Min(uC.upgradeWorkDone, uC.upgradeWorkTotal) * StatWorker_MarketValue.ValuePerWork;
            }
        }

        public override string ExplanationPart(StatRequest req) => null;
    }
}

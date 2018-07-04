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
            if (req.HasThing && req.Thing is Building_TurretGun turret && turret.def.HasComp(typeof(CompUpgradable)))
            {
                CompUpgradable upgradableComp = turret.TryGetComp<CompUpgradable>();
                if (upgradableComp.upgradeCostListFinalized != null)
                {
                    for (int i = 0; i < upgradableComp.innerContainer.Count; i++)
                    {
                        Thing thing = upgradableComp.innerContainer[i];
                        val += thing.MarketValue * thing.stackCount;
                    }
                }
                val += Math.Min(upgradableComp.upgradeWorkDone, upgradableComp.upgradeWorkTotal) * StatWorker_MarketValue.ValuePerWork;
            }
        }

        public override string ExplanationPart(StatRequest req)
        {
            return null;
        }
    }
}

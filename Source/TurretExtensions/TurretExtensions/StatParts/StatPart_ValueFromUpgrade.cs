using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace TurretExtensions
{
    public class StatPart_ValueFromUpgrade : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            if (req.Thing?.GetInnerIfMinified() is Building_Turret turret && turret.IsUpgradable(out CompUpgradable uC))
            {
                if (!uC.finalCostList.NullOrEmpty())
                {
                    for (int i = 0; i < uC.innerContainer.Count; i++)
                    {
                        var thing = uC.innerContainer[i];
                        val += thing.MarketValue * thing.stackCount;
                    }
                }
                val += Math.Min(uC.upgradeWorkDone, uC.upgradeWorkTotal) * StatWorker_MarketValue.ValuePerWork;
            }
        }

        public override string ExplanationPart(StatRequest req) => null;
    }
}

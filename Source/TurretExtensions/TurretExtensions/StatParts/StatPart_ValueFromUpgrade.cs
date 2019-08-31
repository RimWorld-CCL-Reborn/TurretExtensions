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
                //Log.Message(uC.ToStringSafe());
                if (!uC.finalCostList.NullOrEmpty())
                {
                    foreach (Thing thing in uC.innerContainer)
                    {
                        val += thing.MarketValue * thing.stackCount;
                    }
                }
                val += Math.Min(uC.upgradeWorkDone, uC.upgradeWorkTotal) * StatWorker_MarketValue.ValuePerWork;
            }
        }

        public override string ExplanationPart(StatRequest req) => null;
    }
}

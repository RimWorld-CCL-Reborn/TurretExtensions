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
            int failPoint = 1;
            try
            {
                if (req.HasThing && req.Thing.GetInnerIfMinified() is Building_Turret turret && turret.IsUpgradable(out CompUpgradable uC))
                {
                    failPoint = 2;
                    if (!uC.finalCostList.NullOrEmpty())
                    {
                        failPoint = 3;
                        for (int i = 0; i < uC.innerContainer.Count; i++)
                        {
                            failPoint = 4;
                            var thing = uC.innerContainer[i];
                            failPoint = 5;
                            val += thing.MarketValue * thing.stackCount;
                        }
                    }
                    failPoint = 6;
                    val += Math.Min(uC.upgradeWorkDone, uC.upgradeWorkTotal) * StatWorker_MarketValue.ValuePerWork;
                }

            }
            catch (Exception ex)
            {
                Log.Message($"Exception in getting value from upgrade (failPoint={failPoint}): {ex.ToString()}");
            }
        }

        public override string ExplanationPart(StatRequest req) => null;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;

namespace TurretExtensions
{
    public static class TurretExtensionsUtility
    {

        public static bool IsUpgradable(this ThingDef def, out CompProperties_Upgradable upgradableCompProps)
        {
            upgradableCompProps = def.GetCompProperties<CompProperties_Upgradable>();
            return upgradableCompProps != null;
        }

        public static bool IsUpgradable(this Thing thing, out CompUpgradable upgradableComp)
        {
            upgradableComp = thing.TryGetComp<CompUpgradable>();
            return upgradableComp != null;
        }

        public static bool IsUpgraded(this Thing thing, out CompUpgradable upgradableComp)
        {
            bool upgradable = thing.IsUpgradable(out upgradableComp);
            return upgradable && upgradableComp.upgraded;
        }

        public static float AdjustedFuelCapacity(float baseFuelCapacity, Thing t)
        {
            if (t.IsUpgraded(out CompUpgradable upgradableComp))
                return baseFuelCapacity * upgradableComp.Props.fuelCapacityFactor;
            return baseFuelCapacity;
        }

        public static bool WithinFiringArcOf(this IntVec3 pos, Thing thing)
        {
            return WithinFiringArcOf(pos, thing.Position, thing.Rotation, FiringArcFor(thing));
        }

        public static bool WithinFiringArcOf(this IntVec3 pos, IntVec3 pos2, Rot4 rot, float firingArc)
        {
            return GenGeo.AngleDifferenceBetween(rot.AsAngle, (pos - pos2).AngleFlat) <= (firingArc / 2);
        }

        public static float FiringArcFor(Thing thing)
        {
            // Upgraded and defined firing arc
            if (thing.IsUpgraded(out CompUpgradable upgradableComp))
                return upgradableComp.Props.FiringArc;

            // Defined firing arc
            return TurretFrameworkExtension.Get(thing.def).FiringArc;
        }

        public static bool TryDrawFiringCone(Building_Turret turret, float distance)
        {
            return TryDrawFiringCone(turret.Position, turret.Rotation, distance, FiringArcFor(turret));
        }

        public static bool TryDrawFiringCone(IntVec3 centre, Rot4 rot, float distance, float arc)
        {
            if (arc < 360)
            {
                if (distance > GenRadial.MaxRadialPatternRadius)
                {
                    if (!(bool)NonPublicFields.GenDraw_maxRadiusMessaged.GetValue(null))
                    {
                        Log.Error("Cannot draw radius ring of radius " + distance + ": not enough squares in the precalculated list.", false);
                        NonPublicFields.GenDraw_maxRadiusMessaged.SetValue(null, true);
                    }
                    return false;
                }
                var ringDrawCells = (List<IntVec3>)NonPublicFields.GenDraw_ringDrawCells.GetValue(null);
                ringDrawCells.Clear();
                int num = GenRadial.NumCellsInRadius(distance);
                for (int i = 0; i < num; i++)
                {
                    var curCell = centre + GenRadial.RadialPattern[i];
                    if (curCell.WithinFiringArcOf(centre, rot, arc))
                        ringDrawCells.Add(curCell);
                }
                GenDraw.DrawFieldEdges(ringDrawCells);
                return true;
            }
            return false;
        }

        public static string UpgradeReadoutReportText(StatRequest req)
        {
            var tDef = (ThingDef)req.Def;
            var upgradeProps = tDef.GetCompProperties<CompProperties_Upgradable>();

            // First paragraph
            var reportBuilder = new StringBuilder();
            reportBuilder.AppendLine("TurretExtensions.TurretUpgradeBenefitsMain".Translate());

            // Upgradable
            if (upgradeProps != null)
            {
                var upgradeComp = req.Thing?.TryGetComp<CompUpgradable>();

                // Description
                reportBuilder.AppendLine();
                reportBuilder.AppendLine($"{"Description".Translate()}: {upgradeProps.description}");

                // Resource requirements
                if (upgradeProps.costStuffCount > 0 || !upgradeProps.costList.NullOrEmpty())
                {
                    reportBuilder.AppendLine();
                    reportBuilder.AppendLine($"{"TurretExtensions.TurretResourceRequirements".Translate()}:");

                    var usedCostList = upgradeComp != null ? upgradeComp.finalCostList : upgradeProps.costList;
                    for (int i = 0; i < usedCostList.Count; i++)
                    {
                        var curCost = usedCostList[i];
                        reportBuilder.AppendLine($"- {curCost.count}x {curCost.thingDef.LabelCap}");
                    }

                    if (!req.HasThing && upgradeProps.costStuffCount > 0)
                        reportBuilder.AppendLine($"- {upgradeProps.costStuffCount}x {"StatsReport_Material".Translate()}");
                }

                // Construction skill requirement
                if (upgradeProps.constructionSkillPrerequisite > 0)
                {
                    reportBuilder.AppendLine();
                    reportBuilder.AppendLine($"{"ConstructionNeeded".Translate()}: {upgradeProps.constructionSkillPrerequisite}");
                }

                // Research requirements
                if (!upgradeProps.researchPrerequisites.NullOrEmpty())
                {
                    reportBuilder.AppendLine();
                    reportBuilder.AppendLine($"{"ResearchPrerequisites".Translate()}:");

                    for (int i = 0; i < upgradeProps.researchPrerequisites.Count; i++)
                        reportBuilder.AppendLine($"- {upgradeProps.researchPrerequisites[i].LabelCap}");
                }

                // Upgrade bonuses
                reportBuilder.AppendLine();
                reportBuilder.AppendLine($"{"TurretExtensions.TurretUpgradeBenefitsUpgradable".Translate()}:");
            }

            // Not upgradable :(
            else
            {
                reportBuilder.AppendLine("TurretExtensions.TurretUpgradeBenefitsNotUpgradable".Translate());
            }

            // Return report text
            return reportBuilder.ToString();
        }

    }
}

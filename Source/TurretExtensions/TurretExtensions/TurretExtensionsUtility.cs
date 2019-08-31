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

        public static bool WithinFiringConeOf(this IntVec3 pos, Thing thing)
        {
            return WithinFiringConeOf(pos, thing, FiringAngleFor(thing));
        }

        public static bool WithinFiringConeOf(this IntVec3 pos, Thing thing, float firingAngle)
        {
            return GenGeo.AngleDifferenceBetween(thing.Rotation.AsAngle, (pos - thing.Position).AngleFlat) <= (firingAngle / 2);
        }

        public static float FiringAngleFor(Thing thing)
        {
            // Upgraded and defined firing angle
            if (thing.IsUpgraded(out CompUpgradable upgradableComp) && upgradableComp.Props.firingAngle > -1)
                return upgradableComp.Props.firingAngle;

            // Defined firing angle
            var extensionValues = TurretFrameworkExtension.Get(thing.def);
            if (extensionValues.firingAngle > -1)
                return extensionValues.firingAngle;

            // 360 degree default
            return 360;
        }

        public static bool TryDrawFiringCone(Building_Turret turret, float distance)
        {
            var extensionValues = TurretFrameworkExtension.Get(turret.def);
            if (extensionValues.firingAngle != -1 || (turret.IsUpgraded(out CompUpgradable upgradableComp) && upgradableComp.Props.firingAngle != -1))
            {
                float maxAngle = FiringAngleFor(turret);
                if (distance > GenRadial.MaxRadialPatternRadius)
                {
                    if (!(bool)NonPublicFields.GenDraw_maxRadiusMessaged.GetValue(null))
                    {
                        Log.Error("Cannot draw radius ring of radius " + distance + ": not enough squares in the precalculated list.", false);
                        NonPublicFields.GenDraw_maxRadiusMessaged.SetValue(null, true);
                    }
                    return false;
                }
                var centre = turret.Position;
                var ringDrawCells = (List<IntVec3>)NonPublicFields.GenDraw_ringDrawCells.GetValue(null);
                ringDrawCells.Clear();
                int num = GenRadial.NumCellsInRadius(distance);
                for (int i = 0; i < num; i++)
                {
                    var curCell = centre + GenRadial.RadialPattern[i];
                    if (curCell.WithinFiringConeOf(turret, maxAngle))
                        ringDrawCells.Add(curCell);
                }
                GenDraw.DrawFieldEdges(ringDrawCells);
                return true;
            }
            return false;
        }

        public static string UpgradeReadoutReportText(StatRequest req)
        {
            var reportBuilder = new StringBuilder();
            reportBuilder.AppendLine("TurretExtensions.TurretUpgradeBenefitsMain".Translate());

            return reportBuilder.ToString();
        }

    }
}

    using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using UnityEngine;
using Verse;
using RimWorld;

namespace TurretExtensions
{
    public class CompUpgradable : ThingComp, IThingHolder, IConstructible
    {

        public CompProperties_Upgradable Props => (CompProperties_Upgradable)props;

        public Graphic UpgradedGraphic
        {
            get
            {
                if (Props.graphicData != null)
                {
                    if (cachedUpgradedGraphic == null)
                        cachedUpgradedGraphic = Props.graphicData.GraphicColoredFor(parent);
                    return cachedUpgradedGraphic;
                }
                return null;
            }
        }

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);

            ResolveCostList();

            if (upgradeWorkTotal == -1)
                ResolveWorkToUpgrade();

            innerContainer = new ThingOwner<Thing>(this, false);
        }

        private void ResolveCostList()
        {
            if (parent.def.MadeFromStuff && Props.costStuffCount > 0)
                finalCostList.Add(new ThingDefCountClass(parent.Stuff, Props.costStuffCount));
            if (Props.costList != null)
            {
                foreach (ThingDefCountClass thing in Props.costList)
                {
                    var duplicate = finalCostList.FirstOrDefault(t => t.thingDef == thing.thingDef);
                    if (duplicate != null)
                        duplicate.count += thing.count;
                    else
                        finalCostList.Add(thing);
                }
            }
        }

        private void ResolveWorkToUpgrade()
        {
            float upgradeWorkOffset = (Props.upgradeWorkFactorStuff && parent.def.MadeFromStuff) ?
                    parent.Stuff.stuffProps.statOffsets.GetStatOffsetFromList(StatDefOf.WorkToBuild) : 0f;
            float upgradeWorkFactor = (Props.upgradeWorkFactorStuff && parent.def.MadeFromStuff) ?
                    parent.Stuff.stuffProps.statFactors.GetStatFactorFromList(StatDefOf.WorkToBuild) : 1f;

            upgradeWorkTotal = (Props.workToUpgrade + upgradeWorkOffset) * upgradeWorkFactor;
        }


        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);

            // If the turret wasn't minified, drop anything inside the innerContainer
            if (mode != DestroyMode.Vanish)
            {
                float resourceDropFraction = (mode == DestroyMode.KillFinalize) ? Props.destroyedResourceDropPct : Props.baseResourceDropPct;

                foreach (Thing thing in innerContainer)
                {
                    thing.stackCount = GenMath.RoundRandom(thing.stackCount * resourceDropFraction);
                    if (thing.stackCount == 0)
                        thing.Destroy();
                }

                innerContainer.TryDropAll(parent.Position, previousMap, ThingPlaceMode.Near);
            }
        }

        public override string CompInspectStringExtra()
        {
            if (!(ParentHolder is Map))
                return base.CompInspectStringExtra();

            // Not upgraded but designated to be upgraded
            if (!upgraded && parent.Map.designationManager.DesignationOn(parent, DesignationDefOf.UpgradeTurret) != null)
            {
                var inspectBuilder = new StringBuilder();

                // Resource costs
                if (!finalCostList.NullOrEmpty())
                {
                    inspectBuilder.AppendLine($"{"ContainedResources".Translate()}:");
                    foreach (var cost in finalCostList)
                    {
                        var costDef = cost.thingDef;
                        inspectBuilder.AppendLine($"{costDef.LabelCap}: {innerContainer.TotalStackCountOfDef(costDef)} / {cost.count}");
                    }
                }

                // Work left
                inspectBuilder.AppendLine($"{"WorkLeft".Translate()}: {(upgradeWorkTotal - upgradeWorkDone).ToStringWorkAmount()}");

                return inspectBuilder.ToString().TrimEndNewlines();
            }

            return null;
        }

        public bool SufficientMatsToUpgrade
        {
            get
            {
                foreach (var cost in finalCostList)
                {
                    int thingCount = innerContainer.TotalStackCountOfDef(cost.thingDef);
                    if (thingCount < cost.count)
                        return false;
                }
                return true;
            }
        }

        public void Cancel()
        {
            upgradeWorkDone = 0;
            innerContainer.TryDropAll(parent.Position, parent.Map, ThingPlaceMode.Near);
        }

        public void Upgrade()
        {
            upgraded = true;
            upgradeWorkDone = upgradeWorkTotal;

            // Set health to max health
            if (parent.def.useHitPoints)
                parent.HitPoints = parent.MaxHitPoints;

            // Update turret top
            if (parent is Building_TurretGun gunTurret && Props.turretGunDef != null)
            {
                gunTurret.gun.Destroy();
                gunTurret.gun = ThingMaker.MakeThing(Props.turretGunDef);
                NonPublicMethods.Building_TurretGun_UpdateGunVerbs(gunTurret);
            }

            // Update barrel durability
            if (parent.TryGetComp<CompRefuelable>() is CompRefuelable refuelableComp)
            {
                float newFuel = (float)NonPublicFields.CompRefuelable_fuel.GetValue(refuelableComp) * Props.fuelCapacityFactor;
                NonPublicFields.CompRefuelable_fuel.SetValue(refuelableComp, newFuel);
            }

            // Reset CompPowerTrader
            if (parent.TryGetComp<CompPowerTrader>() is CompPowerTrader powerComp)
                powerComp.SetUpPowerVars();

            // Force redraw
            parent.Map.mapDrawer.SectionAt(parent.Position).RegenerateAllLayers();
        }

        public override string TransformLabel(string label)
        {
            if (upgraded)
                return ($"{label} ({"TurretExtensions.TurretUpgradedText".Translate()})");
            return label;
        }

        public void GetChildHolders(List<IThingHolder> outChildren) => ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        public ThingOwner GetDirectlyHeldThings() => innerContainer;

        public List<ThingDefCountClass> MaterialsNeeded()
        {
            cachedMaterialsNeeded.Clear();

            // Determine needed materials
            foreach (var cost in finalCostList)
            {
                int resourceCount = innerContainer.TotalStackCountOfDef(cost.thingDef);
                int amountNeeded = cost.count - resourceCount;
                if (amountNeeded > 0)
                    cachedMaterialsNeeded.Add(new ThingDefCountClass(cost.thingDef, amountNeeded));
            }

            return cachedMaterialsNeeded;
        }
        public ThingDef UIStuff() => parent.Stuff;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
            Scribe_Values.Look(ref upgraded, "upgraded");
            Scribe_Values.Look(ref upgradeWorkDone, "upgradeWorkDone");
            Scribe_Values.Look(ref upgradeWorkTotal, "upgradeWorkTotal", -1);
        }

        public ThingOwner innerContainer;
        public bool upgraded;
        public float upgradeWorkDone;
        public float upgradeWorkTotal = -1;

        private Graphic cachedUpgradedGraphic;
        public List<ThingDefCountClass> finalCostList = new List<ThingDefCountClass>();
        private List<ThingDefCountClass> cachedMaterialsNeeded = new List<ThingDefCountClass>();

    }
}

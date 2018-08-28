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
    class CompUpgradable : ThingComp, IThingHolder
    {

        public List<ThingDefCountClass> upgradeCostListFinalized;

        public bool upgraded = false;
        public float upgradeWorkDone = 0f;
        public float upgradeWorkTotal = 0f;

        public ThingOwner innerContainer;

        public CompUpgradable()
        {
            upgradeCostListFinalized = new List<ThingDefCountClass>();
            innerContainer = new ThingOwner<Thing>(this, false);
        }

        public CompProperties_Upgradable Props => (CompProperties_Upgradable)props;

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            if (parent.def.MadeFromStuff && Props.costStuffCount > 0)
                upgradeCostListFinalized.Add(new ThingDefCountClass(parent.Stuff, Props.costStuffCount));
            if (Props.costList != null)
            {
                foreach (ThingDefCountClass thing in Props.costList)
                {
                    if (upgradeCostListFinalized.Count > 0 && thing.thingDef == upgradeCostListFinalized[0].thingDef)
                        upgradeCostListFinalized[0].count += thing.count;
                    else
                        upgradeCostListFinalized.Add(thing);
                }
            }
        }

        public override void CompTick()
        {
            // Fix for an exploit where cancelling = free upgrade. upgradeWorkTotal will only ever be -1 when an upgrade designation is cancelled
            if (upgradeWorkTotal == -1f)
            {
                upgraded = false;
                upgradeWorkDone = 0f;
                upgradeWorkTotal = 0f;
            }
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            // If the turret wasn't minified, drop its stuff
            if (mode != DestroyMode.Vanish)
            {
                float resourceDropFraction = Props.baseResourceDropPct;

                if (mode == DestroyMode.KillFinalize)
                    resourceDropFraction = Props.destroyedResourceDropPct;

                foreach (Thing thing in innerContainer)
                    thing.stackCount = GenMath.RoundRandom(thing.stackCount * resourceDropFraction);

                innerContainer.TryDropAll(parent.Position, previousMap, ThingPlaceMode.Near);
            }
        }

        public Dictionary<string, int> GetTurretUpgradeCost(List<ThingDefCountClass> upgradeCost)
        {
            Dictionary<string, int> costDict = new Dictionary<string, int>();
            foreach (ThingDefCountClass thing in upgradeCost)
                costDict.Add(thing.thingDef.defName, thing.count);
            return costDict;
        }

        public Dictionary<string, int> GetTurretHeldItems(ThingOwner turretContainer)
        {
            Dictionary<string, int> storedThingDict = new Dictionary<string, int>();
            //for (int i = 0; i < turretContainer.Count; i++)
            //{
            //    Thing currentThing = turretContainer[i];
            //    if (storedThingDict.ContainsKey(currentThing.def.defName))
            //        storedThingDict[currentThing.def.defName] += currentThing.stackCount;
            //    else
            //        storedThingDict.Add(currentThing.def.defName, currentThing.stackCount);
            //}
            foreach (Thing thing in turretContainer)
            {
                if (storedThingDict.ContainsKey(thing.def.defName))
                    storedThingDict[thing.def.defName] += thing.stackCount;
                else
                    storedThingDict.Add(thing.def.defName, thing.stackCount);
            }
            return storedThingDict;
        }

        public void ResolveWorkToUpgrade(bool godMode = false)
        {
            float upgradeWorkOffset = (Props.upgradeWorkFactorStuff && parent.def.MadeFromStuff) ?
                    parent.Stuff.stuffProps.statOffsets.GetStatOffsetFromList(StatDefOf.WorkToBuild) : 0f;
            float upgradeWorkFactor = (Props.upgradeWorkFactorStuff && parent.def.MadeFromStuff) ?
                    parent.Stuff.stuffProps.statFactors.GetStatFactorFromList(StatDefOf.WorkToBuild) : 1f;

            upgradeWorkTotal = (Props.workToUpgrade + upgradeWorkOffset) * upgradeWorkFactor;

            if (godMode)
                upgradeWorkDone = upgradeWorkTotal;
        }

        public void ResolveUpgrade()
        {
            upgraded = true;
            parent.HitPoints = Mathf.RoundToInt(parent.HitPoints * Props.MaxHitPointsFactor);
            if (parent is Building_TurretGun turretWGun && Props.turretGunDef != null)
            {
                turretWGun.gun = ThingMaker.MakeThing(Props.turretGunDef);
                AccessTools.Method(typeof(Building_TurretGun), "UpdateGunVerbs").Invoke(turretWGun, null);
            }
        }

        public override string CompInspectStringExtra()
        {
            if (ParentHolder != null && !(ParentHolder is Map))
                return base.CompInspectStringExtra();
            if (!upgraded)
            {
                string inspectString = "";
                if (upgradeCostListFinalized != null && parent.Map.designationManager.DesignationOn(parent, TE_DesignationDefOf.UpgradeTurret) != null)
                {
                    string contentsString = "CasketContains".Translate() + ": ";

                    Dictionary<string, int> upgradeCostDict = GetTurretUpgradeCost(upgradeCostListFinalized);
                    Dictionary<string, int> storedMatsDict = GetTurretHeldItems(GetDirectlyHeldThings());
                    List<string> requiredDefs = new List<string>(upgradeCostDict.Keys);

                    for (int i = 0, count = requiredDefs.Count; i < count; i++)
                    {
                        string content = "";
                        string thingDef = requiredDefs[i];
                        content += ((storedMatsDict.ContainsKey(thingDef)) ? storedMatsDict[thingDef].ToString() : "0") + " / " + upgradeCostDict[thingDef].ToString() +
                            " " + upgradeCostListFinalized[i].thingDef.label;
                        if (count - i > 1)
                            content += ", ";
                        contentsString += content;
                    }

                    inspectString += contentsString;
                }
                if (upgradeWorkTotal > 0f)
                {
                    if (inspectString != "") inspectString += "\n";
                    float upgradeWorkRemaining = (upgradeWorkTotal - upgradeWorkDone) / GenTicks.TicksPerRealSecond;
                    inspectString += "TurretUpgradeProgress".Translate() + ": " + upgradeWorkRemaining.ToString("0");
                }
                return inspectString;
            }
            return null;
        }

        public override string TransformLabel(string label)
        {
            return (upgraded) ? "TurretUpgradedText".Translate() + " " + label : label;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Deep.Look(ref innerContainer, "innerContainer", new object[] { this });
            Scribe_Values.Look(ref upgraded, "upgraded", false);
            Scribe_Values.Look(ref upgradeWorkDone, "upgradeWorkDone", 0f, true);
            Scribe_Values.Look(ref upgradeWorkTotal, "upgradeWorkTotal", CompProperties_Upgradable.defaultValues.workToUpgrade, true);
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return innerContainer;
        }

    }
}

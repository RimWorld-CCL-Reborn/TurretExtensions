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
    public class CompUpgradable : ThingComp, IThingHolder
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

        public Graphic UpgradedGraphic =>
            Props.graphicData?.GraphicColoredFor(parent);

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
            if (parent.def.useHitPoints)
                parent.HitPoints = parent.MaxHitPoints;
            if (parent is Building_TurretGun turretWGun)
            {
                ((TurretTop)(Traverse.Create(turretWGun).Field("top").GetValue())).DrawTurret();
                if (Props.turretGunDef != null)
                {
                    turretWGun.gun = ThingMaker.MakeThing(Props.turretGunDef);
                    AccessTools.Method(typeof(Building_TurretGun), "UpdateGunVerbs").Invoke(turretWGun, null);
                }
            }
            if (parent.TryGetComp<CompRefuelable>() is CompRefuelable refuelableComp)
            {
                Traverse fuel = Traverse.Create(refuelableComp).Field("fuel");
                fuel.SetValue((float)fuel.GetValue() * Props.barrelDurabilityFactor * Props.effectiveBarrelDurabilityFactor);
            }
            if (parent.TryGetComp<CompPowerTrader>() is CompPowerTrader powerComp)
                powerComp.SetUpPowerVars();
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
            return String.Empty;
        }

        public override string TransformLabel(string label) =>
            ((upgraded) ? "TurretUpgradedText".Translate() + " " : "") + label;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Deep.Look(ref innerContainer, "innerContainer", new[] { this });
            Scribe_Values.Look(ref upgraded, "upgraded", false);
            Scribe_Values.Look(ref upgradeWorkDone, "upgradeWorkDone", 0f, true);
            Scribe_Values.Look(ref upgradeWorkTotal, "upgradeWorkTotal", CompProperties_Upgradable.defaultValues.workToUpgrade, true);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                // For savegame compatibility with mods that add upgrades to existing turrets, since ctor doesn't iron this out for some reason
                if (innerContainer == null)
                    innerContainer = new ThingOwner<Thing>(this, false);
            }
        }

        public void GetChildHolders(List<IThingHolder> outChildren) =>
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());

        public ThingOwner GetDirectlyHeldThings() => innerContainer;

        public override string ToString() =>
            $"CompUpgradable for {parent.ToStringSafe()}...\n\n"+ 
            $"upgradeCostListFinalized - {upgradeCostListFinalized.ToStringSafe()}\n" +
            $"innerContainer - {innerContainer.ToStringSafe()}\n" +
            $"upgraded - {upgraded.ToStringSafe()}";


    }
}

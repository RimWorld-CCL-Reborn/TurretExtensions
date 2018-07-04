using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;

namespace TurretExtensions
{
    public class Designator_UpgradeTurret : Designator
    {

        private List<Building_TurretGun> designatedTurrets = new List<Building_TurretGun>();

        public Designator_UpgradeTurret()
        {
            defaultLabel = "DesignatorUpgradeTurret".Translate();
            defaultDesc = "DesignatorUpgradeTurretDesc".Translate();
            icon = ContentFinder<Texture2D>.Get("Designations/UpgradeTurret");
            soundDragSustain = SoundDefOf.Designate_DragStandard;
            soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
            useMouseIcon = true;
            soundSucceeded = SoundDefOf.Designate_Haul;
        }

        protected override DesignationDef Designation { get { return TE_DesignationDefOf.UpgradeTurret; } }

        public override int DraggableDimensions { get { return 2; } }

        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            if (!loc.InBounds(Map)) return false;
            else if (!DebugSettings.godMode && loc.Fogged(Map)) return false;
            return true;
        }

        public override AcceptanceReport CanDesignateThing(Thing t)
        {
            Building_TurretGun turret = t.GetInnerIfMinified() as Building_TurretGun;
            CompUpgradable upgradableComp = turret?.TryGetComp<CompUpgradable>();

            if (turret == null) return false;
            else if (upgradableComp == null) return false;
            else if (t.Faction != Faction.OfPlayer) return false;
            else if (upgradableComp.upgraded) return false;
            else if (Map.designationManager.DesignationOn(t, Designation) != null) return false;
            return true;
        }

        public override void DesignateSingleCell(IntVec3 c)
        {
            foreach (Thing t in UpgradableTurretsInSelection(c))
            {
                if (DebugSettings.godMode)
                {
                    t.TryGetComp<CompUpgradable>().upgraded = true;
                }
                else { DesignateThing(t); }
            }
        }

        public override void DesignateThing(Thing t)
        {
            if (DebugSettings.godMode)
            {
                CompUpgradable upgradableComp = t.TryGetComp<CompUpgradable>();
                upgradableComp.ResolveUpgrade();
                if (upgradableComp.upgradeCostListFinalized != null)
                {
                    foreach (ThingDefCountClass thing in upgradableComp.upgradeCostListFinalized)
                    {
                        for (int i = 0; i < thing.count; i++)
                        {
                            upgradableComp.innerContainer.TryAdd(ThingMaker.MakeThing(thing.thingDef));
                        }
                    }
                }
                upgradableComp.ResolveWorkToUpgrade(true);
            }
            else
            {
                Map.designationManager.AddDesignation(new Designation(t, Designation));
                designatedTurrets.Add((Building_TurretGun)t);
            }
        }

        protected override void FinalizeDesignationSucceeded()
        {

            foreach (Building_TurretGun turret in designatedTurrets)
            {
                NotifyPlayerOfInsufficientSkill(turret);
                NotifyPlayerOfInsufficientResearch(turret);
                List<ThingDefCountClass> upgradeCostList = turret.TryGetComp<CompUpgradable>().upgradeCostListFinalized;

                //if (upgradeCostList != null)
                //{
                //    string turretResourceCost = "";
                //    bool moreThanOneThing = false;
                //    foreach (ThingDefCountClass thing in upgradeCostList)
                //    {
                //        if (moreThanOneThing)
                //        {
                //            turretResourceCost += ", ";
                //        }
                //        turretResourceCost += thing.Summary;
                //        moreThanOneThing = true;
                //    }
                //    Messages.Message("TurretUpgradeCostMessage".Translate(turret.def.label) + ": " + turretResourceCost, MessageTypeDefOf.CautionInput, false);
                //}
            }
            designatedTurrets.Clear();
        }

        public override void SelectedUpdate() { GenUI.RenderMouseoverBracket(); }

        private void NotifyPlayerOfInsufficientSkill(Thing t)
        {
            bool meetsMinSkill = false;
            int minimumSkill = t.TryGetComp<CompUpgradable>().Props.constructionSkillPrerequisite;
            foreach (Pawn pawn in Find.CurrentMap.mapPawns.FreeColonists)
            {
                if (pawn.skills.GetSkill(SkillDefOf.Construction).Level >= minimumSkill)
                {
                    meetsMinSkill = true;
                    break;
                }
            }
            if (!meetsMinSkill)
            {
                Messages.Message("ConstructionSkillTooLowMessage".Translate(new object[] { Faction.OfPlayer.def.pawnsPlural, t.def.label }), MessageTypeDefOf.CautionInput, false);
            }
        }

        private void NotifyPlayerOfInsufficientResearch(Thing t)
        {
            bool researchRequirementsMet = true;
            List<ResearchProjectDef> researchRequirements = t.TryGetComp<CompUpgradable>().Props.researchPrerequisites;
            List<string> researchProjectsUnfinished = new List<string>();
            if (researchRequirements != null)
            {
                foreach (ResearchProjectDef research in researchRequirements)
                {
                    if (!research.IsFinished)
                    {
                        researchRequirementsMet = false;
                        researchProjectsUnfinished.Add(research.label);
                    }
                }
            }
            if (!researchRequirementsMet)
            {
                string messageText = "UpgradeResearchNotMetMessage".Translate(t.def.label) + ": " + GenText.ToCommaList(researchProjectsUnfinished).CapitalizeFirst();
                Messages.Message(messageText, MessageTypeDefOf.CautionInput, false);
            }
        }

        private IEnumerable<Building_TurretGun> UpgradableTurretsInSelection(IntVec3 c)
        {
            if (c.Fogged(Map)) { yield break; }
            List<Thing> thingList = c.GetThingList(Map);
            for (int i = 0; i < thingList.Count; i++)
            {
                if (CanDesignateThing(thingList[i]).Accepted)
                {
                    yield return (Building_TurretGun)thingList[i];
                }
            }
            yield break;
        }

    }
}

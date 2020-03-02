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

        private List<Building_Turret> designatedTurrets = new List<Building_Turret>();

        public Designator_UpgradeTurret()
        {
            defaultLabel = "TurretExtensions.DesignatorUpgradeTurret".Translate();
            defaultDesc = "TurretExtensions.DesignatorUpgradeTurretDesc".Translate();
            icon = ContentFinder<Texture2D>.Get("Designations/UpgradeTurret");
            soundDragSustain = SoundDefOf.Designate_DragStandard;
            soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
            useMouseIcon = true;
            soundSucceeded = SoundDefOf.Designate_Haul;
        }

        protected override DesignationDef Designation => DesignationDefOf.UpgradeTurret;

        public override int DraggableDimensions => 2;

        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            if (!loc.InBounds(Map))
                return false;
            if (!DebugSettings.godMode && loc.Fogged(Map))
                return false;
            if (!UpgradableTurretsInSelection(loc).Any())
                return "TurretExtensions.MessageMustDesignateUpgradableTurrets".Translate();
            return true;
        }

        public override AcceptanceReport CanDesignateThing(Thing t) =>
            (t is Building_Turret turret && turret.Faction == Faction.OfPlayer && turret.IsUpgradable(out CompUpgradable upgradableComp) && 
            !upgradableComp.upgraded && Map.designationManager.DesignationOn(t, Designation) == null);

        public override void DesignateSingleCell(IntVec3 c)
        {
            var upgradableTurrets = UpgradableTurretsInSelection(c).ToList();
            for (int i = 0; i < upgradableTurrets.Count; i++)
            {
                var turret = upgradableTurrets[i];
                if (DebugSettings.godMode)
                    turret.TryGetComp<CompUpgradable>().upgraded = true;
                else
                    DesignateThing(turret);;
            }
        }

        public override void DesignateThing(Thing t)
        {
            // Godmode
            if (DebugSettings.godMode)
            {
                CompUpgradable upgradableComp = t.TryGetComp<CompUpgradable>();
                upgradableComp.Upgrade();
                if (upgradableComp.finalCostList != null)
                {
                    for (int i = 0; i < upgradableComp.finalCostList.Count; i++)
                    {
                        var thing = upgradableComp.finalCostList[i];
                        var initThingCount = upgradableComp.innerContainer.TotalStackCountOfDef(thing.thingDef);
                        for (int j = initThingCount; j < thing.count; j++)
                            upgradableComp.innerContainer.TryAdd(ThingMaker.MakeThing(thing.thingDef));

                    }
                }
            }

            else
            {
                Map.designationManager.AddDesignation(new Designation(t, Designation));
                designatedTurrets.Add((Building_Turret)t);
            }
        }

        protected override void FinalizeDesignationSucceeded()
        {
            if (!DebugSettings.godMode)
            {
                for (int i = 0; i < designatedTurrets.Count; i++)
                {
                    var turret = designatedTurrets[i];
                    NotifyPlayerOfInsufficientSkill(turret);
                    NotifyPlayerOfInsufficientResearch(turret);
                }
            }
            designatedTurrets.Clear();
        }

        public override void SelectedUpdate() => GenUI.RenderMouseoverBracket(); 

        private void NotifyPlayerOfInsufficientSkill(Thing t)
        {
            bool meetsMinSkill = false;
            int minimumSkill = t.TryGetComp<CompUpgradable>().Props.constructionSkillPrerequisite;
            var freeColonists = Find.CurrentMap.mapPawns.FreeColonists;
            for (int i = 0; i < freeColonists.Count; i++)
            {
                var pawn = freeColonists[i];
                if (pawn.skills.GetSkill(SkillDefOf.Construction).Level >= minimumSkill)
                {
                    meetsMinSkill = true;
                    break;
                }
            }
            if (!meetsMinSkill)
            {
                Messages.Message("TurretExtensions.ConstructionSkillTooLowMessage".Translate(Faction.OfPlayer.def.pawnsPlural, t.def.label), MessageTypeDefOf.CautionInput, false);
            }
        }

        private void NotifyPlayerOfInsufficientResearch(Thing t)
        {
            bool researchRequirementsMet = true;
            List<ResearchProjectDef> researchRequirements = t.TryGetComp<CompUpgradable>().Props.researchPrerequisites;
            List<string> researchProjectsUnfinished = new List<string>();
            if (researchRequirements != null)
            {
                for (int i = 0; i < researchRequirements.Count; i++)
                {
                    var research = researchRequirements[i];
                    if (!research.IsFinished)
                    {
                        researchRequirementsMet = false;
                        researchProjectsUnfinished.Add(research.label);
                    }
                }
            }
            if (!researchRequirementsMet)
            {
                string messageText = "TurretExtensions.UpgradeResearchNotMetMessage".Translate(t.def.label) + ": " + GenText.ToCommaList(researchProjectsUnfinished).CapitalizeFirst();
                Messages.Message(messageText, MessageTypeDefOf.CautionInput, false);
            }
        }

        private IEnumerable<Building_Turret> UpgradableTurretsInSelection(IntVec3 c)
        {
            if (c.Fogged(Map))
                yield break;

            List<Thing> thingList = c.GetThingList(Map);
            for (int i = 0; i < thingList.Count; i++)
                if (CanDesignateThing(thingList[i]).Accepted)
                    yield return (Building_Turret)thingList[i];

            yield break;
        }

    }
}

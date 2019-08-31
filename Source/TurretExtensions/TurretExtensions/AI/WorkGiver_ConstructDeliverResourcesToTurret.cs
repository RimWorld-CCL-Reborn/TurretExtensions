using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;

namespace TurretExtensions
{

    public class WorkGiver_ConstructDeliverResourcesToTurrets : WorkGiver_ConstructDeliverResources
    {

        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial);

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            // Different factions
            if (t.Faction != pawn.Faction)
                return null;

            // Not upgradable
            var upgradableComp = t.TryGetComp<CompUpgradable>();
            if (upgradableComp == null)
                return null;

            // Not designated to be upgraded
            if (t.Map.designationManager.DesignationOn(t, DesignationDefOf.UpgradeTurret) == null)
                return null;

            // Blocked
            if (GenConstruct.FirstBlockingThing(t, pawn) != null)
                return GenConstruct.HandleBlockingThingJob(t, pawn, forced);

            // Construction skill
            bool checkSkill = def.workType == WorkTypeDefOf.Construction;
            if (checkSkill && pawn.skills.GetSkill(SkillDefOf.Construction).Level < upgradableComp.Props.constructionSkillPrerequisite)
                return null;

            return ResourceDeliverJobFor(pawn, upgradableComp, false);
        }

    }

}

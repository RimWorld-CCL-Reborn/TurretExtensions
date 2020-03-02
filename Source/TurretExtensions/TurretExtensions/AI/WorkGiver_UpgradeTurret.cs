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
    public class WorkGiver_UpgradeTurret : WorkGiver_Scanner
    {

        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override Danger MaxPathDanger(Pawn pawn)
        {
            return Danger.Deadly;
        }

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            var upgradeDesignations = pawn.Map.designationManager.SpawnedDesignationsOfDef(DesignationDefOf.UpgradeTurret).ToList();
            for (int i = 0; i < upgradeDesignations.Count; i++)
            {
                var des = upgradeDesignations[i];
                yield return des.target.Thing;
            }
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            // Different factions
            if (t.Faction != pawn.Faction)
                return false;

            // Building isn't a turret
            var turret = t as Building_Turret;
            if (turret == null)
                return false;

            // Not upgradable
            var upgradableComp = turret?.TryGetComp<CompUpgradable>();
            if (upgradableComp == null)
                return false;

            // Already upgraded
            if (upgradableComp.upgraded)
                return false;

            // Not sufficiently skilled
            if (pawn.skills.GetSkill(SkillDefOf.Construction).Level < upgradableComp.Props.constructionSkillPrerequisite)
                return false;

            // Not forced and there's a risk of destroying the turret
            if (!forced && pawn.GetStatValue(StatDefOf.ConstructSuccessChance) * upgradableComp.Props.upgradeSuccessChanceFactor < 1 && turret.HitPoints <= Mathf.Floor(turret.MaxHitPoints * (1 - upgradableComp.Props.upgradeFailMajorDmgPctRange.TrueMax)))
                return false;

            // Havent finished research requirements
            if (upgradableComp.Props.researchPrerequisites != null && upgradableComp.Props.researchPrerequisites.Any(r => !r.IsFinished))
                return false;

            // Not enough materials
            if (!upgradableComp.SufficientMatsToUpgrade)
                return false;

            // Final condition set - the only set that can return true
            return pawn.CanReserve(turret, 1, -1, null, forced) && !turret.IsBurning();
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return new Job(JobDefOf.UpgradeTurret, t);
        }

    }
}

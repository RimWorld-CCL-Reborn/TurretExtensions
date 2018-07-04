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
    class WorkGiver_UpgradeTurret : WorkGiver_Scanner
    {

        private ThingDefCountClass firstMissingIngredient = new ThingDefCountClass();

        protected DesignationDef Designation
        {
            get
            {
                return TE_DesignationDefOf.UpgradeTurret;
            }
        }

        public override PathEndMode PathEndMode
        {
            get
            {
                return PathEndMode.Touch;
            }
        }

        public override Danger MaxPathDanger(Pawn pawn)
        {
            return Danger.Some;
        }

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            foreach (Designation des in pawn.Map.designationManager.SpawnedDesignationsOfDef(Designation))
            {
                yield return des.target.Thing;
            }
            yield break;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            // Setting up variables
            Building_Turret turret = t as Building_Turret;
            CompUpgradable upgradableComp = turret?.TryGetComp<CompUpgradable>();
            float workerSuccessChance = pawn.GetStatValue(StatDefOf.ConstructSuccessChance);

            if (upgradableComp != null) Initialize(upgradableComp);

            // Conditions to return false
            if (turret == null) return false;
            else if (upgradableComp == null) return false;
            else if (turret.Faction != pawn.Faction) return false;
            else if (pawn.skills.GetSkill(SkillDefOf.Construction).Level < upgradableComp.Props.constructionSkillPrerequisite) return false;
            else if (!forced && workerSuccessChance * upgradableComp.Props.upgradeSuccessChanceFactor < 1f
                && turret.HitPoints <= Mathf.Floor(turret.MaxHitPoints * 0.5f)) return false;
            else if (upgradableComp.upgraded) return false;
            else if (upgradableComp.upgradeCostListFinalized != null && ClosestMissingIngredient(pawn) == null) return false;
            else if (upgradableComp.Props.researchPrerequisites != null)
            {
                foreach (ResearchProjectDef research in upgradableComp.Props.researchPrerequisites)
                {
                    if (!research.IsFinished)
                    {
                        return false;
                    }
                }
                return (pawn.CanReserve(turret, 1, -1, null, forced) && !turret.IsBurning());
            }

            else return (pawn.CanReserve(turret, 1, -1, null, forced) && !turret.IsBurning());
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            bool readyToUpgrade = CheckTurretIsReadyToUpgrade(t.TryGetComp<CompUpgradable>());
            if (readyToUpgrade) return new Job(TE_JobDefOf.UpgradeTurret, t);
            else
            {
                return new Job(JobDefOf.HaulToContainer, ClosestMissingIngredient(pawn), t)
                {
                    count = firstMissingIngredient.count,
                    haulMode = HaulMode.ToContainer
                };
            }
        }

        private void Initialize(CompUpgradable c)
        {
            // basically CheckTurretIsReadyToUpgrade but doesn't return anything

            List<ThingDefCountClass> upgradeCost = c.upgradeCostListFinalized;
            if (upgradeCost != null)
            {
                Dictionary<string, int> upgradeCostDict = c.GetTurretUpgradeCost(upgradeCost);
                Dictionary<string, int> storedMatsDict = c.GetTurretHeldItems(c.GetDirectlyHeldThings());
                List<string> requiredDefs = new List<string>(upgradeCostDict.Keys);

                int i = 0;

                while (i < requiredDefs.Count)
                {
                    string curDef = requiredDefs[i];
                    try
                    {
                        if (storedMatsDict[curDef] < upgradeCostDict[curDef])
                        {
                            UpdateFirstMissingIngredient(curDef, upgradeCostDict[curDef], storedMatsDict[curDef]);
                            break;
                        }
                        i++;
                    }
                    catch (KeyNotFoundException)
                    {
                        UpdateFirstMissingIngredient(curDef, upgradeCostDict[curDef]);
                        break;
                    }
                }
            }
        }

        private bool CheckTurretIsReadyToUpgrade(CompUpgradable c)
        {
            List<ThingDefCountClass> upgradeCost = c.upgradeCostListFinalized;

            // If there's no upgrade cost, no material hauling required, thus returning true
            if (upgradeCost == null) return true;

            // Compare stored materials to cost list. Done by converting both objects to common ground (i.e. dictionary), then comparing keys and values.
            else
            {
                bool readyToUpgrade = true;


                // Setting up dicts
                Dictionary<string, int> upgradeCostDict = c.GetTurretUpgradeCost(upgradeCost);
                Dictionary<string, int> storedMatsDict = c.GetTurretHeldItems(c.GetDirectlyHeldThings());

                // Comparing dicts
                List<string> requiredDefs = new List<string>(upgradeCostDict.Keys);
                int i = 0;

                while (i < requiredDefs.Count)
                {
                    string curDef = requiredDefs[i];
                    try
                    {
                        if (storedMatsDict[curDef] < upgradeCostDict[curDef])
                        {
                            UpdateFirstMissingIngredient(curDef, upgradeCostDict[curDef], storedMatsDict[curDef]);
                            readyToUpgrade = false;
                            break;
                        }
                        i++;
                    }
                    // If one of the required things aren't present in the turret
                    catch(KeyNotFoundException)
                    {
                        UpdateFirstMissingIngredient(curDef, upgradeCostDict[curDef]);
                        readyToUpgrade = false;
                        break;
                    }
                }

                return readyToUpgrade;
            }
            
        }

        private Thing ClosestMissingIngredient(Pawn pawn)
        {
            return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(firstMissingIngredient.thingDef), PathEndMode.InteractionCell,
                TraverseParms.For(pawn, pawn.NormalMaxDanger()), 9999f, (Thing x) => !x.IsForbidden(pawn) && pawn.CanReserve(x));
        }

        private void UpdateFirstMissingIngredient(string defName, int upgradeCost, int storedCount = -1)
        {
            firstMissingIngredient.thingDef = DefDatabase<ThingDef>.GetNamed(defName);
            if (storedCount != -1)
                firstMissingIngredient.count = Math.Min(upgradeCost - storedCount, firstMissingIngredient.thingDef.stackLimit);
            else
                firstMissingIngredient.count = Math.Min(upgradeCost, firstMissingIngredient.thingDef.stackLimit);
        }

    }
}

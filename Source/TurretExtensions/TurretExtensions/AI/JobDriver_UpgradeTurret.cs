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
    public class JobDriver_UpgradeTurret : JobDriver
    {

        // Upgrade work is stored in the comp

        private const TargetIndex TurretInd = TargetIndex.A;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, errorOnFailed: errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TurretInd);

            yield return Toils_Goto.GotoCell(TurretInd, PathEndMode.Touch);
            yield return Upgrade();
            yield break;
        }

        private Toil Upgrade()
        {
            Toil upgrade = new Toil();
            upgrade.initAction = delegate
            {
                UpgradableComp.ResolveWorkToUpgrade();
            };
            upgrade.tickAction = delegate
            {
                Pawn actor = upgrade.actor;
                actor.skills.Learn(SkillDefOf.Construction, SkillTuning.XpPerTickConstruction);
                float constructionSpeed = actor.GetStatValue(StatDefOf.ConstructionSpeed);
                if (TargetThingA.def.MadeFromStuff) constructionSpeed *= TargetThingA.Stuff.GetStatValueAbstract(StatDefOf.ConstructionSpeedFactor);
                float successChance = actor.GetStatValue(StatDefOf.ConstructSuccessChance) * UpgradableComp.Props.upgradeSuccessChanceFactor;
                if (Rand.Value < 1f - Mathf.Pow(successChance, constructionSpeed / UpgradableComp.upgradeWorkTotal) && UpgradableComp.Props.upgradeFailable)
                {
                    UpgradableComp.upgradeWorkDone = 0f;
                    FailUpgrade(actor, successChance, TargetThingA);
                    ReadyForNextToil();
                }
                UpgradableComp.upgradeWorkDone += constructionSpeed;
                if (UpgradableComp.upgradeWorkDone >= UpgradableComp.upgradeWorkTotal)
                {
                    UpgradableComp.Upgrade();
                    Map.designationManager.TryRemoveDesignationOn(TargetThingA, TE_DesignationDefOf.UpgradeTurret);
                    actor.records.Increment(TE_RecordDefOf.TurretsUpgraded);
                    actor.jobs.EndCurrentJob(JobCondition.Succeeded);
                }
            };
            upgrade.FailOnCannotTouch(TurretInd, PathEndMode.Touch);
            upgrade.WithEffect(TargetThingA.def.repairEffect, TurretInd);
            upgrade.WithProgressBar(TurretInd, () => UpgradableComp.upgradeWorkDone / UpgradableComp.upgradeWorkTotal);
            upgrade.defaultCompleteMode = ToilCompleteMode.Never;
            upgrade.activeSkill = (() => SkillDefOf.Construction);
            return upgrade;
        }

        private void FailUpgrade(Pawn worker, float successChance, Thing building)
        {
            MoteMaker.ThrowText(building.DrawPos, building.Map, "TextMote_UpgradeFail".Translate(), 6f);
            string upgradeFailMessage = "UpgradeFailMinorMessage".Translate(worker.LabelShort, building.Label);
            float resourceRefund = UpgradableComp.Props.upgradeFailMinorResourcesRecovered;

            // Critical failure (2x construct fail chance by default)
            if (Rand.Value < (1f - successChance) * UpgradableComp.Props.upgradeFailMajorChanceFactor || UpgradableComp.Props.upgradeFailAlwaysMajor)
            {
                upgradeFailMessage = "UpgradeFailMajorMessage".Translate(worker.LabelShort, building.Label);
                resourceRefund = UpgradableComp.Props.upgradeFailMajorResourcesRecovered;

                int maxHealth = building.MaxHitPoints;
                float minDmgPct = UpgradableComp.Props.upgradeFailMajorDmgPctMin;
                float maxDmgPct = UpgradableComp.Props.upgradeFailMajorDmgPctMax;

                // Legacy support is a pain
                int damageAmount = Mathf.RoundToInt((minDmgPct != 0.1f || maxDmgPct != 0.5f) ? Rand.Range(maxHealth * minDmgPct, maxHealth * maxDmgPct) :
                    maxHealth * UpgradableComp.Props.upgradeFailMajorDmgPctRange.RandomInRange);
                building.TakeDamage(new DamageInfo(DamageDefOf.Blunt, damageAmount));
            }

            upgradeFailMessage += ResolveResourceLossMessage(resourceRefund);

            RefundResources(resourceRefund);
            UpgradableComp.innerContainer.Clear();
            Messages.Message(upgradeFailMessage, new TargetInfo(building.Position, building.Map), MessageTypeDefOf.NegativeEvent);
        }

        private string ResolveResourceLossMessage(float yield)
        {
            string resourceLossMessage = "";
            if (yield < 1f)
            {
                resourceLossMessage += " ";
                if (yield >= 0.8f)
                    resourceLossMessage += "UpgradeFailResourceLossSmall".Translate();
                else if (yield >= 0.35f)
                    resourceLossMessage += "UpgradeFailResourceLossMedium".Translate();
                else if (yield > 0f)
                    resourceLossMessage += "UpgradeFailResourceLossHigh".Translate();
                else
                    resourceLossMessage += "UpgradeFailResourceLossTotal".Translate();
            }
            return resourceLossMessage;
        }

        private void RefundResources(float yield)
        {
            List<ThingDefCountClass> ingredientCount = UpgradableComp.upgradeCostListFinalized;
            foreach (ThingDefCountClass thing in ingredientCount)
            {
                int yieldCount = GenMath.RoundRandom(thing.count * yield);
                Thing yieldItem = ThingMaker.MakeThing(thing.thingDef);
                yieldItem.stackCount = yieldCount;
                if (yieldCount > 0) { GenPlace.TryPlaceThing(yieldItem, TargetThingA.Position, TargetThingA.Map, ThingPlaceMode.Near); }
            }
        }

        private CompUpgradable UpgradableComp
        {
            get
            {
                return TargetThingA.TryGetComp<CompUpgradable>();
            }
        }

    }
}

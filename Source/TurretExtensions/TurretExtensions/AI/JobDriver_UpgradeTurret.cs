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

            yield return Toils_Goto.GotoThing(TurretInd, PathEndMode.Touch);
            yield return Upgrade();
            yield break;
        }

        private Toil Upgrade()
        {
            Toil upgrade = new Toil();
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
                    Map.designationManager.TryRemoveDesignationOn(TargetThingA, DesignationDefOf.UpgradeTurret);
                    actor.records.Increment(RecordDefOf.TurretsUpgraded);
                    actor.jobs.EndCurrentJob(JobCondition.Succeeded);
                }
            };
            upgrade.FailOnThingMissingDesignation(TurretInd, DesignationDefOf.UpgradeTurret);
            upgrade.FailOnCannotTouch(TurretInd, PathEndMode.Touch);
            upgrade.WithEffect(UpgradableComp.Props.UpgradeEffect((Building)job.GetTarget(TurretInd).Thing), TurretInd);
            upgrade.WithProgressBar(TurretInd, () => UpgradableComp.upgradeWorkDone / UpgradableComp.upgradeWorkTotal);
            upgrade.defaultCompleteMode = ToilCompleteMode.Never;
            upgrade.activeSkill = (() => SkillDefOf.Construction);
            return upgrade;
        }

        private void FailUpgrade(Pawn worker, float successChance, Thing building)
        {
            MoteMaker.ThrowText(building.DrawPos, building.Map, "TurretExtensions.TextMote_UpgradeFail".Translate(), 6f);
            string upgradeFailMessage = "TurretExtensions.UpgradeFailMinorMessage".Translate(worker.LabelShort, building.Label);
            float resourceRefund = UpgradableComp.Props.upgradeFailMinorResourcesRecovered;

            // Critical failure (2x construct fail chance by default)
            if (UpgradableComp.Props.upgradeFailAlwaysMajor || Rand.Value < (1f - successChance) * UpgradableComp.Props.upgradeFailMajorChanceFactor)
            {
                upgradeFailMessage = "TurretExtensions.UpgradeFailMajorMessage".Translate(worker.LabelShort, building.Label);
                resourceRefund = UpgradableComp.Props.upgradeFailMajorResourcesRecovered;

                float damageAmount = building.MaxHitPoints * UpgradableComp.Props.upgradeFailMajorDmgPctRange.RandomInRange;
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
                    resourceLossMessage += "TurretExtensions.UpgradeFailResourceLossSmall".Translate();
                else if (yield >= 0.35f)
                    resourceLossMessage += "TurretExtensions.UpgradeFailResourceLossMedium".Translate();
                else if (yield > 0f)
                    resourceLossMessage += "TurretExtensions.UpgradeFailResourceLossHigh".Translate();
                else
                    resourceLossMessage += "TurretExtensions.UpgradeFailResourceLossTotal".Translate();
            }
            return resourceLossMessage;
        }

        private void RefundResources(float yield)
        {
            UpgradableComp.innerContainer.TryDropAll(TargetThingA.Position, TargetThingA.Map, ThingPlaceMode.Near, (t, c) =>
            {
                c = GenMath.RoundRandom(c * yield);
                if (c > 0)
                    t.stackCount = c;
                else
                    t.Destroy();
            });        }

        private CompUpgradable UpgradableComp
        {
            get
            {
                return TargetThingA.TryGetComp<CompUpgradable>();
            }
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;

namespace TurretExtensions
{
    class CompProperties_Upgradable : CompProperties
    {

        public CompProperties_Upgradable()
        {
            compClass = typeof(CompUpgradable);
        }

        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            if (constructionSkillPrerequisite < 0 || constructionSkillPrerequisite > 20)
            {
                yield return "Construction skill prerequisite must be between 0 and 20. Resetting to 0...";
                constructionSkillPrerequisite = 0;
            }
        }

        public static readonly CompProperties_Upgradable defaultValues = new CompProperties_Upgradable();

        // Costs

        public int costStuffCount = 0;

        public List<ThingDefCountClass> costList;

        public List<ResearchProjectDef> researchPrerequisites;

        public int workToUpgrade = 1;

        public int constructionSkillPrerequisite = 0;

        // Job Driver

        public bool upgradeWorkFactorStuff = true;

        public bool upgradeFailable = true;

        public float upgradeSuccessChanceFactor = 1f;

        public float upgradeFailMinorResourcesRecovered = 0.5f;

        public float upgradeFailMajorResourcesRecovered = 0f;

        public bool upgradeFailAlwaysMajor = false;

        public float upgradeFailMajorChanceFactor = 2f;

        public float upgradeFailMajorDmgPctMin = 0.1f;

        public float upgradeFailMajorDmgPctMax = 0.5f;

        // Results

        public float MaxHitPointsFactor = 1f;

        public float FlammabilityFactor = 1f;

        public float ShootingAccuracyTurretOffset = 0f;

        public float basePowerConsumptionFactor = 1f;

        public ThingDef turretGunDef;

        public float turretBurstWarmupTimeFactor = 1f;

        public float turretBurstCooldownTimeFactor = 1f;

        public float mannerShootingAccuracyOffsetOffset = 0f;

        public float effectiveBarrelDurabilityFactor = 1f;

        public bool canForceAttack = false;

        // Destroyed

        public float baseResourceDropPct = 0.75f;

        public float destroyedResourceDropPct = 0.25f;

    }
}

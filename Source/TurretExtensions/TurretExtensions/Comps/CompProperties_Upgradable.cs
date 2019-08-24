using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;

namespace TurretExtensions
{
    public class CompProperties_Upgradable : CompProperties
    {

        public CompProperties_Upgradable()
        {
            compClass = typeof(CompUpgradable);
        }

        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (string e in base.ConfigErrors(parentDef))
                yield return e;

            if (upgradeFailMajorDmgPctMin > upgradeFailMajorDmgPctMax)
            {
                yield return "upgradeFailMajorDmgPctMin is greater than upgradeFailMajorDmgPctMax. Resetting to defaults...";
                upgradeFailMajorDmgPctMin = defaultValues.upgradeFailMajorDmgPctMin;
                upgradeFailMajorDmgPctMax = defaultValues.upgradeFailMajorDmgPctMax;
            }
            if (!parentDef.MadeFromStuff && costStuffCount > 0)
            {
                yield return "costStuffCount is greater than 0 but isn't stuffed";
                costStuffCount = 0;
            }
            if (constructionSkillPrerequisite < 0 || constructionSkillPrerequisite > 20)
            {
                yield return "constructionSkillPrerequisite must be between 0 and 20. Resetting to 0...";
                constructionSkillPrerequisite = 0;
            }
        }

        public static readonly CompProperties_Upgradable defaultValues = new CompProperties_Upgradable();

        // Basics

        public string description;

        public string upgradedTurretDescription;

        // Costs

        public int costStuffCount = 0;

        public List<ThingDefCountClass> costList;

        public List<ResearchProjectDef> researchPrerequisites;

        public int workToUpgrade = 1;

        public int constructionSkillPrerequisite = 0;

        // Job Driver

        #region jobdriver_legacy
        public float upgradeFailMajorDmgPctMin = 0.1f;
        public float upgradeFailMajorDmgPctMax = 0.5f;
        #endregion

        public bool upgradeWorkFactorStuff = true;

        public bool upgradeFailable = true;

        public float upgradeSuccessChanceFactor = 1f;

        public float upgradeFailMinorResourcesRecovered = 0.5f;

        public float upgradeFailMajorResourcesRecovered = 0f;

        public bool upgradeFailAlwaysMajor = false;

        public FloatRange upgradeFailMajorDmgPctRange = new FloatRange(0.1f, 0.5f);

        public float upgradeFailMajorChanceFactor = 2f;

        // Results

        #region results_legacy
        public float MaxHitPointsFactor = 1f;
        public float FlammabilityFactor = 1f;
        public float ShootingAccuracyTurretOffset = 0f;
        public float effectiveBarrelDurabilityFactor = 1f;
        public float mannerShootingAccuracyOffsetOffset = 0f;
        #endregion

        public List<StatModifier> statOffsets;

        public List<StatModifier> statFactors;

        public GraphicData graphicData;

        public string turretTopGraphicPath;

        public float turretTopDrawSize = 2f;

        public Vector2 turretTopOffset;

        public float barrelDurabilityFactor = 1f;

        public float basePowerConsumptionFactor = 1f;

        public float turretBurstWarmupTimeFactor = 1f;

        public float turretBurstCooldownTimeFactor = 1f;

        public ThingDef turretGunDef;

        public float mannerShootingAccuracyOffsetBonus = 0f;

        public bool canForceAttack = false;

        // Destroyed

        public float baseResourceDropPct = 0.75f;

        public float destroyedResourceDropPct = 0.25f;

    }
}

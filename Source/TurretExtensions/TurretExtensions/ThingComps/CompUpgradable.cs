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

        #region Fields
        public Dictionary<UpgradeDef, bool> upgradeStatusDict = new Dictionary<UpgradeDef, bool>();
        public List<UpgradeDef> upgradeQueue = new List<UpgradeDef>();
        #endregion

        #region Properties
        public CompProperties_Upgradable Props => (CompProperties_Upgradable)props;

        public UpgradeDef FirstInQueue => upgradeQueue.First();
        #endregion

        #region Methods
        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Collections.Look(ref upgradeStatusDict, "upgradeStatusDict", LookMode.Def, LookMode.Value);
            Scribe_Collections.Look(ref upgradeQueue, "upgradeQueue", LookMode.Def);
        }
        #endregion

    }
}

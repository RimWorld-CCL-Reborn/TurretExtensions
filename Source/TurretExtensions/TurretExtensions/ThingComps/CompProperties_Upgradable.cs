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

        public List<UpgradeDef> upgrades;

    }
}

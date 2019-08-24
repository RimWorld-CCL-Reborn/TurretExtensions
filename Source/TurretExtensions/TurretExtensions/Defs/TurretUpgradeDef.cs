using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;

namespace TurretExtensions
{

    public class UpgradeDef : Def
    {

        public List<UpgradeDef> upgradesRequired;
        public List<ResearchProjectDef> researchPrerequisites;
        public int costStuffCount;
        public List<ThingDefCountClass> costList;
        public List<StatModifier> statFactors;
        public List<StatModifier> statOffsets;
        

    }

}

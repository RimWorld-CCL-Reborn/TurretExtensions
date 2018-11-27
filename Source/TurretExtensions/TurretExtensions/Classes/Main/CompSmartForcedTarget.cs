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
    public class CompSmartForcedTarget : ThingComp
    {

        public CompProperties_SmartForcedTarget Props => (CompProperties_SmartForcedTarget)props;

        public bool attackingNonDownedPawn = false;

    }
}

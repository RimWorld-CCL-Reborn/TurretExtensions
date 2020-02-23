using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using UnityEngine;
using Verse;
using RimWorld;

namespace TurretExtensions
{
    public class CompSmartForcedTarget : ThingComp
    {

        public CompProperties_SmartForcedTarget Props => (CompProperties_SmartForcedTarget)props;

        public bool attackingNonDownedPawn;

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref attackingNonDownedPawn, "attackingNonDownedPawn");
            base.PostExposeData();
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;

namespace TurretExtensions
{
    class CompUpgradable : ThingComp
    {

        public CompProperties_Upgradable Props
        {
            get
            {
                return (CompProperties_Upgradable)props;
            }
        }

        public void HelloWorld()
        {
            if (doHelloWorld)
            {
                Log.Message("Hello world!");
            }
        }

        public bool doHelloWorld = false;

    }
}

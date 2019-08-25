using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;
using Harmony;

namespace TurretExtensions
{
    public class TurretExtensions : Mod
    {
        public TurretExtensions(ModContentPack content) : base(content)
        {
            harmonyInstance = HarmonyInstance.Create("XeoNovaDan.TurretExtensions");
        }

        public static HarmonyInstance harmonyInstance;

    }
}

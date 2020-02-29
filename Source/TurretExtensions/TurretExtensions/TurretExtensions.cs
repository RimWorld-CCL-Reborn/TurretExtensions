using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;
using HarmonyLib;

namespace TurretExtensions
{

    public class TurretExtensions : Mod
    {

        public TurretExtensions(ModContentPack content) : base(content)
        {
            harmonyInstance = new Harmony("XeoNovaDan.TurretExtensions");
        }

        public static Harmony harmonyInstance;

    }
}

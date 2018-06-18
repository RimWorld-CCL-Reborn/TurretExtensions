using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using RimWorld;
using Verse;
using Harmony;
using UnityEngine;

namespace TurretExtensions
{
    [DefOf]
    public static class TE_StatCategoryDefOf
    {
        public static StatCategoryDef Turret;

        public static StatCategoryDef TurretAmmo;
    }
}

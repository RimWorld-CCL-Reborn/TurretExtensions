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

namespace ExtendedTurretFramework
{
    [DefOf]
    public static class ETF_StatCategoryDefOf
    {
        public static StatCategoryDef Turret;

        public static StatCategoryDef TurretAmmo;
    }
}

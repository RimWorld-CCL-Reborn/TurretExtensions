using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection.Emit;
using RimWorld;
using Verse;
using HarmonyLib;
using UnityEngine;

namespace TurretExtensions
{

    public class TurretFrameworkExtension : DefModExtension
    {

        private static readonly TurretFrameworkExtension DefaultValues = new TurretFrameworkExtension();

        public static TurretFrameworkExtension Get(Def def) => def.GetModExtension<TurretFrameworkExtension>() ?? DefaultValues;

        public TurretGunFaceDirection gunFaceDirectionOnSpawn;
        private float firingArc = 360;
        public bool useManningPawnShootingAccuracy = true;
        public bool useManningPawnAimingDelayFactor = true;
        public float manningPawnShootingAccuracyOffset;
        public bool canForceAttack;
        public bool affectedByEMP = true;

        public float FiringArc => Mathf.Clamp(firingArc, 0, 360);

    }

}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection.Emit;
using RimWorld;
using Verse;
using Harmony;
using UnityEngine;

namespace TurretExtensions
{

    public class TurretFrameworkExtension : DefModExtension
    {

        public static readonly TurretFrameworkExtension defaultValues = new TurretFrameworkExtension();

        public bool useMannerShootingAccuracy = false;

        public bool useMannerAimingDelayFactor = false;

        public float mannerShootingAccuracyOffset = 0f;

        public bool canForceAttack = false;

    }

}
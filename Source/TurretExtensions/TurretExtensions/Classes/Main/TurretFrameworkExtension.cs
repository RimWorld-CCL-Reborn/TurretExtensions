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
        public override IEnumerable<string> ConfigErrors()
        {
            if (!useMannerShootingAccuracy && (shootingAccuracy < 0 || shootingAccuracy > 1))
            {
                yield return String.Format("shootingAccuracy is {0} but must be between 0 and 1. Resetting to default value of {1}...", shootingAccuracy, defaultValues.shootingAccuracy);
                shootingAccuracy = defaultValues.shootingAccuracy;
            }
        }

        public static readonly TurretFrameworkExtension defaultValues = new TurretFrameworkExtension();

        public bool useMannerShootingAccuracy = false;

        public bool useMannerAimingDelayFactor = false;

        public float mannerShootingAccuracyOffset = 0f;

        public bool canForceAttack = false;

        public float shootingAccuracy = 0.96f;

    }

}
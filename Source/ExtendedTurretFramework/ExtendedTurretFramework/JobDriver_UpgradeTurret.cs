using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;

namespace ExtendedTurretFramework
{
    class JobDriver_UpgradeTurret : JobDriver
    {
        public override bool TryMakePreToilReservations()
        {
            throw new NotImplementedException();
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            throw new NotImplementedException();
        }
    }
}

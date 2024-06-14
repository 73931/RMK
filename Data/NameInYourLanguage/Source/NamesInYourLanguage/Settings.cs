﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace NamesInYourLanguage
{
    public class NIYL_Settings : ModSettings
    {
        public bool Enable = true;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref Enable, "NIYL_Enable", true);
            base.ExposeData();
        }
    }
}

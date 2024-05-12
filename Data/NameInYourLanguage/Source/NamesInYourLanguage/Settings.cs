using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace NamesInYourLanguage
{
    public class Settings : ModSettings
    {
        public static bool Enable = true;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref Enable, "KoreanNames_Enable", true);
        }
    }
}

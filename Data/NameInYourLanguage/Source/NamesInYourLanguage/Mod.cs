using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace NamesInYourLanguage
{
    public class NIYL_Mod : Mod
    {
        NIYL_Settings settings;
        public NIYL_Mod(ModContentPack content) : base(content)
        {
            this.settings = GetSettings<NIYL_Settings>();

            Harmony harmony = new Harmony("seohyeon.namesinyourlanguage");
            harmony.PatchAll();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);
            var listing = new Listing_Standard();
            listing.Begin(inRect);

            //
            listing.CheckboxLabeled("RMK.NIYL.EnableLabel".Translate(), ref settings.Enable, "RMK.NIYL.EnableDesc".Translate());

            // 
            if (Prefs.DevMode && listing.ButtonText("RMK.NIYL.ExtractUntranslatedNamesLabel".Translate()))
            {
                var allNames = new List<string>();
                foreach (var (key, value) in StaticConstructor.NameTranslationDict)
                {
                    allNames.Add($"{key}->{value}");
                }
                allNames = allNames.Distinct().ToList();
                allNames.Sort();

                foreach (var item in StaticConstructor.NotTranslated)
                {
                    allNames.Add($"{item}->{item}");
                    Log.Message($"not translated -> {item}->{item}");
                }
                var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Translations.txt");
                File.WriteAllLines(path, allNames);
            }

            listing.End();
        }

        public override string SettingsCategory()
        {
            return "RMK - NIYL";
        }
    }
}

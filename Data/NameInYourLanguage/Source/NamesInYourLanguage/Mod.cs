using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime;
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

            // 활성화 설정 체크박스
            listing.CheckboxLabeled("RMK.NIYL.EnableLabel".Translate(), ref settings.Enable, "RMK.NIYL.EnableDesc".Translate());

            // 이름 추출 버튼
            if (Prefs.DevMode && listing.ButtonText("RMK.NIYL.ExtractUntranslatedNamesLabel".Translate()))
            {
                var allNames = new List<string>();
                foreach (var (key, tuple) in StaticConstructor.NameTranslationDict)
                {
                    NameTriple triple = tuple.Item2;
                    allNames.Add($"<{triple.First}::{triple.Nick}::{triple.Last}>{key}->{tuple.Item1}");
                }
                allNames = allNames.Distinct().ToList();
                allNames.Sort();

                foreach (var (key, tuple) in StaticConstructor.NotTranslated)
                {
                    NameTriple triple = tuple.Item2;
                    allNames.Add($"<{triple.First}::{triple.Nick}::{triple.Last}>{key}->{tuple.Item1}");
                    Log.Message($"[RMK.NamesInYourLanguage] Not translated: {key}->{tuple.Item1}");
                }
                var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Translations.txt");
                File.WriteAllLines(path, allNames);
            }

            listing.End();
        }

        public override string SettingsCategory()
        {
            return "RMK - 글자수가어디까지";
        }
    }
}
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
    public class Mod : Verse.Mod
    {
        public static bool Enable = true;

        public Mod(ModContentPack content) : base(content)
        {
            var harmony = new Harmony("seohyeon.namesinyourlanguage");
            harmony.PatchAll();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);
            var listing = new Listing_Standard();
            listing.Begin(inRect);
            listing.CheckboxLabeled("이름 번역 활성화 여부", ref Settings.Enable, null);
            if (Prefs.DevMode && listing.ButtonText("이름 추출하기 (개발자용)"))
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
            return "한글 이름 모드";
        }
    }
}

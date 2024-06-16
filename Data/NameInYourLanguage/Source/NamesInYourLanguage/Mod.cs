using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            if (Prefs.DevMode && listing.ButtonText("RMK.NIYL.ExtractUntranslatedNamesLabel".Translate())) // 팝업창 띄워서 진행 상황 안내해주는 기능 추가하기
            {
                List<string> allNames = new List<string>();
                foreach (var (key, tuple) in StaticConstructor.NameTranslationDict)
                {
                    NameTriple triple = tuple.Item2;
                    string meta = string.Empty;

                    if (triple != null)
                    {
                        Log.Message("<flag> IsValid: true");
                        string first = triple.First; Log.Message($"{first}");
                        string nick = triple.Nick; Log.Message($"{nick}");
                        string last = triple.Last; Log.Message($"{last}");
                    }

                    allNames.Add($"{meta}{key}->{tuple.Item1}");
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

                Log.Message("<flag> 3");
            }

            listing.End();
        }

        public override string SettingsCategory()
        {
            return "RMK - 글자수가어디까지가";
        }
    }
}
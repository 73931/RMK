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
            if (Prefs.DevMode && listing.ButtonText("RMK.NIYL.ExtractNamesLabel".Translate())) // 팝업창이나 알림 띄워서 진행 상황 안내해주는 기능 추가하기
            {
#if DEBUG
                Log.Message("[RMK.Debug] Starting to export names.");
#endif

                List<string> allNames = new List<string>();
#if DEBUG
                Log.Message("<flag> 170");
#endif
                foreach (var (key, tuple) in StaticConstructor.NameTranslationDict)
                {
                    NameTriple triple = tuple.Item2;
                    string tripleStrip = string.Empty;
                    if (triple != null)
                        tripleStrip = $"<{triple.First}::{triple.Nick}::{triple.Last}>";

                    allNames.Add($"{tripleStrip}{key}->{tuple.Item1}");
                }
#if DEBUG
                Log.Message("<flag> 171");
#endif
                allNames = allNames.Distinct().ToList();
                allNames.Sort();

#if DEBUG
                Log.Message("<flag> 172");
#endif

                foreach (var (key, tuple) in StaticConstructor.NotTranslated)
                {
                    NameTriple triple = tuple.Item2;
                    string tripleStrip = string.Empty;
                    if (triple != null)
                        tripleStrip = $"<{triple.First}::{triple.Nick}::{triple.Last}>";

                    allNames.Add($"{tripleStrip}{key}->{tuple.Item1}");
#if DEBUG
                    Log.Message($"[RMK.NamesInYourLanguage] Not translated: {key}->{tuple.Item1}");
#endif
                }
                var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Translations.txt");
                File.WriteAllLines(path, allNames);

#if DEBUG
                Log.Message("<flag> 173");
#endif
            }

            listing.End();
        }

        public override string SettingsCategory()
        {
            return "RMK.NIYL.ModTitle".Translate();
        }
    }
}
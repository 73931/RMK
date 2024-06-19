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

            listing.GapLine(Listing_Standard.DefaultGap); // 줄 간격 삽입

            // 이름 추출 버튼
            if (listing.ButtonText("RMK.NIYL.Export.BottonLabel".Translate(), null, (float)0.3))
            {
                List<string> allNames = new List<string>(); // 여기에 내보낼 데이터를 저장합니다.

                // 이미 번역된 이름도 정리해서 담아둡니다.
                foreach (var (key, tuple) in StaticConstructor.NameTranslationDict)
                {
                    NameTriple triple = tuple.Item2;
                    string tripleStrip = string.Empty;
                    if (triple != null)
                        tripleStrip = $"<{triple.First}::{triple.Nick}::{triple.Last}>";

                    allNames.Add($"{tripleStrip}{key}->{tuple.Item1}");
                }

                allNames = allNames.Distinct().ToList();
                allNames.Sort();

                // 번역되지 않은 이름을 정리해서 담아둡니다.
                foreach (var (key, tuple) in StaticConstructor.NotTranslated)
                {
                    NameTriple triple = tuple.Item2;
                    string tripleStrip = string.Empty;
                    if (triple != null)
                        tripleStrip = $"<{triple.First}::{triple.Nick}::{triple.Last}>";

                    allNames.Add($"{tripleStrip}{key}->{tuple.Item1}");
                }

                // 정리된 이름 데이터를 바탕화면에 내보냅니다.
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Translations.txt");
                try
                {
                    File.WriteAllLines(path, allNames);

                    MessageTypeDef RMK_ExportComplete = new MessageTypeDef();
                    Messages.Message("RMK.NIYL.Export.Success".Translate(path), RMK_ExportComplete, false);
                }
                catch
                {
                    Log.Error("[RMK.NamesInYourLanguage] " + "RMK.NIYL.Export.Failed".Translate());
                }
            }
            listing.End();
        }

        // 모드 설정 창에서 보여지는 이름입니다.
        public override string SettingsCategory()
        {
            return "RMK.NIYL.ModTitle".Translate();
        }
    }
}
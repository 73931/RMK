using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using HarmonyLib;
using UnityEngine;
using Verse;
using System.Runtime.ConstrainedExecution;
using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace NamesInYourLanguage
{
    [StaticConstructorOnStartup]
    public static class StaticConstructor
    {
        public static readonly DictionaryWithMetaValue<string, string, NameTriple> NameTranslationDict = new DictionaryWithMetaValue<string, string, NameTriple>();
        public static readonly DictionaryWithMetaValue<string, string, NameTriple> NotTranslated = new DictionaryWithMetaValue<string, string, NameTriple>();
        public static void Prepare()
        {
            if (Translator.TryGetTranslatedStringsForFile("Names/Translations", out List<string> lst))
            {
                Stopwatch stopwatch = Stopwatch.StartNew();

                NameTranslationDict.Clear();
                foreach (string item in lst)
                {
                    if (item.StartsWith("//"))
                        continue;

                    string pattern = @"^(?:<([^>]*)>)?([^>]+)->(.+)$"; // <Group1>Group2->Group3
                    Match match = Regex.Match(item, pattern);

                    string meta = match.Groups[1].Value; // 아직 사용처 구현을 안함
                    string lhs = match.Groups[2].Value;
                    string rhs = match.Groups[3].Value;

                    if (lhs == string.Empty || rhs == string.Empty)
                        continue;

                    NameTranslationDict.Add(lhs, rhs, null); // 여기에 세 번째 매개변수가 null이 아니라 기존 솔리드DB에서 NameTriple을 검색해서 채워줄 수 있도록 하기
                }
                stopwatch.Stop();

                Log.Message($"[RMK.NamesInYourLanguage] {NameTranslationDict.Count()} name translations were found in {stopwatch.ElapsedMilliseconds}ms.");
            }
            else
            {
                Log.Error("[RMK.NamesInYourLanguage] Name translations were not found.");
            }
        }

        static StaticConstructor()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            LongEventHandler.QueueLongEvent(() =>
            {
                var banks = (Dictionary<PawnNameCategory, NameBank>)AccessTools.Field(typeof(PawnNameDatabaseShuffled), "banks").GetValue(null);
                foreach (var nameBank in banks.Values)
                {
                    var names = (List<string>[,])AccessTools.Field(typeof(NameBank), "names").GetValue(nameBank);
                    foreach (var name in names)
                    {
                        for (int k = 0; k < name.Count; k++)
                        {
                            if (NameTranslationDict.TryGetValue(name[k], out var translation))
                                name[k] = translation;
                            else
                                AddIfNotTranslated(name[k]);
                        }
                    }
                }

                if(LoadedModManager.GetMod<NIYL_Mod>().GetSettings<NIYL_Settings>().Enable)
                {
                    Log.Message("[RMK.NamesInYourLanguage] The module is set to enabled.");

                    foreach (NameTriple nameTriple in PawnNameDatabaseSolid.AllNames())
                    {
                        TranslateNameTriple(nameTriple);
                    }

                    foreach (PawnBio pawnBio in SolidBioDatabase.allBios)
                    {
                        TranslateNameTriple(pawnBio.name);
                    }
                }
                else { Log.Message("[RMK.NamesInYourLanguage] The module is set to disabled."); }

            }
            , "Inject names", false, null);
            stopwatch.Stop();
            Log.Message($"[RMK.NamesInYourLanguage] Translation was complete in {stopwatch.ElapsedMilliseconds}ms. {NotTranslated.Count()} names are left not to be translated.");
        }

        private static readonly FieldInfo FieldInfoNameFirst = AccessTools.Field(typeof(NameTriple), "firstInt");
        private static readonly FieldInfo FieldInfoNameLast = AccessTools.Field(typeof(NameTriple), "lastInt");
        private static readonly FieldInfo FieldInfoNameNick = AccessTools.Field(typeof(NameTriple), "nickInt");

        private static void TranslateNameTriple(NameTriple nameTriple)
        {
            if (nameTriple.First != null && NameTranslationDict.TryGetValue(nameTriple.First, out var translation))
                FieldInfoNameFirst.SetValue(nameTriple, translation);
            else
                AddIfNotTranslated(nameTriple.First);
            if (nameTriple.Last != null && NameTranslationDict.TryGetValue(nameTriple.Last, out translation))
                FieldInfoNameLast.SetValue(nameTriple, translation);
            else
                AddIfNotTranslated(nameTriple.Last);
            if (nameTriple.Nick != null && NameTranslationDict.TryGetValue(nameTriple.Nick, out translation))
                FieldInfoNameNick.SetValue(nameTriple, translation);
            else
                AddIfNotTranslated(nameTriple.Nick);
        }

        private static void AddIfNotTranslated(string name)
        {
            if (Regex.IsMatch(name, "[A-Za-z]+") && !Regex.IsMatch(name, "[가-힣]+"))
            {
                if (NameTranslationDict.TryGetMetaValue(name, out NameTriple triple))
                NotTranslated.Add(name, name, triple);
            }
                
        }
    }
}

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

                    string pattern = @"(?:<([^>]*)>)?([^>]+)->([^>]+)"; // <first::nick:last>name->translation 에서 name과 translation 부분만 캡처하여
                    Match match = Regex.Match(item, pattern); // 각각 Groups[2], Groups[3]에 저장합니다. (비필수인 <first::nick:last> 부분인 Group[1]은 캡처되지 않고 빈 문자열로 남습니다.)

                    string lhs = match.Groups[2].Value;
                    string rhs = match.Groups[3].Value;

                    if (lhs == string.Empty || rhs == string.Empty)
                        continue;

                    NameTranslationDict.Add(lhs, rhs, null);
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

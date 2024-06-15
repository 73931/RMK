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

namespace NamesInYourLanguage
{
    [StaticConstructorOnStartup]
    public static class StaticConstructor
    {
        public static readonly Dictionary<string, string> NameTranslationDict = new Dictionary<string, string>();
        public static readonly HashSet<string> NotTranslated = new HashSet<string>();
        public static void Prepare() // Strings 파일을 불러와서 Dictionary에 저장
        {
            if (Translator.TryGetTranslatedStringsForFile("Names/Translations", out var lst))
            {
                NameTranslationDict.Clear();
                foreach (var item in lst) // item은 Translations.txt 파일의 한 개 줄
                {
                    if (item.StartsWith("//"))
                        continue;

                    string pattern = @"^(?:<([^>]+)>)?([^->]+)->(.+)$";
                    Match match = Regex.Match(item, pattern);

                    // lhs->rhs
                    string lhs = match.Groups[3].Value;
                    string rhs = match.Groups[4].Value;

                    if (lhs == string.Empty || rhs == string.Empty)
                        continue;

                    NameTranslationDict[lhs] = rhs;
                }
                Log.Message($"[RMK.NamesInYourLanguage] {NameTranslationDict.Count} name translations was found.");
            }
            else
            {
                Log.Error("[RMK.NamesInYourLanguage] Name translations was not found.");
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
                    Log.ResetMessageCount();
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
                NotTranslated.Add(name);
        }
    }
}

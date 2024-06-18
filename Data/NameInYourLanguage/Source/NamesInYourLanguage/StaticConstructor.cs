using RimWorld;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Verse;
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

                    string pattern1 = @"^(?:<([^>]*)>)?([^>]+)->(.+)$"; // <Group1>Group2->Group3
                    Match match1 = Regex.Match(item, pattern1);

                    string lhs = match1.Groups[2].Value; // Group2 값 저장
                    string rhs = match1.Groups[3].Value; // Group3 값 저장

                    if (lhs == string.Empty || rhs == string.Empty)
                        continue;

                    string meta = match1.Groups[1].Value; // Group1 값 저장
                    string pattern2 = @"^(.*?)(?:::(.*?))?(?:::(.*?))?$"; // match1의 Group1 -> Group1::Group2::Group3
                    Match match2 = Regex.Match(meta, pattern2);

                    string first = match2.Groups[1].Value;
                    string nick = match2.Groups[2].Value;
                    string last = match2.Groups[3].Value;

                    NameTriple triple = null;

                    Log.Message("[RMK.NIYL.Debug] flag ■");
                    if (first + nick + last != string.Empty) // 일단 Translations.txt에 NameTriple 메타 데이터가 기재되어 있을 경우 그걸 같이 저장해둡니다.
                    {
                        triple = new NameTriple(first, nick, last);
#if DEBUG
                        Log.Message($"[RMK.NIYL.Debug] Match Success | {triple.ToStringFull}");
#endif
                    }
                    else // 아니라면 기존 DB에서 검색을 시도하고, 있다면 그걸 같이 저장해둡니다.
                    {
                        Log.Message("[RMK.NIYL.Debug] flag ■ ■");
                        bool foundName = false;

                        if (PawnNameDatabaseSolid.AllNames().Count() == 0)
                            Log.Message("[RMK.NIYL.Debug] flag ■ ■ | PawnNameDatabaseSolid | No data"); // 이 시점에선 DB 자체가 없네?


                        foreach (NameTriple nameTriple in PawnNameDatabaseSolid.AllNames()) // 여기로 들어가질 못해
                        {
                            Log.Message($"[RMK.NIYL.Debug] flag ■ ■ | {nameTriple.ToStringFull}");
                            if (TryFindNameOnTriple(lhs, nameTriple, out NameTriple foundTriple))
                            {
                                triple = foundTriple;
                                Log.Message($"[RMK.NIYL.Debug] flag ■ ■ | TryFindNameOnTriple => PawnNameDatabaseSolid | {triple.ToStringFull}");
                                foundName = true;
                                break;
                            }
                        }
                        Log.Message($"[RMK.NIYL.Debug] flag ■ ■ ■ | foundName: {foundName}");

                        if (SolidBioDatabase.allBios.Count() == 0)
                            Log.Message("[RMK.NIYL.Debug] flag ■ ■ ■ | SolidBioDatabase | No data"); // 이 시점에선 DB 자체가 없네?

                        if (!foundName)
                        foreach (PawnBio pawnBio in SolidBioDatabase.allBios) // 여기로 들어가질 못해
                            {
                            Log.Message($"[RMK.NIYL.Debug] flag ■ ■ ■ | {pawnBio.name.ToStringFull}");
                            if (TryFindNameOnTriple(lhs, pawnBio.name, out NameTriple foundTriple))
                            {
                                triple = foundTriple;
                                Log.Message($"[RMK.NIYL.Debug] flag ■ ■ ■ | TryFindNameOnTriple => SolidBioDatabase | {triple.ToStringFull}");
                                foundName = true;
                                break;
                            }
                        }
                        Log.Message("[RMK.NIYL.Debug] flag ■ ■ ■ ■");
                    }
                    Log.ResetMessageCount();
                    NameTranslationDict.Add(lhs, rhs, triple);
                }
                stopwatch.Stop();

                Log.Message("[RMK.NamesInYourLanguage] " + "RMK.NIYL.Log.LoadTranslationsSuccess".Translate(NameTranslationDict.Count(), stopwatch.ElapsedMilliseconds));
            }
            else
            {
                Log.Warning("[RMK.NamesInYourLanguage] " + "RMK.NIYL.Log.LoadTranslationsFailed".Translate());
            }
        }

        static StaticConstructor()
        {
            
            LongEventHandler.QueueLongEvent(() =>
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
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
                            { 
                                AddIfNotTranslated(name[k]);
                            }
                        }
                    }
                }

                if(LoadedModManager.GetMod<NIYL_Mod>().GetSettings<NIYL_Settings>().Enable)
                {
                    foreach (NameTriple nameTriple in PawnNameDatabaseSolid.AllNames())
                    {
                        TranslateNameTriple(nameTriple);
                    }

                    foreach (PawnBio pawnBio in SolidBioDatabase.allBios)
                    {
                        TranslateNameTriple(pawnBio.name);
                    }

                    stopwatch.Stop();
                    Log.Message("[RMK.NamesInYourLanguage] " + "RMK.NIYL.Log.TranslationComplete".Translate(stopwatch.ElapsedMilliseconds, NotTranslated.Count()));
                }
                else { stopwatch.Stop(); Log.Message("[RMK.NamesInYourLanguage] " + "RMK.NIYL.Log.ModuleDisabled".Translate()); }
            }
            , "Inject names", false, null);
        }

        private static readonly FieldInfo FieldInfoNameFirst = AccessTools.Field(typeof(NameTriple), "firstInt");
        private static readonly FieldInfo FieldInfoNameLast = AccessTools.Field(typeof(NameTriple), "lastInt");
        private static readonly FieldInfo FieldInfoNameNick = AccessTools.Field(typeof(NameTriple), "nickInt");

        private static void TranslateNameTriple(NameTriple nameTriple)
        {
#if DEBUG
            Log.Message($"[RMK.NIYL.Debug] Trying to translate {nameTriple.ToStringFull}");
            Log.ResetMessageCount();
#endif
            if (nameTriple.First != null && NameTranslationDict.TryGetValue(nameTriple.First, out var translation))
                FieldInfoNameFirst.SetValue(nameTriple, translation);
            else
                AddIfNotTranslated(nameTriple.First, nameTriple);

            if (nameTriple.Last != null && NameTranslationDict.TryGetValue(nameTriple.Last, out translation))
                FieldInfoNameLast.SetValue(nameTriple, translation);
            else
                AddIfNotTranslated(nameTriple.Last, nameTriple);

            if (nameTriple.Nick != null && NameTranslationDict.TryGetValue(nameTriple.Nick, out translation))
                FieldInfoNameNick.SetValue(nameTriple, translation);
            else
                AddIfNotTranslated(nameTriple.Nick, nameTriple);
        }

        private static void AddIfNotTranslated(string name, NameTriple triple = null)
        {
#if DEBUG
            string outString;
            if (triple == null)
                outString = string.Empty;
            else
                outString = triple.ToStringFull;
#endif

            if (Regex.IsMatch(name, "[A-Za-z]+") && !Regex.IsMatch(name, "[가-힣]+"))
            {
                if (!NotTranslated.ContainsKey(name))
                {
                    NotTranslated.Add(name, name, triple);
#if DEBUG
                    Log.Message($"[RMK.NIYL.Debug] AddIfNotTranslated | name: {name} | triple: {outString}");
#endif
                }
            }
        }

        public static bool TryFindNameOnTriple(string name, NameTriple triple, out NameTriple foundTriple)
        {
            List<string> pieces = new List<string> { triple.First, triple.Nick, triple.Last };
            if (pieces.Contains(name))
            {
                foundTriple = triple;
                return true;
            }
            else
            {
                foundTriple = null;
                return false;
            }
        }
    }
}

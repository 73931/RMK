using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Verse;

namespace NamesInYourLanguage
{
    [StaticConstructorOnStartup]
    public static class StaticConstructor
    {
        public static readonly DictionaryWithMetaValue<string, string, NameTriple> NameTranslationDict = new DictionaryWithMetaValue<string, string, NameTriple>();
        public static readonly DictionaryWithMetaValue<string, string, NameTriple> NotTranslated = new DictionaryWithMetaValue<string, string, NameTriple>();

        private static readonly Dictionary<string, NameTriple> PawnNameDatabaseSolidAllNames = new Dictionary<string, NameTriple>();
        private static readonly Dictionary<string, NameTriple> SolidBioDatabaseAllBiosName = new Dictionary<string, NameTriple>();

        public static long TotalWorkTime = 0; // 전체 동작 시간을 체크하기 위한 변수

        public static void Prepare()
        {
            Stopwatch stopwatch_Prepare = Stopwatch.StartNew();

            if (Translator.TryGetTranslatedStringsForFile("Names/Translations", out List<string> lst))
            {
                

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
                    if (first + nick + last != string.Empty) // 일단 Translations.txt에 NameTriple 메타 데이터가 기재되어 있을 경우 그걸 같이 저장해둡니다.
                    {
                        triple = new NameTriple(first, nick, last);
#if DEBUG
                        Log.Message($"[RMK.NIYL.Debug] Match Success | {triple.ToStringFull}");
#endif
                    }

                    Log.ResetMessageCount();
                    NameTranslationDict.Add(lhs, rhs, triple);
                }

                stopwatch_Prepare.Stop();
                TotalWorkTime += stopwatch_Prepare.ElapsedMilliseconds;
                Log.Message("[RMK.NamesInYourLanguage] " + "RMK.NIYL.Log.LoadTranslationsSuccess".Translate(NameTranslationDict.Count(), stopwatch_Prepare.ElapsedMilliseconds));
            }
            else
            {
                stopwatch_Prepare.Stop();
                TotalWorkTime += stopwatch_Prepare.ElapsedMilliseconds;
                Log.Warning("[RMK.NamesInYourLanguage] " + "RMK.NIYL.Log.LoadTranslationsFailed".Translate());
            }
        }

        static StaticConstructor()
        {
            LongEventHandler.QueueLongEvent(() =>
            {
                Stopwatch stopwatch_main = Stopwatch.StartNew();

                // 바닐라의 비번역 NameTriple을 부분별로 쪼개서 찾기 쉽게 정리해둡니다.
                Stopwatch stopwatch_sub = Stopwatch.StartNew();
                foreach (NameTriple nameTriple in PawnNameDatabaseSolid.AllNames())
                {
                    PawnNameDatabaseSolidAllNames.TryAddOnDictionary(nameTriple.First, nameTriple);
                    PawnNameDatabaseSolidAllNames.TryAddOnDictionary(nameTriple.Nick, nameTriple);
                    PawnNameDatabaseSolidAllNames.TryAddOnDictionary(nameTriple.Last, nameTriple);
                }

                foreach (PawnBio pawnBio in SolidBioDatabase.allBios)
                {
                    SolidBioDatabaseAllBiosName.TryAddOnDictionary(pawnBio.name.First, pawnBio.name);
                    SolidBioDatabaseAllBiosName.TryAddOnDictionary(pawnBio.name.Nick, pawnBio.name);
                    SolidBioDatabaseAllBiosName.TryAddOnDictionary(pawnBio.name.Last, pawnBio.name);
                }

                stopwatch_sub.Stop();
                Log.Message($"[RMK.NIYL.Debug] Loading solid NameTriples got {stopwatch_sub.ElapsedMilliseconds}ms");
                //___________________________________________________________________________________________________________

                // Translation.txt 파일을 통해 생성한 NameTranslationDict의 비어있는 NameTriple 정보를 바닐라 데이터에서 찾아봅니다.
                stopwatch_sub.Restart();

                Dictionary<string, NameTriple> tempTripleDict = new Dictionary<string, NameTriple>();

                foreach (var (key, tuple) in NameTranslationDict)
                {
                    Log.ResetMessageCount();

                    NameTriple triple = tuple.Item2;

                    // Translations.txt 파일에서 NameTriple 정보가 기록되지 않은 경우
                    if (triple == null || triple.First + triple.Nick + triple.Last == "")
                    {
                        // 바닐라 데이터에서 검색을 시도합니다.
                        if (TryFindNameTripleFromSolid(key, out NameTriple searchedTriple))
                        {
                            tempTripleDict.Add(key, searchedTriple);
                        }
                    }
                }

                stopwatch_sub.Stop();
                Log.Message($"[RMK.NIYL.Debug] Found {tempTripleDict.Count()} NameTriples from solid database in {stopwatch_sub.ElapsedMilliseconds}ms.");
                //___________________________________________________________________________________________________________

                // 위 단계에서 tempTripleDict에 저장된 NameTriple을 찾은 이름들을 NameTranslationDict에서 다시 찾아 Triple 정보를 채워줍니다.
                stopwatch_sub.Restart();

                foreach (var (key, triple) in tempTripleDict)
                {
                    NameTranslationDict.TrySetMetaValue(key, triple);

                    NameTranslationDict.TryGetMetaValue(key, out NameTriple logTriple);
                    Log.ResetMessageCount();
                    //Log.Message($"[RMK.NIYL.Debug] Filling NameTriple | {key} | with {logTriple.ToStringFull}");
                }

                stopwatch_sub.Stop();
                Log.Message($"[RMK.NIYL.Debug] Filling NameTriple is complete in {stopwatch_sub.ElapsedMilliseconds}ms");
                //___________________________________________________________________________________________________________

                // 모듈 설정이 활성화 돼있을 경우 번역을 시작합니다.
                if (LoadedModManager.GetMod<NIYL_Mod>().GetSettings<NIYL_Settings>().Enable)
                {
                    // PawnNameDatabaseShuffled의 이름을 번역합니다.
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

                    // PawnNameDatabaseSolid의 NameTriple 형식의 이름을 번역합니다.
                    foreach (NameTriple nameTriple in PawnNameDatabaseSolid.AllNames())
                    {
                        TranslateNameTriple(nameTriple);
                    }

                    // SolidBioDatabase.allBios의 NameTriple 형식의 이름을 번역합니다.
                    foreach (PawnBio pawnBio in SolidBioDatabase.allBios)
                    {
                        TranslateNameTriple(pawnBio.name);
                    }

                    stopwatch_main.Stop();
                    TotalWorkTime += stopwatch_main.ElapsedMilliseconds;
                    Log.Message("[RMK.NamesInYourLanguage] " + "RMK.NIYL.Log.TranslationComplete".Translate(stopwatch_main.ElapsedMilliseconds, NotTranslated.Count()));
                }
                //___________________________________________________________________________________________________________
                else
                {
                    stopwatch_main.Stop();
                    TotalWorkTime += stopwatch_main.ElapsedMilliseconds;
                    Log.Message("[RMK.NamesInYourLanguage] " + "RMK.NIYL.Log.ModuleDisabled".Translate());
                }

                Log.Message("[RMK.NamesInYourLanguage] " + "RMK.NIYL.Log.TotalWorkTime".Translate(TotalWorkTime));
            }
            , "RMK.NIYL.StartUp".Translate(), false, null);
        }

        private static readonly FieldInfo FieldInfoNameFirst = AccessTools.Field(typeof(NameTriple), "firstInt");
        private static readonly FieldInfo FieldInfoNameLast = AccessTools.Field(typeof(NameTriple), "lastInt");
        private static readonly FieldInfo FieldInfoNameNick = AccessTools.Field(typeof(NameTriple), "nickInt");

        private static void TranslateNameTriple(NameTriple nameTriple)
        {
#if DEBUG
            //Log.Message($"[RMK.NIYL.Debug] Trying to translate {nameTriple.ToStringFull}");
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
                    //Log.Message($"[RMK.NIYL.Debug] AddIfNotTranslated | name: {name} | triple: {outString}");
#endif
                }
            }
        }

        private static bool TryFindNameTripleFromSolid(string name, out NameTriple result)
        {
            result = null;
            bool found = false;

            if (!found)
                if (PawnNameDatabaseSolidAllNames.TryGetValue(name, out result))
                {
                    found = true;
                }

            if (!found)
                if (SolidBioDatabaseAllBiosName.TryGetValue(name, out result))
                {
                    found = true;
                }

#if DEBUG
            //if (result != null)
            //Log.Message($"[RMK.NIYL.Debug] TryFindNameTripleFromSolid | name: {name} | result: {result.ToStringFull}");
#endif
            return found;
        }
    }

    public static class DictionaryExtension
    {
        public static bool TryAddOnDictionary<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (!dictionary.ContainsKey(key))
            {
                dictionary.Add(key, value);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}

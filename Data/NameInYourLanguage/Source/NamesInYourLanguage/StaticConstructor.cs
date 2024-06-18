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

                // Translation.txt 파일을 통해 생성한 NameTranslationDict의 비어있는 NameTriple 정보를 바닐라 데이터에서 검색하여 저장합니다.
                Log.Message("[RMK.NIYL.Debug] flag ▲");
                Dictionary<string, NameTriple> tempTripleDict = new Dictionary<string, NameTriple>();

                foreach (var (key, tuple) in NameTranslationDict)
                {
                    NameTriple triple = tuple.Item2;

                    // Translations.txt 파일에서 NameTriple 정보가 기록되지 않은 경우 바닐라 데이터에서 검색을 시도합니다.
                    if (triple == null || triple.First + triple.Nick + triple.Last == "")
                    {
                        if(TryFindNameTripleOnDatabase(key, out NameTriple searchedTriple))
                        {
                            tempTripleDict.Add(key, searchedTriple);
                            Log.Message($"[RMK.NIYL.Debug] Found name {key} in {searchedTriple.ToStringFull} from Database");
                        }
                    }
                }

                // 위 단계에서 tempTripleDict에 저장된 NameTriple을 찾은 이름들을 NameTranslationDict에서 다시 찾아 Triple 정보를 채워줍니다.
                /* 임시로 빼둠
                foreach (var (key, triple) in tempTripleDict)
                {
                    NameTranslationDict.TrySetMetaValue(key, triple);

                    NameTranslationDict.TryGetMetaValue(key, out NameTriple logTriple);
                    Log.Message($"[RMK.NIYL.Debug] Filled NameTriple datum of {key} with {logTriple.ToStringFull}");
                }
                */

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

                    stopwatch.Stop();
                    Log.Message("[RMK.NamesInYourLanguage] " + "RMK.NIYL.Log.TranslationComplete".Translate(stopwatch.ElapsedMilliseconds, NotTranslated.Count()));
                }
                else { stopwatch.Stop(); Log.Message("[RMK.NamesInYourLanguage] " + "RMK.NIYL.Log.ModuleDisabled".Translate()); }
            }
            , "RMK.NIYL.StartUp".Translate(), false, null);
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

        // name이 triple에 존재하는지 여부를 확인합니다.
        public static bool TryFindNameOnTriple(string name, NameTriple triple)
        {
            List<string> pieces = new List<string> { triple.First, triple.Nick, triple.Last };
            if (pieces.Contains(name))
                return true;
            else
                return false;
        }

        // name이 바닐라 이름 DB에 존재하는지 확인하고 매치되는 NameTriple을 반환합니다.
        private static bool TryFindNameTripleOnDatabase(string name, out NameTriple outTriple)
        {
            outTriple = null;
            bool foundName = false;

            // 먼저 PawnNameDatabaseSolid에서 name에 해당하는 NameTriple을 검색합니다.
            if (!foundName)
                foreach (NameTriple nameTriple in PawnNameDatabaseSolid.AllNames())
                {
                    // Log.Message($"[RMK.NIYL.Debug] flag Search 1 | {nameTriple.ToStringFull}"); // 여기다 로그 넣지말 것 -> 부하가 극심함
                    if (TryFindNameOnTriple(name, nameTriple))
                    {
                        outTriple = nameTriple;
                        Log.Message($"[RMK.NIYL.Debug] flag Search 1 | TryFindNameOnTriple => PawnNameDatabaseSolid | {outTriple.ToStringFull}");
                        foundName = true;
                        break;
                    }
                    Log.ResetMessageCount();
                }

            Log.Message($"[RMK.NIYL.Debug] flag Search 1 after | foundName: {foundName}");

            if (!foundName)
                // PawnNameDatabaseSolid에서 찾지 못했을 경우 SolidBioDatabase.allBios에서 검색합니다.
                foreach (PawnBio pawnBio in SolidBioDatabase.allBios)
                {
                    // Log.Message($"[RMK.NIYL.Debug] flag Search 2 | {pawnBio.name.ToStringFull}");
                    if (TryFindNameOnTriple(name, pawnBio.name))
                    {
                        outTriple = pawnBio.name;
                        Log.Message($"[RMK.NIYL.Debug] flag Search 2 | TryFindNameOnTriple => SolidBioDatabase | {outTriple.ToStringFull}");
                        foundName = true;
                        break;
                    }
                    Log.ResetMessageCount();
                }

            Log.Message($"[RMK.NIYL.Debug] flag Search 2 after | foundName: {foundName}");
            Log.ResetMessageCount();
            return foundName;
        }
    }
}

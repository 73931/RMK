using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using Verse;

namespace NamesInYourLanguage
{
    [HarmonyPatch]
    public static class Patches
    {
        [HarmonyPatch(typeof(LanguageDatabase)), HarmonyPatch(nameof(LanguageDatabase.InitAllMetadata)), HarmonyPostfix]
        public static void Postfix_InitAllMetadata()
        {
            StaticConstructor.Prepare();
        }
    }
}

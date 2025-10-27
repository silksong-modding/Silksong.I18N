using HarmonyLib;
using TeamCherry.Localization;
using System.Collections.Generic;

namespace Silksong.I18N;

public class LanguagePatches
{
    [HarmonyPatch(typeof(Language), nameof(Language.DoSwitch))]
    [HarmonyPostfix]
    public static void InsertModdedSheets(LanguageCode newLang)
    {
        foreach (var (modId, languageInfo) in I18NPlugin.LanguageData)
        {
            var currentSheets = Language._currentEntrySheets;

            var language = languageInfo.Data.GetValueOrDefault(newLang);
            if (language != null)
            {
                foreach (var (sheetTitle, sheet) in language)
                {
                    currentSheets.Add($"Mod.{modId}{sheetTitle}", sheet);
                }
            }

            var fallbackCode = languageInfo.FallbackLanguageCode;
            if (fallbackCode == LanguageCode.N || fallbackCode == newLang)
                continue;

            var fallbackLanguage = languageInfo.Data[fallbackCode];
            foreach (var (fallbackSheetTitle, fallbackSheet) in fallbackLanguage)
            {
                var sheet = currentSheets.GetValueOrDefault(fallbackSheetTitle);
                if (sheet == null)
                {
                    currentSheets.Add(fallbackSheetTitle, fallbackSheet);
                }
                else
                {
                    foreach (var (key, value) in fallbackSheet)
                    {
                        sheet.TryAdd(key, value);
                    }
                }
            }
        }
    }
}
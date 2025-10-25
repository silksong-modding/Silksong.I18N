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
            var language = languageInfo.Data.GetValueOrDefault(newLang);

            var mainSheet = language?.MainSheet ?? new Dictionary<string, string>();
            var subSheets = language?.SubSheets ?? new Dictionary<string, Dictionary<string, string>>();
            var fallbackCode = languageInfo.FallbackLanguageCode;
            if (fallbackCode != LanguageCode.N && fallbackCode != newLang)
            {
                var fallbackLanguage = languageInfo.Data[fallbackCode];
                if (fallbackLanguage.MainSheet != null)
                {
                    foreach (var (key, value) in fallbackLanguage.MainSheet)
                    {
                        mainSheet.TryAdd(key, value);
                    }
                }

                if (fallbackLanguage.SubSheets != null)
                {
                    foreach (var (subSheetTitle, fallbackSubSheet) in fallbackLanguage.SubSheets)
                    {
                        var subSheet = subSheets.GetValueOrDefault(subSheetTitle);
                        if (subSheet == null)
                        {
                            subSheets.Add(subSheetTitle, fallbackSubSheet);
                        }
                        else
                        {
                            foreach (var (key, value) in fallbackSubSheet)
                            {
                                subSheet.TryAdd(key, value);
                            }
                        }
                    }
                }

            }
            Language._currentEntrySheets.Add($"Mod.{modId}", mainSheet);
            foreach (var (sheetTitle, subSheet) in subSheets)
            {
                Language._currentEntrySheets.Add($"Mod.{modId}/{sheetTitle}", subSheet);
            }
        }
    }
}
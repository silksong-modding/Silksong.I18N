using System;
using System.IO;
using System.Linq;
using HarmonyLib;
using Newtonsoft.Json;
using TeamCherry.Localization;

namespace Silksong.I18N;

public class LanguagePatches
{
    [HarmonyPatch(typeof(Language), nameof(Language.DoSwitch))]
    [HarmonyPostfix]
    public static void InsertModdedSheets(LanguageCode newLang)
    {
        var newLangString = newLang.ToString();
        var serializer = new JsonSerializer();
        foreach ((string modId, ModLanguageInfo languageInfo) in I18NPlugin.LanguageInfos)
        {
            I18NPlugin.Logger.LogDebug($"Inserting sheets for '{modId}'");
            string? targetLanguageFile = languageInfo.LanguageFiles
                .FirstOrDefault(language =>
                    String.Equals(Path.GetFileNameWithoutExtension(language), newLangString,
                        StringComparison.InvariantCultureIgnoreCase));

            targetLanguageFile ??= languageInfo.FallbackFile;

            if (targetLanguageFile == null)
                continue;
            using (StreamReader file = File.OpenText(Path.Combine(languageInfo.LanguageDirectory, targetLanguageFile)))
            {
                var languageData = (ModLanguageData?)serializer.Deserialize(file, typeof(ModLanguageData));
                if (languageData == null)
                    continue;

                if (languageData.MainSheet != null)
                    Language._currentEntrySheets.Add($"Mod.{modId}", languageData.MainSheet);

                if (languageData.SubSheets == null)
                    continue;

                foreach (var (subsheetName, values) in languageData.SubSheets)
                {
                    Language._currentEntrySheets.Add($"Mod.{modId}/{subsheetName}", values);
                }
            }
        }
    }
}
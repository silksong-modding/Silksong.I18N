using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json;
using TeamCherry.Localization;

namespace Silksong.I18N;

[BepInAutoPlugin(id: "org.silksong-modding.i18n")]
public partial class I18NPlugin : BaseUnityPlugin
{
    internal new static ManualLogSource Logger;
    private Harmony? _harmony;

    public static Dictionary<string, ModLanguageInfo> LanguageData = [];

    private void Awake()
    {
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {Name} ({Id}) has loaded!");
    }

    private void Start()
    {
        // Patching Language triggers its static constructor
        // triggering the static constructor at Awake or initial OnEnable will cause a crash
        _harmony = Harmony.CreateAndPatchAll(typeof(LanguagePatches));
        AddPluginLanguageFiles();
        LanguagePatches.InsertModdedSheets(Language._currentLanguage);
    }

    private static void AddPluginLanguageFiles()
    {
        foreach (var (pluginId, pluginInfo) in Chainloader.PluginInfos)
        {
            var pluginDir = Path.GetDirectoryName(pluginInfo.Location);
            if (String.Equals(pluginDir, Paths.PluginPath, StringComparison.InvariantCultureIgnoreCase))
            {
                Logger.LogWarning($"Ignoring '{pluginId}' because it is not in its own subdirectory");
                continue;
            }

            string? fallbackLanguage = null;
            try
            {
                var assembly = Assembly.LoadFile(pluginInfo.Location);
                fallbackLanguage = assembly.GetCustomAttributes(typeof(NeutralResourcesLanguageAttribute))
                    .Cast<NeutralResourcesLanguageAttribute>()
                    .FirstOrDefault()?.CultureName;
            }
            catch (Exception e)
            {
                Logger.LogError($"Failed to load assembly for '{pluginId}': {e.Message}");
            }

            var fallbackLanguageCode = LanguageCode.N;
            if (fallbackLanguage == null)
                Logger.LogWarning($"No neutral language found for '{pluginId}'");
            else if (!Enum.TryParse(fallbackLanguage, out fallbackLanguageCode))
                Logger.LogWarning(
                    $"Neutral language {fallbackLanguage} specified by '{pluginId}' is not a valid language code");

            var enumerationOptions = new EnumerationOptions
            {
                MatchCasing = MatchCasing.CaseInsensitive,
                MatchType = MatchType.Simple,
            };

            var serializer = new JsonSerializer();

            foreach (var languageDirectory in Directory.EnumerateDirectories(pluginDir, "language", enumerationOptions))
            {
                var languageInfo = new ModLanguageInfo();
                foreach (var languageFile in Directory.EnumerateFiles(languageDirectory, "*.json", enumerationOptions))
                {
                    var languageName = Path.GetFileNameWithoutExtension(languageFile);
                    if (!Enum.TryParse(languageName.ToUpper(), out LanguageCode languageCode))
                    {
                        Logger.LogWarning($"'{pluginId}' provides unknown language '{languageName}', ignoring");
                        continue;
                    }
                    try
                    {
                        using (StreamReader file =
                               File.OpenText(languageFile))
                        {
                            var languageData = (ModLanguageData?)serializer.Deserialize(file, typeof(ModLanguageData));
                            if (languageData == null)
                                continue;

                            languageInfo.Data.Add(languageCode, languageData);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.LogError($"Failed to parse '{Path.GetFileName(languageFile)}' from '{pluginId}': {e.Message}");
                    }
                }

                if (fallbackLanguageCode != LanguageCode.N)
                {
                    if (languageInfo.Data.ContainsKey(fallbackLanguageCode))
                        languageInfo.FallbackLanguageCode = fallbackLanguageCode;
                    else
                        Logger.LogWarning(
                            $"'{pluginId}' specifies fallback language {fallbackLanguage}, but that language is not available");
                }

                LanguageData.Add(pluginId, languageInfo);

                break;
            }
        }
    }
}
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
    internal static new ManualLogSource Logger;
    private Harmony? _harmony;

    public static Dictionary<string, ModLanguageInfo> LanguageInfos = [];
    
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
        foreach (var (id, info) in Chainloader.PluginInfos)
        {
            var pluginDir = Path.GetDirectoryName(info.Location);
            if (pluginDir == Paths.PluginPath)
            {
                Logger.LogWarning($"Ignoring '{id}' because it is not in its own subdirectory");
                continue;
            }

            string? fallbackLanguage = null;
            try
            {
                var assembly = Assembly.LoadFile(info.Location);
                fallbackLanguage = assembly.GetCustomAttributes(typeof(NeutralResourcesLanguageAttribute))
                    .Cast<NeutralResourcesLanguageAttribute>()
                    .FirstOrDefault()?.CultureName;
            }
            catch (Exception e)
            {
                Logger.LogError($"Failed to load assembly for '{id}': {e.Message}");
            }
            
            if (fallbackLanguage == null)
                Logger.LogWarning($"No neutral language found for '{id}'");

            var enumerationOptions = new EnumerationOptions
            {
                MatchCasing = MatchCasing.CaseInsensitive,
                MatchType = MatchType.Simple,
            };
            
            
            foreach (var languageDirectory in Directory.EnumerateDirectories(pluginDir, "language", enumerationOptions))
            {
                var languageFiles = new List<string>();
                string? fallbackFile = null;
                foreach (var languageFile in Directory.EnumerateFiles(languageDirectory, "*.json", enumerationOptions))
                {
                    var languageFileName = Path.GetFileName(languageFile);
                    try
                    {
                        // Validation
                        JsonConvert.DeserializeObject(File.ReadAllText(languageFile, Encoding.UTF8), typeof(ModLanguageData));
                    }
                    catch (Exception e)
                    {
                        Logger.LogError($"Failed to parse from {id}: {e.Message}");
                        continue;
                    }
                    if (String.Equals(Path.GetFileNameWithoutExtension(languageFileName), fallbackLanguage, StringComparison.OrdinalIgnoreCase))
                        fallbackFile = languageFileName;
                    languageFiles.Add(languageFileName);
                }
                LanguageInfos.Add(id,
                    new ModLanguageInfo(languageDirectory,
                        languageFiles, fallbackFile));
                break;
            }
        }
    }
}

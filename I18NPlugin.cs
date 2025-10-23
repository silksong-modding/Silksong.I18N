using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using Newtonsoft.Json;
using TeamCherry.Localization;

namespace Silksong.I18N;

[BepInAutoPlugin("org.silksong-modding.i18n")]
sealed partial class I18NPlugin : BaseUnityPlugin
{
    void Start()
    {
        I18NPlugin.Instance = this;
        new Harmony(I18NPlugin.Id).PatchAll(typeof(I18NPlugin));
        Language.SwitchLanguage(Language.CurrentLanguage());
    }

    static I18NPlugin? Instance = null;

    [HarmonyPatch(typeof(Language), nameof(Language.DoSwitch))]
    [HarmonyPostfix]
    static void OnLanguageSwitched()
    {
        var plugin = I18NPlugin.Instance;
        if (plugin)
        {
            plugin.LoadAllModSheets();
        }
    }

    void LoadAllModSheets()
    {
        var lang = Language._currentLanguage;
        foreach (var (id, info) in Chainloader.PluginInfos)
        {
            var mod = info.Instance;
            if (!mod)
            {
                continue;
            }

            var modAsm = mod.GetType().Assembly;

            var modDir = Path.GetDirectoryName(modAsm.Location);
            if (!Directory.Exists(modDir))
            {
                continue;
            }

            var isPluginsDir =
                string.Compare(
                    Path.GetFullPath(modDir).TrimEnd(Path.DirectorySeparatorChar),
                    Path.GetFullPath(Paths.PluginPath).TrimEnd(Path.DirectorySeparatorChar),
                    StringComparison.InvariantCultureIgnoreCase
                ) == 0;

            if (isPluginsDir)
            {
                this.Logger.LogWarning($"mod {id} installed directly in plugins dir");
                continue;
            }

            Dictionary<string, string>? fallbackSheet = null;
            var langAttr = modAsm.GetCustomAttribute<NeutralResourcesLanguageAttribute>();
            if (langAttr is not null)
            {
                var fallbackLang = langAttr.CultureName;
                fallbackSheet = this.LoadModSheet(modDir, fallbackLang);
                if (fallbackSheet is not null)
                {
                    this.Logger.LogDebug(
                        $"loaded fallback sheet in language {fallbackLang} for mod {id}"
                    );
                }
            }

            var sheet = this.LoadModSheet(modDir, lang.ToString().ToLower(), fallbackSheet);
            if (sheet is not null)
            {
                Language._currentEntrySheets[$"Mods.{id}"] = sheet;
                this.Logger.LogDebug($"loaded sheet in language {lang} for mod {id}");
            }
        }
    }

    Dictionary<string, string>? LoadModSheet(
        string modDir,
        string lang,
        Dictionary<string, string>? fallback = null
    )
    {
        var opts = new EnumerationOptions();
        opts.MatchCasing = MatchCasing.CaseInsensitive;

        var hit = false;
        var modSheet = fallback ?? new Dictionary<string, string>();

        try
        {
            var modSheets = Directory
                .EnumerateDirectories(modDir, "languages", opts)
                .SelectMany(dir => Directory.EnumerateFiles(dir, $"{lang}.json", opts))
                .OrderBy(p => p)
                .Select(this.ReadSheetFile)
                .OfType<Dictionary<string, string>>();

            foreach (var sheet in modSheets)
            {
                hit = true;
                foreach (var (k, v) in sheet)
                {
                    modSheet[k] = v;
                }
            }
        }
        catch (Exception ex)
        {
            this.Logger.LogWarning($"unable to load mod sheets: {modDir}\n{ex}");
            return null;
        }

        if (hit || fallback is not null)
        {
            return modSheet;
        }
        else
        {
            return null;
        }
    }

    Dictionary<string, string>? ReadSheetFile(string path)
    {
        try
        {
            using var s = new StreamReader(File.OpenRead(path), Encoding.UTF8, false);
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(s.ReadToEnd());
        }
        catch (Exception ex)
        {
            this.Logger.LogWarning($"unable to read language file: {path}\n{ex}");
            return null;
        }
    }
}

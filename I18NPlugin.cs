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

[BepInAutoPlugin(id: "org.silksong-modding.i18n")]
internal sealed partial class I18NPlugin : BaseUnityPlugin
{
    private void Start()
    {
        I18NPlugin._instance = this;
        new Harmony(I18NPlugin.Id).PatchAll(typeof(I18NPlugin));
        this.LoadAllModSheets();
    }

    private static I18NPlugin? _instance = null;
    private static I18NPlugin? Instance => I18NPlugin._instance ? I18NPlugin._instance : null;

    [HarmonyPatch(typeof(Language), nameof(Language.DoSwitch))]
    [HarmonyPostfix]
    private static void OnLanguageSwitched() => I18NPlugin.Instance?.LoadAllModSheets();

    private void LoadAllModSheets()
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

            var isPluginsDir = string.Equals(
                Path.GetFullPath(modDir).TrimEnd(Path.DirectorySeparatorChar),
                Path.GetFullPath(Paths.PluginPath).TrimEnd(Path.DirectorySeparatorChar),
                StringComparison.InvariantCultureIgnoreCase
            );

            if (isPluginsDir)
            {
                this.Logger.LogInfo(
                    $"mod {id} installed directly in plugins dir, not loading languages"
                );
                continue;
            }

            Dictionary<string, string>? fallbackSheet = null;
            var langAttr = modAsm.GetCustomAttribute<NeutralResourcesLanguageAttribute>();
            if (langAttr is not null)
            {
                // We do effectively `.ToUpper().ToLower()` here to maintain semantic parity with
                // the subsequent call to `LoadModSheet` and avoid making assumptions about the
                // Unicode behavior of the `NeutralResourcesLanguageAttribute` string.
                var fallbackLang = langAttr.CultureName.ToUpper();
                fallbackSheet = this.LoadModSheet(modDir, fallbackLang.ToLower());
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

    private Dictionary<string, string>? LoadModSheet(
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
                if (hit)
                {
                    this.Logger.LogWarning(
                        $"multiple casings found for language {lang.ToUpper()} in: {modDir}"
                    );
                }

                hit = true;
                foreach (var (k, v) in sheet)
                {
                    modSheet[k] = v;
                }
            }
        }
        catch (Exception ex)
        {
            this.Logger.LogError($"unable to load mod sheets: {modDir}\n{ex}");
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

    private Dictionary<string, string>? ReadSheetFile(string path)
    {
        try
        {
            using var s = new StreamReader(File.OpenRead(path), Encoding.UTF8, false);
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(s.ReadToEnd());
        }
        catch (Exception ex)
        {
            this.Logger.LogError($"unable to read language file: {path}\n{ex}");
            return null;
        }
    }

    [HarmonyPatch(typeof(Language), nameof(Language.Get), [typeof(string), typeof(string)])]
    [HarmonyPostfix]
    private static void OnGetLocalizedText(string? key, string? sheetTitle) =>
        I18NPlugin.Instance?.CheckKeyExists(sheetTitle, key);

#pragma warning disable Harmony003
    [HarmonyPatch(typeof(LocalisedString), nameof(LocalisedString.ToString), [typeof(bool)])]
    private static void OnGetLocalizedString(LocalisedString __instance, bool allowBlankText) =>
        I18NPlugin.Instance?.CheckKeyExists(__instance.Sheet, __instance.Key, allowBlankText);
#pragma warning restore Harmony003

    private void CheckKeyExists(string? sheet, string? key, bool allowBlankText = true)
    {
        if (!string.IsNullOrEmpty(sheet) && !string.IsNullOrEmpty(key) && sheet.StartsWith("Mods."))
        {
            if (!Language.Has(key, sheet))
            {
                var lang = Language.CurrentLanguage();
                var modId = sheet.Substring("Mods.".Length);
                this.Logger.LogWarning($"language {lang} for mod {modId} missing: {key}");
            }
            else if (!allowBlankText)
            {
                var text = LocalisedString.ReplaceTags(Language.Get(key, sheet));
                if (string.IsNullOrWhiteSpace(text))
                {
                    var lang = Language.CurrentLanguage();
                    var modId = sheet.Substring("Mods.".Length);
                    this.Logger.LogWarning($"language {lang} for mod {modId} is blank at: {key}");
                }
            }
        }
    }
}

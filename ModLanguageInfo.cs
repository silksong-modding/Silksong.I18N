using System.Collections.Generic;

namespace Silksong.I18N;

public class ModLanguageInfo(string languageDirectory, List<string> languageFiles, string? fallbackFile)
{
    public readonly string LanguageDirectory = languageDirectory;
    public readonly List<string> LanguageFiles = languageFiles;
    public readonly string? FallbackFile = fallbackFile;
}
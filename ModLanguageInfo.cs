using System.Collections.Generic;
using TeamCherry.Localization;

namespace Silksong.I18N;

public class ModLanguageInfo
{
    public Dictionary<LanguageCode, Dictionary<string, Dictionary<string, string>>> Data = [];
    public LanguageCode FallbackLanguageCode = LanguageCode.N;
}
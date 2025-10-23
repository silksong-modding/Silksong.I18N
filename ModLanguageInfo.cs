using System.Collections.Generic;
using TeamCherry.Localization;

namespace Silksong.I18N;

public class ModLanguageInfo
{
    public Dictionary<LanguageCode, ModLanguageData> Data = new();
    public LanguageCode FallbackLanguageCode = LanguageCode.N;
}
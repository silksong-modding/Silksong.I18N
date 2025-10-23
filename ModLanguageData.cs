using System.Collections.Generic;

namespace Silksong.I18N;

public class ModLanguageData
{
    public Dictionary<string, string>? MainSheet { get; set; }
    public Dictionary<string, Dictionary<string, string>>? SubSheets { get; set; }
}
# Silksong.I18N

A Hollow Knight: Silksong mod to enable developers to provide translated text hassle-free.

## Developer Usage
This plugin does **not** need to be added as a reference, but **should** be specified as a `BepinDependency`.
It obtains translations by scanning plugin directories for `Language` folders.

### Creating Sheets
Create a directory named `Language` inside the folder of your plugin.

Inside the `Language` directory, create a json file named with the two-character code for the language you want to provide text for.

#### Example
In file `Language/en.json`
```json
{
  "EXAMPLE": "Hello World!"
}
```
### Using sheets in code
Sheet titles are named based on your BepInEx plugin Id.

If your plugin id is `me.mymod`, your main sheet will be named `Mod.me.mymod`.

#### Example
```csharp
using BepInEx;
using TeamCherry.Localization;

[BepinAutoPlugin(id: "me.mymod")]
[BepInDependency(DependencyGUID: "org.silksong-modding.i18n")]
public partial class MyMod : BaseUnityPlugin
{
    //...
    private void ExampleFunction()
    {
        //...
        var localisedString = new LocalisedString {
            Key = "EXAMPLE",
            Sheet = $"Mod.{Id}"
        };
        var directString = Language.Get("EXAMPLE", $"Mod.{Id}");
        //...
    }
    //...
}
```

### Fallback/Neutral Language
If files for some languages are not provided, a mod may specify a language to be used as fallback.
The fallback language is set via the `NeutralLanguageResources` attribute.

It can be provided in a csproj as such:
```xml
<ItemGroup>
    <AssemblyAttribute Include="System.Resources.NeutralResourcesLanguageAttribute">
        <_Parameter1>en</_Parameter1>
    </AssemblyAttribute>
</ItemGroup>
```

### Sub-sheets
Translated text can be organised into sub-sheets. 
Keys are not shared between sheets, so can be re-used to refer to different text.

To create a sub-sheet, create a `<language>.json` file in a subdirectory of your language folder

#### Example
In file `Language/ExampleSub/en.json`
```json
{
  "EXAMPLE": "This is a value in a sub-sheet"
}
```

Code usage
```csharp
var localisedString = new LocalisedString { 
    Sheet = $"Mod.{Id}/ExampleSub",
    Key = "EXAMPLE",
};
```
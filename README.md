# Silksong.I18N

A Hollow Knight: Silksong mod to enable developers to provide translated text hassle-free.

## Developer Usage

This plugin does **not** need to be added as a reference, but **should** be specified as a `BepinDependency`.
It obtains translations by scanning plugin directories for `language` folders.

### Creating Sheets
Create a directory named `language` inside the folder of your plugin.

Inside the `language` directory, create a json file named with the two-character code for the language you want to provide text for.
#### Example
In file `en.json`
```json
{
  "MainSheet": {
    "EXAMPLE": "Hello World!"
  },
  "SubSheets": {
    "OptionsSheet": {
      "OPT_1": "Option A",
      "OPT_2": "Option B",
      "OPT_3": "Option C"
    }
  }
}
```
### Using sheets in code

Sheet titles are named based on your BepInEx plugin Id.

If your plugin id is `me.mymod`, your main sheet will be named `Mod.me.mymod`.

Sub-sheets will be named `Mod.me.mymod/<sub-sheet name>`.

#### Example
```csharp

[BepinAutoPlugin(id: "me.mymod")]
[BepInDependency(DependencyGUID: "org.silksong-modding.i18n")]
public partial class MyMod : BaseUnityPlugin
{
    //...
    private void ExampleFunction()
    {
        //...
        var helloWorld = TeamCherry.Localization.Language.Get("EXAMPLE", $"Mod.{Id}");
        var optionStr = TeamCherry.Localization.Language.Get("OPT_1", $"Mod.{Id}/OptionsSheet");
        //...
    }
    //...
}
```

### Fallback/Neutral Language
If files for some languages are not provided, a mod may specify a language to be used as fallback.
The fallback language is provided through the `NeutralLanguageResources` attribute.

It can be provided in a csproj as such:
```xml
<ItemGroup>
    <AssemblyAttribute Include="System.Resources.NeutralResourcesLanguageAttribute">
        <_Parameter1>en</_Parameter1>
    </AssemblyAttribute>
</ItemGroup>
```
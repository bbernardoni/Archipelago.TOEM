using System.Linq;
using System.Reflection;
using Il2CppInterop.Runtime;
using UnityEngine;

namespace Archipelago.TOEM.UI;

public class AssetManager
{
    private Font _NaikaiFont;

    public byte[] ApLogoData;
    public Font NaikaiFont => _NaikaiFont ??= GetNaikaiFont();

    public AssetManager()
    {
        var res = Assembly.GetExecutingAssembly().GetManifestResourceStream("Archipelago.TOEM.assets.aplogo.png");
        ApLogoData = new byte[res.Length];
        res.Read(ApLogoData, 0, ApLogoData.Length);
    }

    private static Font GetNaikaiFont()
    {
        var allFonts = Object.FindObjectsOfTypeAll(Il2CppType.Of<Font>());
        return allFonts.First(font => font.name == "NaikaiFont-noHearts").Cast<Font>();
    }

}

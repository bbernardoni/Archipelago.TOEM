using System.Linq;
using System.Reflection;
using Il2CppInterop.Runtime;
using UnityEngine;

namespace Archipelago.TOEM.UI;

public class AssetManager
{
    private Font _NaikaiFont;
    private Material _UIShadow;

    public byte[] ApLogoData;
    public Font NaikaiFont => _NaikaiFont ??= GetFont("NaikaiFont-noHearts");
    public Material UIShadow => _UIShadow ??= GetMaterial("UIShadow");

    public AssetManager()
    {
        var res = Assembly.GetExecutingAssembly().GetManifestResourceStream("Archipelago.TOEM.assets.aplogo.png");
        ApLogoData = new byte[res.Length];
        res.Read(ApLogoData, 0, ApLogoData.Length);
    }

    private static Font GetFont(string fontName)
    {
        var allFonts = Object.FindObjectsOfTypeAll(Il2CppType.Of<Font>());
        return allFonts.First(font => font.name == fontName).Cast<Font>();
    }

    private static Material GetMaterial(string materialName)
    {
        var allMaterials = Object.FindObjectsOfTypeAll(Il2CppType.Of<Material>());
        return allMaterials.First(material => material.name == materialName).Cast<Material>();
    }

}

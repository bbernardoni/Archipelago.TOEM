using System.Linq;
using System.Reflection;
using Il2CppInterop.Runtime;
using UnityEngine;

namespace Archipelago.TOEM.UI;

public class AssetManager
{
    private Font _NaikaiFont;
    private Material _UIShadow;
    private Texture2D _ScrollBox;

    public byte[] ApLogoData;
    public Font NaikaiFont => _NaikaiFont ??= GetFont("NaikaiFont-noHearts");
    public Material UIShadow => _UIShadow ??= GetMaterial("UIShadow");
    public Texture2D ScrollBox => _ScrollBox ??= GetTexture2D("ScrollBox");

    public AssetManager()
    {
        var res = Assembly.GetExecutingAssembly().GetManifestResourceStream("Archipelago.TOEM.assets.aplogo.png");
        ApLogoData = new byte[res.Length];
        res.Read(ApLogoData, 0, ApLogoData.Length);
    }

    private static Font GetFont(string name)
    {
        var allFonts = Object.FindObjectsOfTypeAll(Il2CppType.Of<Font>());
        return allFonts.First(font => font.name == name).Cast<Font>();
    }

    private static Material GetMaterial(string name)
    {
        var allMaterials = Object.FindObjectsOfTypeAll(Il2CppType.Of<Material>());
        return allMaterials.First(material => material.name == name).Cast<Material>();
    }

    private static Texture2D GetTexture2D(string name)
    {
        var allTex2D = Object.FindObjectsOfTypeAll(Il2CppType.Of<Texture2D>());
        return allTex2D.First(tex => tex.name == name).Cast<Texture2D>();
    }
}

using System.Reflection;
using HarmonyLib;

namespace Archipelago.TOEM;

[HarmonyPatch]
public class AssetManager
{
    public byte[] ApLogoData;

    public AssetManager()
    {
        var res = Assembly.GetExecutingAssembly().GetManifestResourceStream("Archipelago.TOEM.assets.aplogo.png");
        ApLogoData = new byte[res.Length];
        res.Read(ApLogoData, 0, ApLogoData.Length);
    }

}

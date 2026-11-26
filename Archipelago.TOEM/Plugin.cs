using System.Reflection;
using Archipelago.TOEM.UI;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace Archipelago.TOEM;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin
{
    public const string Name = "TOEM: A Photo Adventure";
    public const string MinimumVersion = "2.2.0";

    public static ManualLogSource Logger { get; private set; }
    public static Client Client { get; private set; }
    public static State State { get; set; }
    public static SlotOptions Options => State.SlotData.Options;

    public static AssetManager AssetManager { get; set; }
    public static SaveFileManager SaveFileManager { get; set; }
    public static ItemManager ItemManager { get; set; }
    public static LocationManager LocationManager { get; set; }
    public static SceneManager SceneManager { get; set; }

    public override void Load()
    {
        Logger = Log;

        if (!ConfigManager.InitConfig(Config))
        {
            Logger.LogWarning("Archipelago disabled");
            return;
        }

        var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        harmony.PatchAll(Assembly.GetExecutingAssembly());

        Client = new();
        State = new();

        AssetManager = new();
        SaveFileManager = new();
        ItemManager = new();
        LocationManager = new();
        SceneManager = new();
        UIManager.Init();

        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} is loaded!");
    }
}

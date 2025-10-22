using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace Archipelago.TOEM;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin
{
    public static ManualLogSource Logger { get; private set; }
    public static Client.Client Client { get; private set; }
    public static State State { get; set; }
    public static Game Game { get; set; }

    private static ConfigEntry<string> _configUri;
    private static ConfigEntry<string> _configSlotName;
    private static ConfigEntry<string> _configPassword;
    private static ConfigEntry<bool> _configShowConnection;
    private static ConfigEntry<bool> _configShowConsole;

    public override void Load()
    {
        Logger = Log;

        var configEnabled = Config.Bind("Archipelago", "enabled", true, "Enable or disable the mod as a whole");

        _configUri = Config.Bind("Archipelago", "uri", "archipelago.gg:38281", "The address and port of the archipelago server to connect to");
        _configSlotName = Config.Bind("Archipelago", "slotName", "Player1", "The slot name of the player you are connecting as");
        _configPassword = Config.Bind("Archipelago", "password", "", "The password for the player you are connecting as");

        _configShowConnection = Config.Bind("UI", "showConnection", true, "Show or hide the AP connection info when connected");
        _configShowConsole = Config.Bind("UI", "showConsole", true, "Show or hide the AP message console at the top of the screen");

        if (!configEnabled.Value)
        {
            Logger.LogWarning("Archipelago disabled");
            return;
        }

        Settings.ShowConnection = _configShowConnection.Value;
        Settings.ShowConsole = _configShowConsole.Value;

        var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        harmony.PatchAll(Assembly.GetExecutingAssembly());

        State = new(_configUri.Value, _configSlotName.Value, _configPassword.Value);
        Game = new();
        Client = new();
        HUD.Initialize();

        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} is loaded!");
    }

    public static void UpdateConnectionInfo()
    {
        State.Uri = _configUri.Value;
        State.SlotName = _configSlotName.Value;
        State.Password = _configPassword.Value;
    }

    public static void UpdateConnectionInfo(string uri, string slotName, string password)
    {
        _configUri.Value = uri;
        _configSlotName.Value = slotName;
        _configPassword.Value = password;
        UpdateConnectionInfo();
    }

    public static void ToggleConnection()
    {
        var enabled = !Settings.ShowConnection;
        _configShowConnection.Value = enabled;
        Settings.ShowConnection = enabled;
    }

    public static void ToggleConsole()
    {
        var enabled = !Settings.ShowConsole;
        _configShowConsole.Value = enabled;
        Settings.ShowConsole = enabled;
    }
}

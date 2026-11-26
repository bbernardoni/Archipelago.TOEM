using BepInEx.Configuration;

namespace Archipelago.TOEM;

public class ConfigManager
{
    public static ConfigEntry<string> ConfigUri { get; set; }
    public static ConfigEntry<string> ConfigSlotName { get; set; }
    public static ConfigEntry<string> ConfigPassword { get; set; }


    public static bool InitConfig(ConfigFile Config)
    {
        var configEnabled = Config.Bind("Archipelago", "enabled", true, "Enable or disable the mod as a whole");
        ConfigUri = Config.Bind("Archipelago", "uri", "archipelago.gg:38281", "The address and port of the archipelago server to connect to");
        ConfigSlotName = Config.Bind("Archipelago", "slotName", "Player1", "The slot name of the player you are connecting as");
        ConfigPassword = Config.Bind("Archipelago", "password", "", "The password for the player you are connecting as");

        return configEnabled.Value;
    }

    public static void UpdateConnectionInfo()
    {
        ConfigUri.Value = Plugin.State.Uri;
        ConfigSlotName.Value = Plugin.State.SlotName;
        ConfigPassword.Value = Plugin.State.Password;
    }
}

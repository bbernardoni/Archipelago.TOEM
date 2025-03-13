using System;
using Archipelago.TOEM.Client;
using UnityEngine;

namespace Archipelago.TOEM;

public class HUD : MonoBehaviour
{
    public const string ModDisplayInfo = $"{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION}";

    public static bool ConnectionFocused { get; private set; }

    public static void Initialize(Plugin plugin)
    {
        var component = plugin.AddComponent<HUD>();
        component.hideFlags = HideFlags.HideAndDontSave;
        DontDestroyOnLoad(component.gameObject);
        Cursor.visible = true;
    }

    private void Start()
    {
        ClientConsole.Awake();
    }

    private void OnGUI()
    {
        ClientConsole.OnGUI();

        if (Plugin.Client.Connected)
        {
            ConnectionFocused = false;

            if (!Settings.ShowConnection)
            {
                return;
            }

            GUI.BeginGroup(new(Screen.width - 304, Screen.height - 52, 304, 48));
            GUI.Box(new(0, 0, 300, 48), "");
            GUI.Label(new(4, 0, 300, 20), $"{ModDisplayInfo} (F1 for Debug)");
            GUI.Label(new(4, 24, 300, 20), "Archipelago Status: Connected");
            GUI.EndGroup();
            return;
        }

        GUI.BeginGroup(new(Screen.width - 308, Screen.height - 132, 308, 128));

        GUI.Box(new(0, 0, 300, 124), "");

        GUI.Label(new(4, 0, 300, 20), ModDisplayInfo);
        GUI.Label(new(4, 20, 300, 20), "Archipelago Status: Disconnected");
        GUI.Label(new(4, 40, 150, 20), "Host: ");
        GUI.Label(new(4, 60, 150, 20), "Player Name: ");
        GUI.Label(new(4, 80, 150, 20), "Password: ");

        // var e = Event.current;
        // var control = GUI.GetNameOfFocusedControl();
        // var pressedEnter = e.type == EventType.KeyUp &&
        //                     control is "uri" or "slotName" or "password" &&
        //                     e.keyCode is KeyCode.KeypadEnter or KeyCode.Return;

        // ConnectionFocused = control is "uri" or "slotName" or "password";

        try
        {
            // GUI.SetNextControlName("uri");
            // var uri = GUI.TextField(new(134, 40, 150, 20), ArchipelagoPlugin.State.Uri);
            // GUI.SetNextControlName("slotName");
            // var slotName = GUI.TextField(new(134, 60, 150, 20), ArchipelagoPlugin.State.SlotName);
            // GUI.SetNextControlName("password");
            // var password = GUI.PasswordField(new(134, 80, 150, 20), ArchipelagoPlugin.State.Password, "*"[0]);
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError(e);
        }
        finally
        {
            GUI.EndGroup();
        }
        // ArchipelagoPlugin.UpdateConnectionInfo(uri, slotName, password);

        // var pressedButton = GUI.Button(new(4, 100, 100, 20), "Connect");

        // if (pressedEnter || pressedButton)
        // {
        //     ArchipelagoPlugin.Client.Connect();
        // }

        // GUI.EndGroup();
    }
}

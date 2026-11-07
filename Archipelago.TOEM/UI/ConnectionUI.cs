using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Archipelago.TOEM.UI;

[HarmonyPatch]
public class ConnectionUI
{
    private static Button ConnectButton = null;
    
    public static Inputbox CreateInput(string name, GameObject parent)
    {
        var labelHolder = new GameObject(name + " Label");
        labelHolder.transform.SetParent(parent.transform, false);
        labelHolder.AddComponent<RectTransform>();
        var lhElem = labelHolder.AddComponent<LayoutElement>();
        lhElem.preferredHeight = 90;
        
        var labelText = new GameObject("LabelText");
        labelText.transform.SetParent(labelHolder.transform, false);
        var ltRect = labelText.AddComponent<RectTransform>();
        ltRect.anchorMin = Vector2.zero;
        ltRect.anchorMax = Vector2.one;
        //ltRect.anchoredPosition = new Vector2(32, 0);
        ltRect.sizeDelta = Vector2.zero;
        var ltMesh = labelText.AddComponent<SuperTextMesh>();
        ltMesh.size = 70;
        ltMesh.text = name;
        ltMesh.anchor = TextAnchor.MiddleLeft;
        ltMesh.textMaterial = Plugin.AssetManager.UIShadow;

        return new Inputbox(name, parent)
        {
            selectable = () => !Plugin.Client.Connected
        };
    }

    [HarmonyPostfix, HarmonyPatch(typeof(TitleScreenMenu), nameof(TitleScreenMenu.OnMenuOpen))]
    public static void OnMenuOpen_Postfix(TitleScreenMenu __instance)
    {
        __instance.hasSaveFile = true;
        __instance.SetupTitleScreen(true, false);
        __instance.displayPlaytime.gameObject.active = false;
        // Hide New adventure button
        __instance.mainGrid.gameObject.transform.GetChild(2).gameObject.active = false;
        
        __instance.versionText.text = $"{__instance.versionLocalizedString.ToString()} {Application.version} (Toem), {MyPluginInfo.PLUGIN_VERSION} (AP)";

        var mainTransform = __instance.continueGameButtonContainer.transform;
        if(mainTransform.childCount == 3)
        {
            var connectionGroup = new GameObject("Connection Group");
            connectionGroup.transform.SetParent(mainTransform, false);
            var cgRect = connectionGroup.AddComponent<RectTransform>();
            cgRect.anchorMin = new Vector2(1, 0.1f);
            cgRect.anchorMax = new Vector2(1, 0.85f);
            cgRect.anchoredPosition = new Vector2(-63, 0);
            cgRect.pivot = new Vector2(1, 0.5f);
            cgRect.sizeDelta = new Vector2(750, 0);
            var cgVert = connectionGroup.AddComponent<VerticalLayoutGroup>();
            cgVert.childForceExpandHeight = false;
            
            var topButton = __instance.topButton.gameObject;
            var buttonSprite = topButton.GetComponent<Image>().sprite;
            
            var hostInput = CreateInput("Hostname:Port", connectionGroup);
            hostInput.SetText(Plugin.State.Uri);
            hostInput.onDeselect = text => Plugin.State.Uri = text;
            var slotInput = CreateInput("Player Name", connectionGroup);
            slotInput.SetText(Plugin.State.SlotName);
            slotInput.onDeselect = text => Plugin.State.SlotName = text;
            slotInput.textRect.anchoredPosition = new Vector2(32, -10);
            slotInput.overrideFont = Plugin.AssetManager.NaikaiFont;
            var passwordInput = CreateInput("Password", connectionGroup);
            passwordInput.SetText(Plugin.State.Password);
            passwordInput.onDeselect = text => Plugin.State.Password = text;
            passwordInput.password = true;

            var spacer = new GameObject("Spacer");
            spacer.transform.SetParent(connectionGroup.transform, false);
            spacer.AddComponent<RectTransform>();
            var spacerElem = spacer.AddComponent<LayoutElement>();
            spacerElem.preferredHeight = 60;
            
            ConnectButton = new Button("Connect", connectionGroup);
            ConnectButton.button.onClick.AddListener((UnityAction)ToggleConnection);
        }
    }

    public static void ToggleConnection()
    {
        if (Plugin.Client.Connected)
            Plugin.Client.Disconnect();
        else
            Plugin.Client.Connect();
    }

    [HarmonyPrefix, HarmonyPatch(typeof(TitleScreenMenu), nameof(TitleScreenMenu.Update))]
    public static void Update(TitleScreenMenu __instance)
    {
        Inputbox.UpdateAll();

        // Set connect button text
        var continueButton = __instance.mainGrid.gameObject.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<SuperTextMesh>();
        if (Plugin.Client.Connected)
        {
            ConnectButton.text.text = "Disconnect";
            continueButton.text = "<w=sassy>Start";
        }
        else
        {
            ConnectButton.text.text = "Connect";
            continueButton.text = "Connect & Start";
            if (SaveFileManager.GetRootFolder() != "Save")
            {
                Plugin.Logger.LogInfo("Reset SetRootFolder");
                SaveFileManager.SetRootFolder("Save");
            }
        }
    }
}

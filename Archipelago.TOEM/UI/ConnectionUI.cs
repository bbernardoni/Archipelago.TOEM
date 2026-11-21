using HarmonyLib;
using UnityEngine;
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
        
        var labelText = new Text(name, labelHolder);
        labelText.textRect.anchoredPosition = Vector2.zero;

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
            hostInput.Text = Plugin.State.Uri;
            hostInput.onDeselect = text => Plugin.State.Uri = text;
            var slotInput = CreateInput("Player Name", connectionGroup);
            slotInput.Text = Plugin.State.SlotName;
            slotInput.onDeselect = text => Plugin.State.SlotName = text;
            slotInput.textObj.textRect.anchoredPosition = new Vector2(32, -10);
            slotInput.textObj.overrideFont = Plugin.AssetManager.NaikaiFont;
            var passwordInput = CreateInput("Password", connectionGroup);
            passwordInput.Text = Plugin.State.Password;
            passwordInput.onDeselect = text => Plugin.State.Password = text;
            passwordInput.password = true;
            hostInput.onDown = slotInput.Select;
            hostInput.onEnter = slotInput.Select;
            slotInput.onUp = hostInput.Select;
            slotInput.onDown = passwordInput.Select;
            slotInput.onEnter = passwordInput.Select;
            passwordInput.onUp = slotInput.Select;
            passwordInput.onDown = () =>
            {
                passwordInput.Deselect();
                UiButtonSystem.Instance.SetTargetButton(ConnectButton.button);
            };
            passwordInput.onEnter = passwordInput.onDown;

            var spacer = new GameObject("Spacer");
            spacer.transform.SetParent(connectionGroup.transform, false);
            spacer.AddComponent<RectTransform>();
            var spacerElem = spacer.AddComponent<LayoutElement>();
            spacerElem.preferredHeight = 60;
            
            ConnectButton = new Button("Connect", connectionGroup);
            ConnectButton.AddOnClick(ToggleConnection);
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
        // Set connect button text
        var continueButton = __instance.mainGrid.gameObject.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<SuperTextMesh>();
        if (Plugin.Client.Connected)
        {
            ConnectButton.text.textMesh.text = "Disconnect";
            continueButton.text = "<w=sassy>Start";
        }
        else
        {
            ConnectButton.text.textMesh.text = "Connect";
            continueButton.text = "Connect & Start";
            if (SaveFileManager.GetRootFolder() != "Save")
            {
                Plugin.Logger.LogInfo("Reset SetRootFolder");
                SaveFileManager.SetRootFolder("Save");
            }
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using Archipelago.TOEM.Client;
using UnityEngine;
using UnityEngine.UI;
using UniverseLib;
using UniverseLib.Input;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.Utility;

namespace Archipelago.TOEM;

public class HUD : UniverseLib.UI.Panels.PanelBase
{
    public const string ModDisplayInfo = $"{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION}";

    public static UIBase UiBase { get; private set; }

    public static void Initialize()
    {
        UniverseLib.Config.UniverseLibConfig config = new()
        {
            Force_Unlock_Mouse = true
        };
        Universe.Init(1f, LateInit, Log, config);
    }

    static void LateInit()
    {
        UiBase = UniversalUI.RegisterUI(MyPluginInfo.PLUGIN_GUID, UiUpdate);
        HUD HUDPanel = new(UiBase);
        ClientConsole ClientPanel = new(UiBase);
    }

    static void UiUpdate()
    {
        // Called once per frame when your UI is being displayed.
    }

    public HUD(UIBase owner) : base(owner) { }

    public override string Name => "Connection Info";
    public override int MinWidth => 300;
    public override int MinHeight => 48;
    public override Vector2 DefaultAnchorMin => new(0f, 0.85f);
    public override Vector2 DefaultAnchorMax => new(0.15f, 1f);
    public override bool CanDragAndResize => false;

    private GameObject ConnectedUIObject;
    private GameObject DisconnectedUIObject;
    private static bool InputWasFocused;
    private static List<InputFieldRef> Inputs;

    protected override void ConstructPanelContent()
    {
        UIRoot.GetComponent<Image>().enabled = false;
        UIRoot.GetComponent<VerticalLayoutGroup>().childForceExpandHeight = false;
        ContentRoot.GetComponent<Image>().color = new(0f, 0f, 0f, 0.5f);
        ContentRoot.GetComponent<VerticalLayoutGroup>().padding = new(10, 10, 10, 10);
        ContentRoot.GetComponent<VerticalLayoutGroup>().spacing = 5;
        ContentRoot.GetComponent<LayoutElement>().flexibleHeight = 0;
        ContentRoot.GetComponent<RectTransform>().pivot = new(0f, 1f);

        Text modDisplayInfoText = UIFactory.CreateLabel(ContentRoot, "ModDisplayInfo", ModDisplayInfo, TextAnchor.MiddleCenter);
        UIFactory.SetLayoutElement(modDisplayInfoText.gameObject, minHeight: 16, flexibleWidth: 9999);

        ConnectedUIObject = UIFactory.CreateVerticalGroup(ContentRoot, "ConnectedUI", false, false, true, true, 5);
        ConnectedUIObject.GetComponent<Image>().enabled = false;
        ConnectedUIObject.SetActive(false);
        UIFactory.SetLayoutElement(ConnectedUIObject.gameObject);
        Text connectedText = UIFactory.CreateLabel(ConnectedUIObject, "Status", "Archipelago Status: Connected");
        UIFactory.SetLayoutElement(connectedText.gameObject, minHeight: 16);

        DisconnectedUIObject = UIFactory.CreateVerticalGroup(ContentRoot, "DisconnectedUI", false, false, true, true, 5);
        DisconnectedUIObject.GetComponent<Image>().enabled = false;
        UIFactory.SetLayoutElement(DisconnectedUIObject);
        Text disconnectedText = UIFactory.CreateLabel(DisconnectedUIObject, "Status", "Archipelago Status: Disconnected");
        UIFactory.SetLayoutElement(disconnectedText.gameObject, minHeight: 16);

        var hostGroup = UIFactory.CreateHorizontalGroup(DisconnectedUIObject, "HostGroup", false, false, true, true, 5, childAlignment: TextAnchor.MiddleLeft);
        hostGroup.GetComponent<Image>().enabled = false;
        UIFactory.SetLayoutElement(hostGroup, flexibleWidth: 9999);
        Text hostText = UIFactory.CreateLabel(hostGroup, "HostText", "Host: ");
        UIFactory.SetLayoutElement(hostText.gameObject);
        var hostInput = UIFactory.CreateInputField(hostGroup, "HostInput", "hostname");
        hostInput.UIRoot.transform.GetChild(0).GetComponent<RectTransform>().anchoredPosition = new(5, 0);
        UIFactory.SetLayoutElement(hostInput.UIRoot, minHeight: 20, flexibleWidth: 9999);
        hostInput.Component.text = Plugin.State.Uri;
        hostInput.Component.GetOnEndEdit().AddListener(OnHostInput);

        var playerNameGroup = UIFactory.CreateHorizontalGroup(DisconnectedUIObject, "PlayerNameGroup", false, false, true, true, 5, childAlignment: TextAnchor.MiddleLeft);
        playerNameGroup.GetComponent<Image>().enabled = false;
        UIFactory.SetLayoutElement(playerNameGroup, flexibleWidth: 9999);
        Text playerNameText = UIFactory.CreateLabel(playerNameGroup, "PlayerNameText", "Player Name: ");
        UIFactory.SetLayoutElement(playerNameText.gameObject);
        var playerNameInput = UIFactory.CreateInputField(playerNameGroup, "PlayerNameInput", "player name");
        playerNameInput.UIRoot.transform.GetChild(0).GetComponent<RectTransform>().anchoredPosition = new(5, 0);
        UIFactory.SetLayoutElement(playerNameInput.UIRoot, minHeight: 20, flexibleWidth: 9999);
        playerNameInput.Component.text = Plugin.State.SlotName;
        playerNameInput.Component.GetOnEndEdit().AddListener(OnPlayerNameInput);

        var passwordGroup = UIFactory.CreateHorizontalGroup(DisconnectedUIObject, "PasswordGroup", false, false, true, true, 5, childAlignment: TextAnchor.MiddleLeft);
        passwordGroup.GetComponent<Image>().enabled = false;
        UIFactory.SetLayoutElement(passwordGroup, flexibleWidth: 9999);
        Text passwordText = UIFactory.CreateLabel(passwordGroup, "PasswordText", "Password: ");
        UIFactory.SetLayoutElement(passwordText.gameObject);
        var passwordInput = UIFactory.CreateInputField(passwordGroup, "PasswordInput", "password");
        passwordInput.UIRoot.transform.GetChild(0).GetComponent<RectTransform>().anchoredPosition = new(5, 0);
        passwordInput.Component.contentType = InputField.ContentType.Password;
        UIFactory.SetLayoutElement(passwordInput.UIRoot, minHeight: 20, flexibleWidth: 9999);
        passwordInput.Component.text = Plugin.State.Password;
        passwordInput.Component.GetOnEndEdit().AddListener(OnPasswordInput);

        Inputs = [hostInput, playerNameInput, passwordInput];

        var connectButton = UIFactory.CreateButton(DisconnectedUIObject, "ConnectButton", "Connect");
        UIFactory.SetLayoutElement(connectButton.GameObject, minHeight: 25, flexibleWidth: 9999);
        connectButton.OnClick += Plugin.Client.Connect;

        foreach (var rect in ContentRoot.GetComponentsInChildren<RectTransform>())
        {
            rect.pivot = new(0f, 1f);
        }
    }

    public override void Update()
    {
        ConnectedUIObject.SetActive(Plugin.Client.Connected && Settings.ShowConnection);
        DisconnectedUIObject.SetActive(!Plugin.Client.Connected);

        if (InputManager.GetMouseButtonDown(0))
        {
            foreach (var input in Inputs)
            {
                if (input.Component.isFocused && !RectContainsPoint(input.Transform, InputManager.MousePosition))
                    input.Component.DeactivateInputField();
            }
        }

        bool InputIsFocused = Inputs.Any(i => i.Component.isFocused);
        if (InputWasFocused != InputIsFocused)
            OurInputManager.playerHasControl = !InputIsFocused;
        InputWasFocused = InputIsFocused;
    }

    private void OnHostInput(string input)
    {
        Plugin.State.Uri = input;
    }

    private void OnPlayerNameInput(string input)
    {
        Plugin.State.SlotName = input;
    }

    private void OnPasswordInput(string input)
    {
        Plugin.State.Password = input;
    }

    public override void SetDefaultSizeAndPosition()
    {
        Rect.position = new(4f, UiBase.Canvas.pixelRect.height - 4);
        Rect.pivot = new Vector2(0f, 1f);

        Rect.anchorMin = DefaultAnchorMin;
        Rect.anchorMax = DefaultAnchorMax;

        LayoutRebuilder.ForceRebuildLayoutImmediate(this.Rect);

        EnsureValidPosition();
        EnsureValidSize();

        Dragger.OnEndResize();
    }

    private static void Log(object message, LogType logType)
    {
        string log = message?.ToString() ?? "";

        switch (logType)
        {
            case LogType.Assert:
            case LogType.Log:
                Plugin.Logger.LogMessage(log);
                break;

            case LogType.Warning:
                Plugin.Logger.LogWarning(log);
                break;

            case LogType.Error:
            case LogType.Exception:
                Plugin.Logger.LogError(log);
                break;
        }
    }

    public static bool RectContainsPoint(RectTransform rect, Vector2 point)
    {
        return point.x >= rect.position.x && point.x <= (rect.position.x + rect.rect.width) &&
               point.y <= rect.position.y && point.y >= (rect.position.y - rect.rect.height);
    }
}

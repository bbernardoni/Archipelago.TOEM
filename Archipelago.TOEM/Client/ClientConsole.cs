using System.Collections.Generic;
using Quests;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniverseLib.Input;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.UI.Widgets;
using UniverseLib;

namespace Archipelago.TOEM.Client;

// adapted from oc2-modding https://github.com/toasterparty/oc2-modding/blob/main/OC2Modding/GameLog.cs
public class ClientConsole(UIBase owner) : UniverseLib.UI.Panels.PanelBase(owner)
{
    public override string Name => "Client Console";
    public override int MinWidth => 300;
    public override int MinHeight => 48;
    public override Vector2 DefaultAnchorMin => new(0.3f, 0.5f);
    public override Vector2 DefaultAnchorMax => new(0.7f, 1.0f);
    public override bool CanDragAndResize => false;
    
    public static bool Hidden { get; private set; } = true;

    private static readonly List<string> LogLines = [];
    private static float LastUpdateTime = -HideTimeout;
    private static int UpdatedLogLines = 0;
    private const int MaxLogLines = 80;
    private const float HideTimeout = 15f;

    private static GameObject scrollViewGroup;
    private static GameObject shortGroup;
    private static GameObject scrollView;
    private static Text scrollText;
    private static Text shortText;
    private static Text buttonText;
    private static InputFieldRef commandInput;
    private static bool commandInputWasFocused;
    private static ButtonRef dummyButton;

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
        ClientConsole ClientPanel = new(UiBase);
    }

    static void UiUpdate()
    {
        // Called once per frame when your UI is being displayed.
    }

    protected override void ConstructPanelContent()
    {
        UIRoot.GetComponent<Image>().enabled = false;
        UIRoot.GetComponent<VerticalLayoutGroup>().childForceExpandHeight = false;
        UIRoot.GetComponent<VerticalLayoutGroup>().padding = new(0, 0, 0, 0);
        ContentRoot.GetComponent<Image>().enabled = false;
        ContentRoot.GetComponent<VerticalLayoutGroup>().padding = new(0, 0, 0, 0);
        ContentRoot.GetComponent<VerticalLayoutGroup>().spacing = 5;
        ContentRoot.GetComponent<LayoutElement>().flexibleHeight = 0;
        ContentRoot.GetComponent<RectTransform>().pivot = new(0f, 1f);

        var consoleGroup = UIFactory.CreateHorizontalGroup(ContentRoot, "ConsoleGroup", false, false, true, true, 5, childAlignment: TextAnchor.UpperRight);
        consoleGroup.GetComponent<Image>().enabled = false;
        UIFactory.SetLayoutElement(consoleGroup, flexibleWidth: 9999);

        scrollViewGroup = UIFactory.CreateVerticalGroup(consoleGroup, "ScrollViewGroup", false, false, true, true, 5, new(5f, 5f, 5f, 5f));
        UIFactory.SetLayoutElement(scrollViewGroup, flexibleWidth: 9999);
        scrollViewGroup.GetComponent<Image>().color = new(0f, 0f, 0f, 0.5f);
        scrollView = UIFactory.CreateScrollView(scrollViewGroup, "ConsoleScrollView", out GameObject scrollContent, out AutoSliderScrollbar scrollbar);
        scrollView.GetComponent<Image>().enabled = false;
        int scrollHeight = (int)(UiBase.Canvas.pixelRect.height * 0.3);
        UIFactory.SetLayoutElement(scrollView, minHeight: scrollHeight, flexibleWidth: 9999);
        UIFactory.SetLayoutGroup<VerticalLayoutGroup>(scrollContent, spacing: 5, padTop: 5, padBottom: 5, padLeft: 5, padRight: 5);
        // IDK why this is necessary but I need to toggle the scroll bad to get it rendering correctly
        scrollbar.SetActive(false);
        scrollbar.SetActive(true);
        scrollText = UIFactory.CreateLabel(scrollContent, "ConsoleScrollText", "", TextAnchor.UpperLeft, fontSize: 30);
        UIFactory.SetLayoutElement(scrollText.gameObject);

        var commandGroup = UIFactory.CreateHorizontalGroup(scrollViewGroup, "CommandGroup", false, false, true, true, 5, childAlignment: TextAnchor.MiddleLeft);
        commandGroup.GetComponent<Image>().enabled = false;
        UIFactory.SetLayoutElement(commandGroup, flexibleWidth: 9999);
        commandInput = UIFactory.CreateInputField(commandGroup, "CommandInput", "Command");
        commandInput.UIRoot.transform.GetChild(0).GetComponent<RectTransform>().anchoredPosition = new(5, 0);
        foreach (var text in commandInput.UIRoot.GetComponentsInChildren<Text>())
            text.fontSize = 30;
        UIFactory.SetLayoutElement(commandInput.UIRoot, minHeight: 43, flexibleWidth: 9999);
        var commandButton = UIFactory.CreateButton(commandGroup, "CommandButton", "Send", normalColor: new(0.15f, 0.15f, 0.15f));
        var commandButtonText = commandButton.GameObject.transform.GetChild(0).GetComponent<Text>();
        commandButtonText.fontSize = 20;
        UIFactory.SetLayoutElement(commandButton.GameObject, minHeight: 43, minWidth: 80);
        commandButton.OnClick += SendCommand;
        scrollViewGroup.SetActive(false);

        shortGroup = UIFactory.CreateVerticalGroup(consoleGroup, "ShortTextGroup", false, false, true, true, 5, new(5f, 5f, 5f, 5f));
        UIFactory.SetLayoutElement(shortGroup, minHeight: 43, preferredHeight: 43, flexibleWidth: 9999);
        shortGroup.GetComponent<Image>().color = new(0f, 0f, 0f, 0.5f);
        shortText = UIFactory.CreateLabel(shortGroup, "ConsoleText", "", TextAnchor.UpperLeft, fontSize: 30);
        UIFactory.SetLayoutElement(shortText.gameObject);
        shortText.gameObject.GetComponent<RectTransform>().pivot = new(0f, 1f);
        shortGroup.SetActive(false);

        var showButton = UIFactory.CreateButton(consoleGroup, "ShowButton", "Show");
        buttonText = showButton.GameObject.transform.GetChild(0).GetComponent<Text>();
        buttonText.fontSize = 20;
        UIFactory.SetLayoutElement(showButton.GameObject, minHeight: 43, minWidth: 80);
        showButton.OnClick += ToggleConsole;

        dummyButton = UIFactory.CreateButton(consoleGroup, "DummyButton", "");

        foreach (var rect in ContentRoot.GetComponentsInChildren<RectTransform>())
            rect.pivot = new(0f, 1f);
        foreach (var rect in scrollViewGroup.GetComponentsInChildren<RectTransform>())
            rect.pivot = new(0f, 1f);
        foreach (var rect in scrollView.transform.GetChild(1).GetComponentsInChildren<RectTransform>())
            rect.pivot = new(0.5f, 0.5f);
    }

    public static void ToggleConsole()
    {
        Hidden = !Hidden;
        buttonText.text = Hidden ? "Show" : "Hide";
    }

    public static void SendCommand()
    {
        string command = commandInput.Text;
        if (!string.IsNullOrWhiteSpace(command))
        {
            if (command[0] == '/')
            {
                LogMessage(command);
                ExecuteCommand(command[1..]);
            }
            else if (Plugin.Client.Connected)
            {
                Plugin.Client.SendMessage(command);
            }
        }
        commandInput.Text = "";
    }

    public static void ExecuteCommand(string command_str)
    {
        string[] command = command_str.Split(" ");
        if(command[0] == "help")
        {
            LogMessage("Use '!help' for server commands.");
            LogMessage("/version");
            LogMessage("    Print out the mod version");
            LogMessage("/connect");
            LogMessage("    Connect to slot with previously entered credentials");
            LogMessage("/disconnect");
            LogMessage("    Disconnect from current slot");
        }
        else if(command[0] == "tp")
            Plugin.SceneManager.TpCommand(command);
        else if(command[0] == "turbo")
        {
            int newSpeed = 20;
            if(command.Length > 1)
                int.TryParse(command[1], out newSpeed);

            PlayerController.Instance.moveSpeed = newSpeed;
        }
        else if(command[0] == "intro")
        {
            List<string> introKeys = ["Got Bag", "Grandma Bye", "Got Camera", "Grandma Intro", "Kiosky Intro"];
            foreach (var key in GameManager.QuestDatabase.saveKeys)
            {
                if (introKeys.Contains(key.jsonSaveKey))
                    key.SetTrue();
            }
            foreach (var quest in GameManager.QuestDatabase.homeRegionQuests)
            {
                if (quest.jsonSaveKey == "Kiosky - Home" && quest.currentStatus == Quest.QuestStatus.Undiscovered)
                    quest.SetStatus(Quest.QuestStatus.Ongoing);
            }
            Plugin.ItemManager.GiveGameItem("Bag");
            Plugin.ItemManager.GiveGameItem("Camera");
            Plugin.ItemManager.GiveGameItem("Album");
            Plugin.ItemManager.GiveGameItem("CommunityCard");
        }
        else if(command[0] == "connect")
            Plugin.Client.Connect();
        else if(command[0] == "disconnect")
            Plugin.Client.Disconnect();
        else if(command[0] == "version")
            LogMessage($"Toem AP mod version {MyPluginInfo.PLUGIN_VERSION}");
        else if(command[0] == "asdf")
        {
            int num = 0;
            if(command.Length > 1)
                int.TryParse(command[1], out num);
            
        }
        else
            LogMessage("Unknown command. See /help for list of supported commands.");
    }

    public static void LogMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        if (LogLines.Count == MaxLogLines)
        {
            LogLines.RemoveAt(0);
        }

        LogLines.Add(message);
        LastUpdateTime = Time.time;
        UpdatedLogLines = 2;
    }
    
    public static bool RectContainsPoint(RectTransform rect, Vector2 point)
    {
        return point.x >= rect.position.x && point.x <= (rect.position.x + rect.rect.width) &&
               point.y <= rect.position.y && point.y >= (rect.position.y - rect.rect.height);
    }

    public override void Update()
    {
        bool oldScrollViewActive = scrollViewGroup.active;
        scrollViewGroup.SetActive(!Hidden);
        shortGroup.SetActive(Hidden && Time.time - LastUpdateTime < HideTimeout);

        if (!oldScrollViewActive && scrollViewGroup.active)
        {
            commandInput.Component.Select();
            commandInput.Component.ActivateInputField();
        }
        else if(oldScrollViewActive && !scrollViewGroup.active)
        {
            // dumb hack to deselect the hide/show button
            dummyButton.Component.Select();
        }

        if (LogLines.Count > 0)
        {
            shortText.text = LogLines[^1];
            string scrollString = "";
            for (var i = 0; i < LogLines.Count; i++)
            {
                scrollString += LogLines.ElementAt(i);
                if (i < LogLines.Count - 1)
                {
                    scrollString += "\n";
                }
            }
            scrollText.text = scrollString;
            if (UpdatedLogLines > 0)
            {
                UpdatedLogLines--;
                if  (UpdatedLogLines == 0)
                {
                    scrollView.GetComponent<ScrollRect>().SetVerticalNormalizedPosition(0f);
                }
            }
        }
        else
        {
            shortText.text = "";
            scrollText.text = "";
        }

        bool enter = InputManager.GetKeyDown(KeyCode.Return) || InputManager.GetKeyDown(KeyCode.KeypadEnter);
        if (commandInputWasFocused && enter)
            SendCommand();

        if (InputManager.GetMouseButtonDown(0) && commandInput.Component.isFocused &&
                !RectContainsPoint(commandInput.Transform, InputManager.MousePosition))
            commandInput.Component.DeactivateInputField();

        if (commandInputWasFocused != commandInput.Component.isFocused)
            OurInputManager.playerHasControl = !commandInput.Component.isFocused;
        commandInputWasFocused = commandInput.Component.isFocused;
    }

    public override void SetDefaultSizeAndPosition()
    {
        Rect.position = new(UiBase.Canvas.pixelRect.width * 0.3f, UiBase.Canvas.pixelRect.height);
        Rect.pivot = new Vector2(0f, 1f);

        Rect.anchorMin = DefaultAnchorMin;
        Rect.anchorMax = DefaultAnchorMax;

        LayoutRebuilder.ForceRebuildLayoutImmediate(Rect);

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
}

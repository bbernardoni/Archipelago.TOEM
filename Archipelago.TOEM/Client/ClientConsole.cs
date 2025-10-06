using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniverseLib;
using UniverseLib.UI;
using UniverseLib.UI.Widgets;

namespace Archipelago.TOEM.Client;

// adapted from oc2-modding https://github.com/toasterparty/oc2-modding/blob/main/OC2Modding/GameLog.cs
public class ClientConsole : UniverseLib.UI.Panels.PanelBase
{
    public ClientConsole(UIBase owner) : base(owner) { }

    public override string Name => "Client Console";
    public override int MinWidth => 300;
    public override int MinHeight => 48;
    public override Vector2 DefaultAnchorMin => new(0f, 0.85f);
    public override Vector2 DefaultAnchorMax => new(0.15f, 1f);
    public override bool CanDragAndResize => false;

    protected override void ConstructPanelContent()
    {
        UIRoot.GetComponent<Image>().enabled = false;
        UIRoot.GetComponent<VerticalLayoutGroup>().childForceExpandHeight = false;
        ContentRoot.GetComponent<Image>().color = new(0f, 0f, 0f, 0.5f);
        ContentRoot.GetComponent<VerticalLayoutGroup>().padding = new(10, 10, 10, 10);
        ContentRoot.GetComponent<VerticalLayoutGroup>().spacing = 5;
        ContentRoot.GetComponent<LayoutElement>().flexibleHeight = 0;
        ContentRoot.GetComponent<RectTransform>().pivot = new(0f, 1f);

        GameObject scrollObj = UIFactory.CreateScrollView(ContentRoot, "ConsoleScrollView", out GameObject scrollContent, out AutoSliderScrollbar scrollbar,
            new Color(0f, 0f, 0f, 0.5f));
        UIFactory.SetLayoutElement(scrollObj, minHeight: 250, preferredHeight: 300, flexibleHeight: 0, flexibleWidth: 9999);
        Text consoleText = UIFactory.CreateLabel(scrollContent, "ConsoleText", "Hello World\n1\n1\n1\n1\n1\n1\n1\n1\n1\n1\n1\n5");
        UIFactory.SetLayoutElement(consoleText.gameObject, minHeight: 100);


        foreach (var rect in ContentRoot.GetComponentsInChildren<RectTransform>())
        {
            rect.pivot = new(0f, 1f);
        }
    }

    public override void Update()
    {
    }
}

public static class OldClientConsole
{
    public static bool Hidden { get; private set; } = true;

    private static readonly List<string> LogLines = [];
    private static Vector2 _scrollView;
    private static Rect _window;
    private static Rect _scroll;
    private static Rect _text;
    private static Rect _hideShowButton;

    private static readonly GUIStyle TextStyle = new();
    private static string _scrollText = "";
    private static float _lastUpdateTime = Time.time;
    private const int MaxLogLines = 80;
    private const float HideTimeout = 15f;

    private static string _commandText = "";
    private static Rect _commandTextRect;
    private static Rect _sendCommandButton;

    public static void Awake()
    {
        UpdateWindow();
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
        _lastUpdateTime = Time.time;
        UpdateWindow();
    }

    public static void OnGUI()
    {
        if (!Settings.ShowConsole || LogLines.Count == 0)
        {
            return;
        }

        if (!Hidden || Time.time - _lastUpdateTime < HideTimeout)
        {
            _scrollView = GUI.BeginScrollView(_window, _scrollView, _scroll);
            GUI.Box(_text, "");
            GUI.Box(_text, _scrollText, TextStyle);
            GUI.EndScrollView();
        }

        if (GUI.Button(_hideShowButton, Hidden ? "Show" : "Hide"))
        {
            Hidden = !Hidden;
            UpdateWindow();
            if (!Hidden)
            {
                GUI.FocusControl("message");
            }
        }

        // draw client/server commands entry
        if (Hidden || !Plugin.Client.Connected)
        {
            return;
        }

        /*
        var e = Event.current;
        var control = GUI.GetNameOfFocusedControl();
        var pressedEnter = e.type == EventType.KeyDown && control == "message" &&
                           e.keyCode is KeyCode.KeypadEnter or KeyCode.Return;

        GUI.SetNextControlName("message");
        _commandText = GUI.TextField(_commandTextRect, _commandText);
        var pressedButton = GUI.Button(_sendCommandButton, "Send");
        if (!string.IsNullOrWhiteSpace(_commandText) && (pressedButton || pressedEnter))
        {
            Plugin.Client.SendMessage(_commandText);
            _commandText = "";
        }
        */
    }

    public static void UpdateWindow()
    {
        _scrollText = "";

        if (Hidden)
        {
            if (LogLines.Count > 0)
            {
                _scrollText = LogLines[^1];
            }
        }
        else
        {
            for (var i = 0; i < LogLines.Count; i++)
            {
                _scrollText += LogLines.ElementAt(i);
                if (i < LogLines.Count - 1)
                {
                    _scrollText += "\n";
                }
            }
        }

        var width = (int)(Screen.width * 0.4f);
        int height;
        int scrollDepth;
        if (Hidden)
        {
            height = (int)(Screen.height * 0.03f);
            scrollDepth = height;
        }
        else
        {
            height = (int)(Screen.height * 0.3f);
            scrollDepth = height * 10;
        }

        _window = new(Screen.width / 2f - width / 2f, 0, width, height);
        _scroll = new(0, 0, width * 0.9f, scrollDepth);
        _scrollView = new(0, scrollDepth);
        _text = new(0, 0, width, scrollDepth);

        TextStyle.alignment = TextAnchor.LowerLeft;
        TextStyle.fontSize = Hidden ? (int)(Screen.height * 0.0165f) : (int)(Screen.height * 0.0185f);
        TextStyle.normal.textColor = Color.white;
        TextStyle.wordWrap = !Hidden;

        var xPadding = (int)(Screen.width * 0.01f);
        var yPadding = (int)(Screen.height * 0.01f);

        TextStyle.padding = Hidden
            ? new(xPadding / 2, xPadding / 2, yPadding / 2, yPadding / 2)
            : new(xPadding, xPadding, yPadding, yPadding);

        var buttonWidth = (int)(Screen.width * 0.12f);
        var buttonHeight = (int)(Screen.height * 0.03f);

        _hideShowButton = new(Screen.width / 2f + width / 2f + buttonWidth / 3f, Screen.height * 0.004f, buttonWidth,
            buttonHeight);

        // draw server command text field and button
        width = (int)(Screen.width * 0.4f);
        var xPos = (int)(Screen.width / 2.0f - width / 2.0f);
        var yPos = (int)(Screen.height * 0.307f);
        height = 20;

        _commandTextRect = new(xPos, yPos, width, height);

        width = 100;
        yPos += 24;
        _sendCommandButton = new(xPos, yPos, width, height);
    }
}

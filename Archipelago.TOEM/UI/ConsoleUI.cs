
using System.Collections.Generic;
using Quests;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using HarmonyLib;

namespace Archipelago.TOEM.UI;

[HarmonyPatch]
public class ConsoleUI
{
    public static bool Hidden { get; private set; } = true;

    private static readonly List<string> LogLines = [];
    private static float LastUpdateTime = -HideTimeout;
    private static bool UpdatedLogLines = false;
    private static readonly List<string> CommandHist = [];
    private static int CommandHistIndex = 0;
    private const int MaxLogLines = 80;
    private const int MaxCommandHist = 80;
    private const float HideTimeout = 15f;

    private static GameObject fullGroup;
    private static GameObject shortGroup;
    private static GameObject scrollViewGroup;
    private static Inputbox commandInput;
    private static Text consoleLogText;
    private static Text shortText;
    private static Button showButton;

    [HarmonyPrefix, HarmonyPatch(typeof(MenuManager), nameof(MenuManager.InitializeManager))]
    public static void InitializeManager(MenuManager __instance)
    {
        if(__instance.transform.Find("AP Console") == null)
        {
            // AP console container
            var apConsoleGroup = new GameObject("AP Console");
            apConsoleGroup.transform.SetParent(__instance.transform, false);
            var acgRect = apConsoleGroup.AddComponent<RectTransform>();
            acgRect.anchorMin = new Vector2(0.3f, 0.6f);
            acgRect.anchorMax = new Vector2(0.7f, 1);
            acgRect.sizeDelta = Vector2.zero;
            var acgHori = apConsoleGroup.AddComponent<HorizontalLayoutGroup>();
            acgHori.childForceExpandHeight = false;
            acgHori.childForceExpandWidth = false;
            acgHori.childAlignment = TextAnchor.UpperRight;
            
            // Full console container
            fullGroup = new GameObject("FullConsoleGroup");
            fullGroup.transform.SetParent(apConsoleGroup.transform, false);
            var fgVert = fullGroup.AddComponent<VerticalLayoutGroup>();
            fgVert.padding = new RectOffset(5, 5, 8, 5);
            fgVert.spacing = 3;
            var fgElem = fullGroup.AddComponent<LayoutElement>();
            fgElem.flexibleWidth = 9999;
            var svgImage = fullGroup.AddComponent<Image>();
            svgImage.color = new Color(0, 0, 0, 0.2901961f);
            svgImage.type = Image.Type.Sliced;
            svgImage.sprite = Sprite.Create(Plugin.AssetManager.ScrollBox, new Rect(318, 350, 60, 64), 
                Vector2.zero, 100, 0, SpriteMeshType.Tight, new Vector4(29, 33, 29, 29));
            scrollViewGroup = new GameObject("ScrollViewGroup");
            scrollViewGroup.transform.SetParent(fullGroup.transform, false);
            var svgElem = scrollViewGroup.AddComponent<LayoutElement>();
            svgElem.flexibleHeight = 9999;
            var svgScrollRect = scrollViewGroup.AddComponent<ScrollRect>();
            svgScrollRect.horizontal = false;
            svgScrollRect.movementType = ScrollRect.MovementType.Clamped;
            svgScrollRect.scrollSensitivity = 35;
            svgScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

            // Scrollbar for full console
            var scrollBar = new GameObject("ScrollBar");
            scrollBar.transform.SetParent(scrollViewGroup.transform, false);
            var sbRect = scrollBar.AddComponent<RectTransform>();
            sbRect.anchorMin = new Vector2(1, 0);
            sbRect.anchorMax = new Vector2(1, 1);
            sbRect.pivot = new Vector2(1, 0.5f);
            sbRect.sizeDelta = new Vector2(50, 0);
            var sbImage = scrollBar.AddComponent<Image>();
            sbImage.color = new Color(0, 0, 0, 1);
            sbImage.type = Image.Type.Sliced;
            var sbSpriteNormal = Sprite.Create(Plugin.AssetManager.ScrollBox, new Rect(0, 256, 94, 94), 
                Vector2.zero, 100, 0, SpriteMeshType.Tight, new Vector4(22, 39, 47, 47));
            sbImage.sprite = sbSpriteNormal;
            var sbScrollBar = scrollBar.AddComponent<Scrollbar>();
            sbScrollBar.direction = Scrollbar.Direction.BottomToTop;
            sbScrollBar.transition = Selectable.Transition.SpriteSwap;
            sbScrollBar.spriteState = new()
            {
                m_HighlightedSprite = sbSpriteNormal,
                m_SelectedSprite = sbSpriteNormal,
                m_PressedSprite = Sprite.Create(Plugin.AssetManager.ScrollBox, new Rect(95, 256, 94, 94), 
                    Vector2.zero, 100, 0, SpriteMeshType.Tight, new Vector4(22, 39, 47, 47)),
                m_DisabledSprite = Sprite.Create(Plugin.AssetManager.ScrollBox, new Rect(0, 162, 94, 94), 
                    Vector2.zero, 100, 0, SpriteMeshType.Tight, new Vector4(23, 46, 48, 47))
            };
            var scrollBarBar = new GameObject("Bar");
            scrollBarBar.transform.SetParent(scrollBar.transform, false);
            var sbbRect = scrollBarBar.AddComponent<RectTransform>();
            sbbRect.sizeDelta = Vector2.zero;
            var sbbImage = scrollBarBar.AddComponent<Image>();
            sbbImage.type = Image.Type.Sliced;
            sbbImage.sprite = sbImage.sprite;
            sbScrollBar.handleRect = sbbRect;
            sbScrollBar.image = sbbImage;
            svgScrollRect.verticalScrollbar = sbScrollBar;

            // Scroll viewport for full console
            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollViewGroup.transform, false);
            var vpRect = viewport.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.anchoredPosition = new Vector2(5, 0);
            vpRect.pivot = Vector2.zero;
            vpRect.sizeDelta = new Vector2(-58, 0);
            viewport.AddComponent<Image>();
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var ctRect = content.AddComponent<RectTransform>();
            ctRect.anchorMin = Vector2.zero;
            ctRect.anchorMax = Vector2.one;
            ctRect.sizeDelta = Vector2.zero;
            ctRect.pivot = new Vector2(0.5f, 1);
            content.AddComponent<VerticalLayoutGroup>();
            var ctFitter = content.AddComponent<ContentSizeFitter>();
            ctFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            ctFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var ctSpacer = new GameObject("Spacer");
            ctSpacer.transform.SetParent(content.transform, false);
            ctSpacer.AddComponent<LayoutElement>().minHeight = 3;
            consoleLogText = new("", content);
            consoleLogText.textMesh.size = 30;
            consoleLogText.textMesh.textMaterial = Plugin.AssetManager.UIDefault;
            consoleLogText.overrideFont = Plugin.AssetManager.NaikaiFont;
            svgScrollRect.viewport = vpRect;
            svgScrollRect.content = ctRect;

            // Command container for full console
            var commandGroup = new GameObject("CommandGroup");
            commandGroup.transform.SetParent(fullGroup.transform, false);
            commandGroup.AddComponent<LayoutElement>();
            commandGroup.AddComponent<HorizontalLayoutGroup>();
            commandInput = new Inputbox("CommandInput", commandGroup, true)
            {
                onEnter = SendCommand,
                onUp = PrevCommand,
                onDown = NextCommand,
            };
            commandInput.holderElem.flexibleWidth = 9999;
            commandInput.bgImage.color = new Color(0, 0, 0, 0.6f);
            var sendButton = new Button("Send", commandGroup, true);
            sendButton.bgImage.color = new Color(0, 0, 0, 0.6f);
            sendButton.AddOnClick(SendCommand);
            fullGroup.SetActive(false);
            
            // Short console container
            shortGroup = new GameObject("ShortTextGroup");
            shortGroup.transform.SetParent(apConsoleGroup.transform, false);
            shortGroup.AddComponent<RectTransform>();
            var sgElem = shortGroup.AddComponent<LayoutElement>();
            sgElem.preferredHeight = 43;
            sgElem.minHeight = 43;
            sgElem.flexibleWidth = 9999;
            var sgImage = shortGroup.AddComponent<Image>();
            sgImage.color = new Color(0, 0, 0, 0.2901961f);
            sgImage.type = Image.Type.Sliced;
            sgImage.sprite = Sprite.Create(Plugin.AssetManager.ScrollBox, new Rect(318, 350, 60, 64), 
                Vector2.zero, 100, 0, SpriteMeshType.Tight, new Vector4(29, 33, 29, 29));
            shortGroup.AddComponent<Mask>();
            shortText = new("", shortGroup);
            shortText.textRect.anchoredPosition = new Vector2(10, -10);
            shortText.textMesh.size = 30;
            shortText.textMesh.textMaterial = Plugin.AssetManager.UIDefault;
            shortText.textMesh.anchor = TextAnchor.UpperLeft;
            shortText.textMesh.uiWrap = false;
            shortText.overrideFont = Plugin.AssetManager.NaikaiFont;
            shortGroup.SetActive(false);

            // Show button
            showButton = new Button("Show", apConsoleGroup, true);
            showButton.AddOnClick(ToggleConsole);
        }
    }

    public static void ToggleConsole()
    {
        Hidden = !Hidden;
        showButton.text.textMesh.text = Hidden ? "Show" : "Hide";
    }

    public static void SendCommand()
    {
        string command = commandInput.Text;
        if (!string.IsNullOrWhiteSpace(command))
        {
            if (CommandHist.Count == 0 || command != CommandHist[^1])
            {
                if (CommandHist.Count == MaxCommandHist)
                    CommandHist.RemoveAt(0);
                CommandHist.Add(command);
            }
            CommandHistIndex = -1;

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

    public static void SetCommandFromHist()
    {
        if (CommandHistIndex == -1)
            commandInput.Text = "";
        else
            commandInput.Text = CommandHist[^(CommandHistIndex + 1)];
        commandInput.SetCursorLast();
    }

    public static void PrevCommand()
    {
        if (CommandHistIndex+1 < CommandHist.Count)
        {
            CommandHistIndex++;
            SetCommandFromHist();
        }
    }

    public static void NextCommand()
    {
        if (CommandHistIndex >= 0)
        {
            CommandHistIndex--;
            SetCommandFromHist();
        }
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
                _ = int.TryParse(command[1], out newSpeed);

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
                _ = int.TryParse(command[1], out num);
            
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
        UpdatedLogLines = true;
    }
    
    public static bool RectContainsPoint(RectTransform rect, Vector2 point)
    {
        return point.x >= rect.position.x && point.x <= (rect.position.x + rect.rect.width) &&
               point.y <= rect.position.y && point.y >= (rect.position.y - rect.rect.height);
    }

    public static void Update()
    {
        if (shortGroup == null)
            return;
        
        bool oldFullActive = fullGroup.active;
        if (Hidden)
        {
            fullGroup.SetActive(false);
            shortGroup.SetActive(Time.time - LastUpdateTime < HideTimeout);
        }
        else
        {
            shortGroup.SetActive(false);
            fullGroup.SetActive(true);
        }

        if (!oldFullActive && fullGroup.active)
            commandInput.Select();

        if (LogLines.Count > 0)
        {
            shortText.textMesh.text = LogLines[^1];
            string scrollString = "";
            for (var i = 0; i < LogLines.Count; i++)
            {
                scrollString += LogLines.ElementAt(i);
                if (i < LogLines.Count - 1)
                {
                    scrollString += "\n";
                }
            }
            consoleLogText.textMesh.text = scrollString;
        }
        else
        {
            shortText.textMesh.text = "";
            consoleLogText.textMesh.text = "";
        }
    }

    public static void LateUpdate()
    {
        if (UpdatedLogLines)
        {
            UpdatedLogLines = false;
            scrollViewGroup.GetComponent<ScrollRect>().SetVerticalNormalizedPosition(0f);
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Il2CppInterop.Runtime;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Archipelago.TOEM;

[HarmonyPatch]
public class UIConnection
{
    public struct InputObjects
    {
        public RectTransform holderRect;
        public UiButton button;
        public SuperTextMesh text;
        public RectTransform textRect;
        public RectTransform caretRect;
    }

    private static List<InputObjects> Inputs = [];
    private static int SelectedInput = -1;
    private static int CaretIndex = -1;
    private static KeyCode HeldKey = KeyCode.None;
    private static float HeldRepeatTime = float.MaxValue;
    private static float CaretToggleTime = float.MaxValue;
    private static SuperTextMesh ConnectText = null;
    private static string Password;
    private static Font NaikaiFont;

    public static void CreateInput(string name, GameObject parent, Sprite sprite, Material textMaterial)
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
        ltMesh.textMaterial = textMaterial;

        var inputHolder = new GameObject(name + " Input");
        inputHolder.transform.SetParent(parent.transform, false);
        var ihRect = inputHolder.AddComponent<RectTransform>();
        ihRect.pivot = Vector2.zero;
        var ihElem = inputHolder.AddComponent<LayoutElement>();
        ihElem.preferredHeight = 120;
        var ihImage = inputHolder.AddComponent<Image>();
        ihImage.color = new Color(0, 0, 0, 0.2901961f);
        ihImage.type = Image.Type.Sliced;
        ihImage.sprite = sprite;
        var ihButton = inputHolder.AddComponent<UiButton>();
        ihButton.HasClickSound = false;
        ihButton.animateButton = false;

        var inputText = new GameObject("InputText");
        inputText.transform.SetParent(inputHolder.transform, false);
        var itRect = inputText.AddComponent<RectTransform>();
        itRect.anchorMin = Vector2.zero;
        itRect.anchorMax = Vector2.one;
        itRect.anchoredPosition = new Vector2(32, 0);
        itRect.pivot = Vector2.zero;
        itRect.sizeDelta = Vector2.zero;
        var itMesh = inputText.AddComponent<SuperTextMesh>();
        itMesh.size = 70;
        itMesh.text = "";
        itMesh.anchor = TextAnchor.MiddleLeft;
        itMesh.textMaterial = textMaterial;
        itMesh.ColorDeselect();

        var caret = new GameObject("Caret");
        caret.transform.SetParent(inputHolder.transform, false);
        var crRect = caret.AddComponent<RectTransform>();
        crRect.anchorMin = Vector2.zero;
        crRect.anchorMax = Vector2.zero;
        crRect.anchoredPosition = Vector2.zero;
        crRect.pivot = Vector2.zero;
        crRect.sizeDelta = new Vector2(2, 70);
        caret.AddComponent<Image>();
        caret.active = false;

        Inputs.Add(new InputObjects{holderRect = ihRect, button = ihButton, text = itMesh, textRect = itRect, caretRect = crRect});
    }
    
    [HarmonyPostfix, HarmonyPatch(typeof(TitleScreenMenu), nameof(TitleScreenMenu.OnMenuOpen))]
    public static void OnMenuOpen_Postfix(TitleScreenMenu __instance)
    {
        Plugin.Logger.LogInfo("TitleScreenMenu.OnMenuOpen() postfix");
        __instance.hasSaveFile = true;
        __instance.SetupTitleScreen(true, false);
        __instance.displayPlaytime.gameObject.active = false;
        // Hide New adventure button
        __instance.mainGrid.gameObject.transform.GetChild(2).gameObject.active = false;
        
        __instance.versionText.text = $"{__instance.versionLocalizedString.ToString()} {Application.version} (Toem), {MyPluginInfo.PLUGIN_VERSION} (AP)";

        SelectedInput = -1;
        var mainTransform = __instance.continueGameButtonContainer.transform;
        if(mainTransform.childCount == 3)
        {
            var allFonts = Object.FindObjectsOfTypeAll(Il2CppType.Of<Font>());
            NaikaiFont = allFonts.First(font => font.name == "NaikaiFont-noHearts").Cast<Font>();

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
            var textMaterial = topButton.transform.GetChild(0).GetComponent<SuperTextMesh>().textMaterial;
            Inputs = [];
            CreateInput("Hostname:Port", connectionGroup, buttonSprite, textMaterial);
            CreateInput("Player Name", connectionGroup, buttonSprite, textMaterial);
            CreateInput("Password", connectionGroup, buttonSprite, textMaterial);
            Inputs[1].textRect.anchoredPosition = new Vector2(32, -10);
            Inputs[0].text.text = Plugin.State.Uri;
            Inputs[1].text.text = Plugin.State.SlotName;
            Password = Plugin.State.Password;
            Inputs[2].text.text = new string('*', Password.Length);

            var spacer = new GameObject("Spacer");
            spacer.transform.SetParent(connectionGroup.transform, false);
            spacer.AddComponent<RectTransform>();
            var spacerElem = spacer.AddComponent<LayoutElement>();
            spacerElem.preferredHeight = 60;
            
            var connectHolder = new GameObject("Connect Button");
            connectHolder.transform.SetParent(connectionGroup.transform, false);
            connectHolder.AddComponent<RectTransform>();
            var chElem = connectHolder.AddComponent<LayoutElement>();
            chElem.preferredHeight = 120;
            var chImage = connectHolder.AddComponent<Image>();
            chImage.color = new Color(0, 0, 0, 0.2901961f);
            chImage.type = Image.Type.Sliced;
            chImage.sprite = buttonSprite;
            var chButton = connectHolder.AddComponent<UiButton>();
            
            var connectText = new GameObject("Text");
            connectText.transform.SetParent(connectHolder.transform, false);
            var ctRect = connectText.AddComponent<RectTransform>();
            ctRect.anchorMin = Vector2.zero;
            ctRect.anchorMax = Vector2.one;
            ctRect.anchoredPosition = new Vector2(32, 0);
            ctRect.sizeDelta = Vector2.zero;
            ConnectText = connectText.AddComponent<SuperTextMesh>();
            ConnectText.size = 70;
            ConnectText.text = "Connect";
            ConnectText.anchor = TextAnchor.MiddleLeft;
            ConnectText.textMaterial = textMaterial;
            ConnectText.ColorDeselect();
            
            chButton.onClick = new UnityEvent();
            chButton.onHover = new UnityEvent();
            chButton.onExit = new UnityEvent();
            chButton.onClick.AddListener((UnityAction)ToggleConnection);
            chButton.onHover.AddListener((UnityAction)ConnectText.ColorSelect);
            chButton.onExit.AddListener((UnityAction)ConnectText.ColorDeselect);
        }
    }

    public static void ToggleConnection()
    {
        if (Plugin.Client.Connected)
            Plugin.Client.Disconnect();
        else
            Plugin.Client.Connect();
    }
    
    public static bool RectContainsPoint(RectTransform rect, Vector2 point)
    {
        var uiCam = UiButtonSystem.Instance.uiCam;
        var posMin = uiCam.WorldToScreenPoint(rect.TransformPoint(rect.rect.min));
        var posMax = uiCam.WorldToScreenPoint(rect.TransformPoint(rect.rect.max));
        return point.x >= (int)posMin.x && point.x < (int)posMax.x &&
               point.y >= (int)posMin.y && point.y < (int)posMax.y;
    }
    
    public static int FindClosesCaretIndex(Vector2 point)
    {
        InputObjects input = Inputs[SelectedInput];
        var uiCam = UiButtonSystem.Instance.uiCam;
        float bestDistance = float.MaxValue;
        int bestChar = -1;
        int caretIndex = 0;
        for (int i = 0; i < input.text.info.Count; i++)
        {
            if (input.text.info[i].chGlyphIndex < 0)
                continue;
            Vector2 charPos = Vector2.zero;
            for (int j = 0; j < 4; j++)
                charPos += (Vector2)input.text.endVerts[i*4+j];
            charPos /= 4;
            Vector2 worldPos = (Vector2)uiCam.WorldToScreenPoint(input.textRect.TransformPoint(charPos));
            float dist = (point - worldPos).sqrMagnitude;
            if(dist < bestDistance)
            {
                bestDistance = dist;
                bestChar = point.x < worldPos.x? caretIndex-1: caretIndex;
            }
            caretIndex++;
        }
        return bestChar;
    }
    
    public static Vector2 SetCaretPos(int index)
    {
        InputObjects input = Inputs[SelectedInput];
        int stmIndex = -1;
        int caretIndex = 0;
        if (index >= 0)
        {
            for (stmIndex = 0; stmIndex < input.text.info.Count; stmIndex++)
            {
                if (input.text.info[stmIndex].chGlyphIndex < 0)
                    continue;
                if (caretIndex == index)
                    break;
                caretIndex++;
            }
        }
        float caretX = 32;
        if (stmIndex >= 0)
            caretX += input.text.endVerts[stmIndex*4+1].x;
        float caretY = 26;
        if (input.text.lineBreaks.Count > 1)
        {
            int line = 0;
            foreach (int lineBreak in input.text.lineBreaks)
                if (stmIndex > lineBreak)
                    line++;
            caretY = 51 - 70 * line;
        }
        return new Vector2(caretX, caretY);
    }

    [HarmonyPrefix, HarmonyPatch(typeof(TitleScreenMenu), nameof(TitleScreenMenu.Update))]
    public static void Update(TitleScreenMenu __instance)
    {
        float time = Time.time;
        // Check if an Input field was clicked
        if (Input.GetMouseButtonDown(0))
        {
            bool selected = false;
            int oldSelectedInput = SelectedInput;
            for (int i = 0; i < Inputs.Count; i++)
            {
                var input = Inputs[i];
                if (RectContainsPoint(input.holderRect, Input.mousePosition) && !Plugin.Client.Connected)
                {
                    input.text.ColorSelect();
                    SelectedInput = -1;
                    UiButtonSystem.Instance.ForceSetTargetButton(input.button);
                    SelectedInput = i;
                    
                    CaretIndex = FindClosesCaretIndex(Input.mousePosition);
                    input.caretRect.anchoredPosition = SetCaretPos(CaretIndex);
                    input.caretRect.gameObject.active = true;
                    CaretToggleTime = time + 0.5f;
                    selected = true;
                }
                else
                {
                    if (oldSelectedInput == i)
                    {
                        if (oldSelectedInput == 0)
                            Plugin.State.Uri = Inputs[0].text.text;
                        else if (oldSelectedInput == 1)
                            Plugin.State.SlotName = Inputs[1].text.text;
                        else if (oldSelectedInput == 2)
                            Plugin.State.Password = Password;
                    }
                    input.caretRect.gameObject.active = false;
                    input.text.ColorDeselect();
                }
            }
            if(!selected && SelectedInput >= 0)
            {
                SelectedInput = -1;
                UiButtonSystem.Instance.SetTargetButton(null);
            }
        }

        // Handle input when an input field is selected
        if (SelectedInput >= 0)
        {
            var inspectMenu = MenuManager.Instance.menu_PhotoInspectDelete.gameObject.GetComponent<Photographing.PictureInspectUI>();
            var input = Inputs[SelectedInput];
            string inputText = input.text.text;
            if (SelectedInput == 2)
                inputText = Password;

            // Handle printable characters (and backspace)
            foreach (char c in Input.inputString)
            {
                if (c == '\b')
                {
                    if (CaretIndex >= 0)
                    {
                        inputText = inputText.Remove(CaretIndex, 1);
                        CaretIndex--;
                        inspectMenu.onLetterErasedSound.PlaySound();
                    }
                }
                else if (c >= ' ')
                {
                    CaretIndex++;
                    inputText = inputText.Insert(CaretIndex, c.ToString());
                    inspectMenu.onLetterAddedSound.PlaySound();
                }
            }
        
            // Handle manual repeating of delete, and arrows
            KeyCode action = KeyCode.None;
            if (Input.GetKeyDown(KeyCode.Delete))
            {
                action = KeyCode.Delete;
                HeldKey = KeyCode.Delete;
                HeldRepeatTime = time + 0.5f;
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                action = KeyCode.LeftArrow;
                HeldKey = KeyCode.LeftArrow;
                HeldRepeatTime = time + 0.5f;
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                action = KeyCode.RightArrow;
                HeldKey = KeyCode.RightArrow;
                HeldRepeatTime = time + 0.5f;
            }
            else if (!Input.GetKey(HeldKey))
                HeldKey = KeyCode.None;

            if (time > HeldRepeatTime)
            {
                action = HeldKey;
                HeldRepeatTime = time + 0.03f;
            }

            if (action == KeyCode.Delete && CaretIndex+1 < inputText.Length)
            {
                inputText = inputText.Remove(CaretIndex+1, 1);
                inspectMenu.onLetterErasedSound.PlaySound();
            }
            else if (action == KeyCode.LeftArrow && CaretIndex >= 0)
            {
                CaretIndex--;
            }
            else if (action == KeyCode.RightArrow && CaretIndex+1 < inputText.Length)
            {
                CaretIndex++;
            }

            bool ctrlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            if (ctrlHeld && Input.GetKeyDown(KeyCode.V))
            {
                string clipboard = GUIUtility.systemCopyBuffer;
                inputText = inputText.Insert(CaretIndex+1, clipboard);
                CaretIndex += clipboard.Length;
                inspectMenu.onLetterAddedSound.PlaySound();
            }
            
            if (SelectedInput == 2)
            {
                Password = inputText;
                inputText = new string('*', Password.Length);
            }
            Inputs[SelectedInput].text.text = inputText;
            input.caretRect.anchoredPosition = SetCaretPos(CaretIndex);

            // Caret blink
            if (time > CaretToggleTime)
            {
                CaretToggleTime = time + 0.5f;
                input.caretRect.gameObject.active = !input.caretRect.gameObject.active;
            }
        }

        // Set connect button text
        var continueButton = __instance.mainGrid.gameObject.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<SuperTextMesh>();
        if (Plugin.Client.Connected)
        {
            ConnectText.text = "Disconnect";
            continueButton.text = "<w=sassy>Start";
        }
        else
        {
            ConnectText.text = "Connect";
            continueButton.text = "Connect & Start";
        }
    }
    
    [HarmonyPrefix, HarmonyPatch(typeof(UiButtonSystem), nameof(UiButtonSystem.SetTargetButton))]
    public static bool SetTargetButton()
    {
        return SelectedInput < 0;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(SuperTextMesh), nameof(SuperTextMesh.Rebuild), [typeof(float), typeof(bool), typeof(bool)])]
    public static void Rebuild(SuperTextMesh __instance)
    {
        if (Inputs.Count > 2 && __instance == Inputs[1].text)
            SuperTextMesh.font = NaikaiFont;
    }

    [HarmonyPostfix, HarmonyPatch(typeof(SuperTextMesh), nameof(SuperTextMesh.Rebuild), [typeof(float), typeof(bool), typeof(bool)])]
    public static void Rebuild_Postfix(SuperTextMesh __instance)
    {
        if (Inputs.Count > 2 && __instance == Inputs[1].text)
            SuperTextMesh.font = __instance.data.defaultFont;
    }
}

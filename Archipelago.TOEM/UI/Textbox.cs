using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace Archipelago.TOEM.UI;

[HarmonyPatch]
public class Textbox
{

    private static Textbox SelectedInput = null;
    private static List<Textbox> Instances = [];

    public GameObject gameObject;
    public RectTransform holderRect;
    public UiButton button;
    public SuperTextMesh textMesh;
    public RectTransform textRect;
    public RectTransform caretRect;

    public Font overrideFont = null;
    public bool password = false;
    public Func<bool> selectable = () => true;
    public Action<string> onDeselect = text => {};

    private string text = "";
    private int caretIndex = -1;
    private float caretToggleTime = float.MaxValue;
    private KeyCode heldKey = KeyCode.None;
    private float heldRepeatTime = float.MaxValue;
    
    public Textbox(string name, GameObject parent, Sprite sprite, Material textMaterial)
    {
        gameObject = new GameObject(name + " Input");
        gameObject.transform.SetParent(parent.transform, false);
        holderRect = gameObject.AddComponent<RectTransform>();
        holderRect.pivot = Vector2.zero;
        var ihElem = gameObject.AddComponent<LayoutElement>();
        ihElem.preferredHeight = 120;
        var ihImage = gameObject.AddComponent<Image>();
        ihImage.color = new Color(0, 0, 0, 0.2901961f);
        ihImage.type = Image.Type.Sliced;
        ihImage.sprite = sprite;
        button = gameObject.AddComponent<UiButton>();
        button.HasClickSound = false;
        button.animateButton = false;

        var inputText = new GameObject("InputText");
        inputText.transform.SetParent(gameObject.transform, false);
        textRect = inputText.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.anchoredPosition = new Vector2(32, 0);
        textRect.pivot = Vector2.zero;
        textRect.sizeDelta = Vector2.zero;
        textMesh = inputText.AddComponent<SuperTextMesh>();
        textMesh.size = 70;
        textMesh.text = "";
        textMesh.anchor = TextAnchor.MiddleLeft;
        textMesh.textMaterial = textMaterial;
        textMesh.ColorDeselect();

        var caret = new GameObject("Caret");
        caret.transform.SetParent(gameObject.transform, false);
        caretRect = caret.AddComponent<RectTransform>();
        caretRect.anchorMin = Vector2.zero;
        caretRect.anchorMax = Vector2.zero;
        caretRect.anchoredPosition = Vector2.zero;
        caretRect.pivot = Vector2.zero;
        caretRect.sizeDelta = new Vector2(2, 70);
        caret.AddComponent<Image>();
        caret.active = false;

        Instances.Add(this);
    }

    public void SetText(string textString)
    {
        text = textString;
        textMesh.text = password? new string('*', text.Length): text;
    }
    
    public void FindClosesCaretIndex(Vector2 point)
    {
        var uiCam = UiButtonSystem.Instance.uiCam;
        float bestDistance = float.MaxValue;
        caretIndex = -1;
        int testCaretIndex = 0;
        for (int i = 0; i < textMesh.info.Count; i++)
        {
            if (textMesh.info[i].chGlyphIndex < 0)
                continue;
            Vector2 charPos = Vector2.zero;
            for (int j = 0; j < 4; j++)
                charPos += (Vector2)textMesh.endVerts[i*4+j];
            charPos /= 4;
            Vector2 worldPos = (Vector2)uiCam.WorldToScreenPoint(textRect.TransformPoint(charPos));
            float dist = (point - worldPos).sqrMagnitude;
            if(dist < bestDistance)
            {
                bestDistance = dist;
                caretIndex = point.x < worldPos.x? testCaretIndex-1: testCaretIndex;
            }
            testCaretIndex++;
        }
        
        UpdateCaretPos();
    }
    
    public void UpdateCaretPos()
    {
        int stmIndex = -1;
        int testCaretIndex = 0;
        if (caretIndex >= 0)
        {
            for (stmIndex = 0; stmIndex < textMesh.info.Count; stmIndex++)
            {
                if (textMesh.info[stmIndex].chGlyphIndex < 0)
                    continue;
                if (testCaretIndex == caretIndex)
                    break;
                testCaretIndex++;
            }
        }
        float caretX = 32;
        if (stmIndex >= 0)
            caretX += textMesh.endVerts[stmIndex*4+1].x;
        float caretY = 26;
        if (textMesh.lineBreaks.Count > 1)
        {
            int line = 0;
            foreach (int lineBreak in textMesh.lineBreaks)
                if (stmIndex > lineBreak)
                    line++;
            caretY = 51 - 70 * line;
        }

        caretRect.anchoredPosition = new Vector2(caretX, caretY);
    }
    
    public static bool RectContainsPoint(RectTransform rect, Vector2 point)
    {
        var uiCam = UiButtonSystem.Instance.uiCam;
        var posMin = uiCam.WorldToScreenPoint(rect.TransformPoint(rect.rect.min));
        var posMax = uiCam.WorldToScreenPoint(rect.TransformPoint(rect.rect.max));
        return point.x >= (int)posMin.x && point.x < (int)posMax.x &&
               point.y >= (int)posMin.y && point.y < (int)posMax.y;
    }

    public static void UpdateAll()
    {
        if (SelectedInput != null && !SelectedInput.gameObject.activeInHierarchy)
            SelectedInput = null;

        // Check if an Input field was clicked
        if (Input.GetMouseButtonDown(0))
        {
            bool new_selection = false;
            Textbox oldSelectedInput = SelectedInput;
            foreach (var instance in Instances)
            {
                if (!instance.gameObject.activeInHierarchy)
                    continue;

                if (RectContainsPoint(instance.holderRect, Input.mousePosition) && !Plugin.Client.Connected)
                {
                    new_selection = true;
                    SelectedInput = null;
                    UiButtonSystem.Instance.ForceSetTargetButton(instance.button);
                    SelectedInput = instance;
                    
                    instance.FindClosesCaretIndex(Input.mousePosition);
                    instance.caretRect.gameObject.active = true;
                    instance.caretToggleTime = Time.time + 0.5f;
                    instance.textMesh.ColorSelect();
                }
                else
                {
                    if (oldSelectedInput == instance)
                        instance.onDeselect(instance.text);
                    instance.caretRect.gameObject.active = false;
                    instance.textMesh.ColorDeselect();
                }
            }
            if(!new_selection)
            {
                SelectedInput = null;
                UiButtonSystem.Instance.SetTargetButton(null);
            }
        }

        // Handle input when an input field is selected
        SelectedInput?.Update();
    }

    public void Update()
    {
        float time = Time.time;
        var inspectMenu = MenuManager.Instance.menu_PhotoInspectDelete.gameObject.GetComponent<Photographing.PictureInspectUI>();

        // Handle printable characters (and backspace)
        foreach (char c in Input.inputString)
        {
            if (c == '\b')
            {
                if (caretIndex >= 0)
                {
                    text = text.Remove(caretIndex, 1);
                    caretIndex--;
                    inspectMenu.onLetterErasedSound.PlaySound();
                }
            }
            else if (c >= ' ')
            {
                caretIndex++;
                text = text.Insert(caretIndex, c.ToString());
                inspectMenu.onLetterAddedSound.PlaySound();
            }
        }
    
        // Handle manual repeating of delete, and arrows
        KeyCode action = KeyCode.None;
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            action = KeyCode.Delete;
            heldKey = KeyCode.Delete;
            heldRepeatTime = time + 0.5f;
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            action = KeyCode.LeftArrow;
            heldKey = KeyCode.LeftArrow;
            heldRepeatTime = time + 0.5f;
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            action = KeyCode.RightArrow;
            heldKey = KeyCode.RightArrow;
            heldRepeatTime = time + 0.5f;
        }
        else if (!Input.GetKey(heldKey))
            heldKey = KeyCode.None;

        if (time > heldRepeatTime)
        {
            action = heldKey;
            heldRepeatTime = time + 0.03f;
        }

        if (action == KeyCode.Delete && caretIndex+1 < text.Length)
        {
            text = text.Remove(caretIndex+1, 1);
            inspectMenu.onLetterErasedSound.PlaySound();
        }
        else if (action == KeyCode.LeftArrow && caretIndex >= 0)
        {
            caretIndex--;
        }
        else if (action == KeyCode.RightArrow && caretIndex+1 < text.Length)
        {
            caretIndex++;
        }

        bool ctrlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        if (ctrlHeld && Input.GetKeyDown(KeyCode.V))
        {
            string clipboard = GUIUtility.systemCopyBuffer;
            text = text.Insert(caretIndex+1, clipboard);
            caretIndex += clipboard.Length;
            inspectMenu.onLetterAddedSound.PlaySound();
        }
        
        SetText(text);
        UpdateCaretPos();

        // Caret blink
        if (time > caretToggleTime)
        {
            caretToggleTime = time + 0.5f;
            caretRect.gameObject.active = !caretRect.gameObject.active;
        }
    }
    
    [HarmonyPrefix, HarmonyPatch(typeof(UiButtonSystem), nameof(UiButtonSystem.SetTargetButton))]
    public static bool SetTargetButton()
    {
        return SelectedInput == null || !SelectedInput.gameObject.activeInHierarchy;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(SuperTextMesh), nameof(SuperTextMesh.Rebuild), [typeof(float), typeof(bool), typeof(bool)])]
    public static void Rebuild(SuperTextMesh __instance)
    {
        foreach (var instance in Instances)
        {
            if (instance.textMesh == __instance && instance.overrideFont)
            {
                SuperTextMesh.font = instance.overrideFont;
                break;
            }
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(SuperTextMesh), nameof(SuperTextMesh.Rebuild), [typeof(float), typeof(bool), typeof(bool)])]
    public static void Rebuild_Postfix(SuperTextMesh __instance)
    {
        foreach (var instance in Instances)
        {
            if (instance.textMesh == __instance && instance.overrideFont)
            {
                SuperTextMesh.font = __instance.data.defaultFont;
                break;
            }
        }
    }
}

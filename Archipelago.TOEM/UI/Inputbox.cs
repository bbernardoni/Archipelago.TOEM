using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace Archipelago.TOEM.UI;

[HarmonyPatch]
public class Inputbox
{
    private static Inputbox SelectedInput = null;
    private static readonly List<Inputbox> Instances = [];

    public GameObject gameObject;
    public RectTransform holderRect;
    public LayoutElement holderElem;
    public Image bgImage;
    public UiButton button;
    public Text textObj;
    public RectTransform selectionRect;
    public RectTransform caretRect;

    public bool password = false;
    public Func<bool> selectable = () => true;
    public Action<string> onDeselect = text => {};
    public Action onUp = () => {};
    public Action onDown = () => {};
    public Action onEnter = () => {};
    public string Text
    {
        get { return text; }
        set { SetText(value); }
    }

    private string text = "";
    private int caretIndex = -1;
    private int selectStartIndex = -1;
    private float caretToggleTime = float.MaxValue;
    private KeyCode heldKey = KeyCode.None;
    private float heldRepeatTime = float.MaxValue;
    private readonly float leftPadding;
    private readonly float rightPadding;

    public bool IsActive => gameObject != null && gameObject.activeInHierarchy;
    
    public Inputbox(string name, GameObject parent, bool small = false)
    {
        gameObject = new GameObject(name + " Input");
        gameObject.transform.SetParent(parent.transform, false);
        holderRect = gameObject.AddComponent<RectTransform>();
        holderRect.pivot = Vector2.zero;
        holderElem = gameObject.AddComponent<LayoutElement>();
        bgImage = gameObject.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.2901961f);
        bgImage.type = Image.Type.Sliced;
        bgImage.sprite = Sprite.Create(Plugin.AssetManager.ScrollBox, new Rect(318, 350, 60, 64), 
            Vector2.zero, 100, 0, SpriteMeshType.Tight, new Vector4(29, 33, 29, 29));
        button = gameObject.AddComponent<UiButton>();
        button.HasClickSound = false;
        button.animateButton = false;
        gameObject.AddComponent<Mask>();

        textObj = new(name, gameObject);
        textObj.textMesh.text = "";
        textObj.textMesh.uiWrap = false;
        textObj.textMesh.ColorDeselect();
        
        var selection = new GameObject("Selection");
        selection.transform.SetParent(gameObject.transform, false);
        selectionRect = selection.AddComponent<RectTransform>();
        selectionRect.anchorMin = Vector2.zero;
        selectionRect.anchorMax = Vector2.zero;
        selectionRect.pivot = Vector2.zero;
        selection.AddComponent<Image>().color = new Color(1, 1, 1, 0.6f);
        selection.active = false;

        var caret = new GameObject("Caret");
        caret.transform.SetParent(gameObject.transform, false);
        caretRect = caret.AddComponent<RectTransform>();
        caretRect.anchorMin = Vector2.zero;
        caretRect.anchorMax = Vector2.zero;
        caretRect.pivot = Vector2.zero;
        caret.AddComponent<Image>();
        caret.active = false;

        if (small)
        {
            leftPadding = 10;
            rightPadding = 10;
            holderElem.preferredHeight = 43;
            holderElem.minHeight = 43;
            button.hoverType = UiButtonSystem.HoverType.None;
            textObj.textRect.anchoredPosition = new Vector2(10, -5);
            textObj.textMesh.size = 30;
            textObj.textMesh.textMaterial = Plugin.AssetManager.UIDefault;
            textObj.overrideFont = Plugin.AssetManager.NaikaiFont;
            selectionRect.anchoredPosition = new Vector2(0, 6);
            selectionRect.sizeDelta = new Vector2(0, 30);
            caretRect.anchoredPosition3D = new Vector3(0, 6, 1);
            caretRect.sizeDelta = new Vector2(1, 30);
        }
        else
        {
            leftPadding = 32;
            rightPadding = 16;
            holderElem.preferredHeight = 120;
            selectionRect.anchoredPosition = new Vector2(0, 26);
            selectionRect.sizeDelta = new Vector2(0, 70);
            caretRect.anchoredPosition3D = new Vector3(0, 26, 1);
            caretRect.sizeDelta = new Vector2(2, 70);
        }

        Instances.Add(this);
    }

    public void SetText(string textString)
    {
        text = textString;
        if (caretIndex > text.Length - 1)
            SetCursorLast();
        textObj.textMesh.text = password? new string('*', text.Length): text;
    }
    
    public void FindClosesCaretIndex(Vector2 point)
    {
        var uiCam = UiButtonSystem.Instance.uiCam;
        float bestDistance = float.MaxValue;
        caretIndex = -1;
        int testCaretIndex = 0;
        for (int i = 0; i < textObj.textMesh.info.Count; i++)
        {
            if (textObj.textMesh.info[i].chGlyphIndex < 0)
                continue;
            Vector2 charPos = Vector2.zero;
            for (int j = 0; j < 4; j++)
                charPos += (Vector2)textObj.textMesh.endVerts[i*4+j];
            charPos /= 4;
            Vector2 worldPos = (Vector2)uiCam.WorldToScreenPoint(textObj.textRect.TransformPoint(charPos));
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
    
    public float IndexToOffset(int index)
    {
        int stmIndex = -1;
        int testIndex = 0;
        if (index >= 0)
        {
            for (stmIndex = 0; stmIndex < textObj.textMesh.info.Count; stmIndex++)
            {
                if (textObj.textMesh.info[stmIndex].chGlyphIndex < 0)
                    continue;
                if (testIndex == index)
                    break;
                testIndex++;
            }
        }
        float offset = stmIndex < 0? 0: textObj.textMesh.endVerts[stmIndex*4+1].x;
        offset += textObj.textRect.anchoredPosition.x;
        return offset;
    }
    
    public void UpdateCaretPos()
    {
        caretRect.anchoredPosition = new Vector2(IndexToOffset(caretIndex), caretRect.anchoredPosition.y);
    }

    public void SetCursorLast()
    {
        caretIndex = text.Length - 1;
        UpdateCaretPos();
    }

    public void Deselect()
    {
        onDeselect(text);
        caretRect.gameObject.active = false;
        selectionRect.gameObject.active = false;
        textObj.textMesh.ColorDeselect();
        SelectedInput = null;
    }

    public void Select()
    {
        if (SelectedInput != null && SelectedInput != this)
            SelectedInput.Deselect();

        SelectedInput = null;
        UiButtonSystem.Instance.ForceSetTargetButton(button);
        SelectedInput = this;

        caretRect.gameObject.active = true;
        caretToggleTime = Time.time + 0.5f;
        textObj.textMesh.ColorSelect();
        SetCursorLast();
    }

    private void DeleteSelection()
    {
        if (caretIndex == selectStartIndex)
            return;
        int removeLen = Math.Abs(caretIndex - selectStartIndex);
        int removeStart = Math.Min(caretIndex, selectStartIndex);
        text = text.Remove(removeStart + 1, removeLen);
        caretIndex = removeStart;
        selectStartIndex = removeStart;
    }
    
    public static void UpdateAll()
    {
        // Clear stale selected input
        if (SelectedInput != null && !SelectedInput.IsActive)
            SelectedInput = null;

        // Remove destroyed inputs
        for (int i = Instances.Count - 1; i >= 0; i--)
            if (Instances[i].gameObject == null)
                Instances.RemoveAt(i);

        // Check if an Input field was clicked
        if (Input.GetMouseButtonDown(0))
        {
            bool newSelection = false;
            foreach (var instance in Instances)
            {
                if (!instance.IsActive)
                    continue;

                if (UiButtonSystem.hoverButton == instance.button && instance.selectable())
                {
                    newSelection = true;
                    bool sameInput = SelectedInput == instance;
                    instance.Select();
                    instance.FindClosesCaretIndex(Input.mousePosition);
                    if (!sameInput || !(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
                        instance.selectStartIndex = instance.caretIndex;
                }
                else
                {
                    instance.caretRect.gameObject.active = false;
                    instance.selectionRect.gameObject.active = false;
                    instance.textObj.textMesh.ColorDeselect();
                }
            }
            if(!newSelection && SelectedInput != null)
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

        if (Input.GetMouseButton(0))
        {
            FindClosesCaretIndex(Input.mousePosition);
        }

        // Handle printable characters (and backspace)
        foreach (char c in Input.inputString)
        {
            if (c == '\b')
            {
                if (caretIndex != selectStartIndex)
                {
                    DeleteSelection();
                    inspectMenu.onLetterErasedSound.PlaySound();
                }
                else if (caretIndex >= 0)
                {
                    text = text.Remove(caretIndex, 1);
                    caretIndex--;
                    selectStartIndex = caretIndex;
                    inspectMenu.onLetterErasedSound.PlaySound();
                }
            }
            else if (c >= ' ')
            {
                DeleteSelection();
                caretIndex++;
                selectStartIndex = caretIndex;
                text = text.Insert(caretIndex, c.ToString());
                inspectMenu.onLetterAddedSound.PlaySound();
            }
        }
    
        // Handle manual repeating of delete, and arrows
        bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
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
            if (caretIndex != selectStartIndex)
            {
                DeleteSelection();
                inspectMenu.onLetterErasedSound.PlaySound();
            }
            else if (caretIndex+1 < text.Length)
            {
                text = text.Remove(caretIndex+1, 1);
                inspectMenu.onLetterErasedSound.PlaySound();
            }
        }
        else if (action == KeyCode.LeftArrow && caretIndex >= 0)
        {
            caretIndex--;
            if (!shiftHeld)
                selectStartIndex = caretIndex;
        }
        else if (action == KeyCode.RightArrow && caretIndex+1 < text.Length)
        {
            caretIndex++;
            if (!shiftHeld)
                selectStartIndex = caretIndex;
        }
        
        if (Input.GetKeyDown(KeyCode.UpArrow))
            onUp();
        if (Input.GetKeyDown(KeyCode.DownArrow))
            onDown();
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            onEnter();

        bool ctrlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        if (ctrlHeld)
        {
            if (Input.GetKeyDown(KeyCode.V))
            {
                DeleteSelection();
                string clipboard = GUIUtility.systemCopyBuffer;
                clipboard = clipboard.Replace("\n", "").Replace("\r", "");
                text = text.Insert(caretIndex+1, clipboard);
                caretIndex += clipboard.Length;
                selectStartIndex = caretIndex;
                inspectMenu.onLetterAddedSound.PlaySound();
            }
            else if (Input.GetKeyDown(KeyCode.C) && caretIndex != selectStartIndex)
            {
                int selectLen = Math.Abs(caretIndex - selectStartIndex);
                int selectStart = Math.Min(caretIndex, selectStartIndex);
                GUIUtility.systemCopyBuffer = text.Substring(selectStart + 1, selectLen);
            }
            else if (Input.GetKeyDown(KeyCode.X) && caretIndex != selectStartIndex)
            {
                int selectLen = Math.Abs(caretIndex - selectStartIndex);
                int selectStart = Math.Min(caretIndex, selectStartIndex);
                GUIUtility.systemCopyBuffer = text.Substring(selectStart + 1, selectLen);
                DeleteSelection();
                inspectMenu.onLetterErasedSound.PlaySound();
            }
            else if (Input.GetKeyDown(KeyCode.A))
            {
                selectStartIndex = -1;
                SetCursorLast();
            }
        }
        
        SetText(text);
        UpdateCaretPos();
        float caretXMax = holderRect.sizeDelta.x - rightPadding;
        float textX = textObj.textRect.anchoredPosition.x;
        if (caretRect.anchoredPosition.x > caretXMax)
        {
            textX -= caretRect.anchoredPosition.x - caretXMax;
            textObj.textRect.anchoredPosition = new Vector2(textX, textObj.textRect.anchoredPosition.y);
            caretRect.anchoredPosition = new Vector2(caretXMax, caretRect.anchoredPosition.y);
        }
        if (caretRect.anchoredPosition.x < leftPadding)
        {
            textX += leftPadding - caretRect.anchoredPosition.x;
            textObj.textRect.anchoredPosition = new Vector2(textX, textObj.textRect.anchoredPosition.y);
            caretRect.anchoredPosition = new Vector2(leftPadding, caretRect.anchoredPosition.y);
        }
        var endVerts = textObj.textMesh.endVerts;
        if (endVerts.Count == 0 || leftPadding + endVerts[^2].x <= caretXMax)
        {
            textObj.textRect.anchoredPosition = new Vector2(leftPadding, textObj.textRect.anchoredPosition.y);
            UpdateCaretPos();
        }
        else if (textX + endVerts[^2].x < caretXMax)
        {
            textX = caretXMax - endVerts[^2].x;
            textObj.textRect.anchoredPosition = new Vector2(textX, textObj.textRect.anchoredPosition.y);
            UpdateCaretPos();
        }
        selectionRect.gameObject.active = caretIndex != selectStartIndex;
        if (selectionRect.gameObject.active)
        {
            float selectOffset = IndexToOffset(selectStartIndex);
            float leftX = Math.Min(selectOffset, caretRect.anchoredPosition.x);
            float width = Math.Abs(selectOffset - caretRect.anchoredPosition.x);
            selectionRect.anchoredPosition = new Vector2(leftX, selectionRect.anchoredPosition.y);
            selectionRect.sizeDelta = new Vector2(width, selectionRect.sizeDelta.y);
        }

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
        return SelectedInput == null || !SelectedInput.IsActive;
    }
}

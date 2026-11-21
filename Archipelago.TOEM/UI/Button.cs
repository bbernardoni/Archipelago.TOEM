using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Archipelago.TOEM.UI;

public class Button
{
    public LayoutElement layoutElem;
    public Text text;
    public Image bgImage;
    public UiButton button;

    public Button(string name, GameObject parent, bool small = false)
    {
        var connectHolder = new GameObject(name+" Button");
        connectHolder.transform.SetParent(parent.transform, false);
        connectHolder.AddComponent<RectTransform>();
        layoutElem = connectHolder.AddComponent<LayoutElement>();
        bgImage = connectHolder.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.2901961f);
        bgImage.type = Image.Type.Sliced;
        bgImage.sprite = Sprite.Create(Plugin.AssetManager.ScrollBox, new Rect(318, 350, 60, 64), 
            Vector2.zero, 100, 0, SpriteMeshType.Tight, new Vector4(29, 33, 29, 29));
        button = connectHolder.AddComponent<UiButton>();
        button.onClick = new UnityEvent();
        button.onHover = new UnityEvent();
        button.onExit = new UnityEvent();
        text = new(name, connectHolder);
        
        if (small)
        {
            layoutElem.preferredHeight = 43;
            layoutElem.minHeight = 43;
            layoutElem.minWidth = 80;
            text.textRect.anchoredPosition = Vector2.zero;
            text.textMesh.size = 30;
            text.textMesh.alignment = SuperTextMesh.Alignment.Center;
            text.textMesh.textMaterial = Plugin.AssetManager.UIDefault;
            text.textMesh.ColorSelect();
        }
        else
        {
            layoutElem.preferredHeight = 120;
            text.textMesh.ColorDeselect();
            button.onHover.AddListener((UnityAction)text.textMesh.ColorSelect);
            button.onExit.AddListener((UnityAction)text.textMesh.ColorDeselect);
        }
    }

    public void AddOnClick(Action onClickAction)
    {
        button.onClick.AddListener((UnityAction)onClickAction);
    }
}

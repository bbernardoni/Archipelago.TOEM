using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Linq;
using Il2CppInterop.Runtime;

namespace Archipelago.TOEM.UI;

public class Button
{
    public SuperTextMesh text;
    public UiButton button;

    public Button(string name, GameObject parent, Sprite sprite)
    {
        var connectHolder = new GameObject(name+" Button");
        connectHolder.transform.SetParent(parent.transform, false);
        connectHolder.AddComponent<RectTransform>();
        var chElem = connectHolder.AddComponent<LayoutElement>();
        chElem.preferredHeight = 120;
        var chImage = connectHolder.AddComponent<Image>();
        chImage.color = new Color(0, 0, 0, 0.2901961f);
        chImage.type = Image.Type.Sliced;
        chImage.sprite = sprite;
        button = connectHolder.AddComponent<UiButton>();
        
        var connectText = new GameObject("Text");
        connectText.transform.SetParent(connectHolder.transform, false);
        var ctRect = connectText.AddComponent<RectTransform>();
        ctRect.anchorMin = Vector2.zero;
        ctRect.anchorMax = Vector2.one;
        ctRect.anchoredPosition = new Vector2(32, 0);
        ctRect.sizeDelta = Vector2.zero;
        text = connectText.AddComponent<SuperTextMesh>();
        text.size = 70;
        text.text = name;
        text.anchor = TextAnchor.MiddleLeft;
        text.textMaterial = Plugin.AssetManager.UIShadow;
        text.ColorDeselect();
        
        button.onClick = new UnityEvent();
        button.onHover = new UnityEvent();
        button.onExit = new UnityEvent();
        button.onHover.AddListener((UnityAction)text.ColorSelect);
        button.onExit.AddListener((UnityAction)text.ColorDeselect);
    }
}

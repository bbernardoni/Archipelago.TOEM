using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Archipelago.TOEM.UI;

[HarmonyPatch]
public class Text
{
    private static readonly List<Text> Instances = [];

    public GameObject gameObject;
    public RectTransform textRect;
    public SuperTextMesh textMesh;
    public Font overrideFont = null;

    public Text(string name, GameObject parent)
    {
        gameObject = new GameObject(name == ""? "Text": name + " Text");
        gameObject.transform.SetParent(parent.transform, false);
        textRect = gameObject.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.anchoredPosition = new Vector2(32, 0);
        textRect.sizeDelta = Vector2.zero;
        textRect.pivot = Vector2.zero;
        textMesh = gameObject.AddComponent<SuperTextMesh>();
        textMesh.size = 70;
        textMesh.text = name;
        textMesh.anchor = TextAnchor.MiddleLeft;
        textMesh.textMaterial = Plugin.AssetManager.UIShadow;
        textMesh.ColorSelect();

        Instances.Add(this);
    }
    
    public static void UpdateAll()
    {
        // Remove destroyed inputs
        for (int i = Instances.Count - 1; i >= 0; i--)
            if (Instances[i].gameObject == null)
                Instances.RemoveAt(i);
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

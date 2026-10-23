using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using Menus;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Archipelago.TOEM;

[HarmonyPatch]
public class UIOptions
{
    private static List<GameObject> apCategoryGameObjects;

    [HarmonyPostfix, HarmonyPatch(typeof(OptionsMenu), nameof(OptionsMenu.OnMenuInitialize))]
    public static void OnMenuInitialize_Postfix(OptionsMenu __instance)
    {
        Plugin.Logger.LogInfo("OptionsMenu.OnMenuInitialize() postfix");
        var scrollFieldTransform = __instance.transform.GetChild(2);
        var tabFieldTransform = scrollFieldTransform.GetChild(1);
        if(tabFieldTransform.childCount == 2)
        {
            // Tab UI
            var firstTab = tabFieldTransform.GetChild(0);
            var firstTabButton = firstTab.GetComponent<UiButton>();

            var apTab = new GameObject("Tab - AP");
            apTab.transform.SetParent(tabFieldTransform, false);
            var apTabRect = apTab.AddComponent<RectTransform>();
            apTab.AddComponent<LayoutElement>().preferredWidth = 220;
            var apTabBgImage = apTab.AddComponent<Image>();
            apTabBgImage.color = new Color(0.6509f, 0.6509f, 0.6509f);
            apTabBgImage.type = Image.Type.Sliced;
            apTabBgImage.sprite = firstTab.GetComponent<Image>().sprite;
            var apTabButton = apTab.AddComponent<UiButton>();
            apTabButton.animateButton = false;
            apTabButton.customAudioReference = firstTabButton.customAudioReference;
            apTabButton.ignoreSystem = true;
            apTabButton.useAudioReference = true;

            var apTabImageObj = new GameObject("Image");
            apTabImageObj.transform.SetParent(apTab.transform, false);
            apTabImageObj.AddComponent<RectTransform>().localScale = new Vector3(0.85f, 0.85f, 1f);
            var tex = new Texture2D(256, 256, TextureFormat.RGBA32, false);
            ImageConversion.LoadImage(tex, Plugin.Game.ApLogoData);
            var ApLogo = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero);
            var apTabImage = apTabImageObj.AddComponent<Image>();
            apTabImage.color = new Color(0.8f, 0.8f, 0.8f, 0.2509804f);
            apTabImage.sprite = ApLogo;
            
            var tabScrollRightRect = scrollFieldTransform.GetChild(4).GetComponent<RectTransform>();
            var tabScrollRightPos = tabScrollRightRect.anchoredPosition;
            tabScrollRightPos.x = 697f;
            tabScrollRightRect.anchoredPosition = tabScrollRightPos;

            var tabField = tabFieldTransform.GetComponent<TabField>();
            var onTabPressed = new UnityEvent();
            onTabPressed.AddListener((UnityAction)(() => __instance.ViewCategoryTab(2)));
            var apTabFieldTap = new TabField.Tab
            {
                image = apTabBgImage,
                onPressed = onTabPressed,
                rectTransform = apTabRect
            };
            tabField.tabs.Add(apTabFieldTap);
            apTabButton.onClick = new UnityEvent();
            apTabButton.onClick.AddListener((UnityAction)(() => tabField.TabPressed(apTabRect)));

            // Options
            apCategoryGameObjects = [];
            
            __instance.AddOptionsDivider(NewLocString("AP/Options", "AP options"), (OptionsMenu.OptionsCategory)2);
            AddOptionsCommand(__instance, "Open Save Directory", "UI/button_select", 
                (Il2CppSystem.Action<bool>)OpenSaveDir);
            AddOptionsCommand(__instance, "Delete AP Saves", "UI/button_delete", 
                (Il2CppSystem.Action<bool>)PromptDeleteAPSaves);
                
            I2.Loc.LocalizationManager.Sources[0].UpdateDictionary();
        }
    }

    public static I2.Loc.LocalizedString NewLocString(string term, string english)
    {
        var sourceData = I2.Loc.LocalizationManager.Sources[0];
        I2.Loc.TermData apLocTest = new()
        {
            Term = term,
            Languages = new string[] { 
                english, english, english, english,
                english, english, english, english,
                english, english, english, english,
            }
        };
        sourceData.mTerms.Add(apLocTest);
        return new I2.Loc.LocalizedString() { mTerm = term };
    }

    public static void AddOptionsCommand(OptionsMenu menu, string name, string buttonTerm, Il2CppSystem.Action<bool> action)
    {
        var optionName = NewLocString("AP/Options/"+name.Replace(' ', '_'), name);
        menu.AddOptionsCommandToggle(optionName, name, (OptionsMenu.OptionsCategory)2, action);
        var option = menu.options[^1];
        option.type = (OptionsButton.Type)3;
        option.localizedInfoText = new I2.Loc.LocalizedString() { mTerm = buttonTerm };
    }
      
    [HarmonyPrefix, HarmonyPatch(typeof(OptionsMenu), nameof(OptionsMenu.AddOptionToCategoryList))]
    public static bool AddOptionToCategoryList(ref GameObject createdOptionGameObject, int category)
    {
        if (category != 2)
            return true;

        apCategoryGameObjects.Add(createdOptionGameObject);
        return false;
    }
    
    [HarmonyPrefix, HarmonyPatch(typeof(OptionsMenu), nameof(OptionsMenu.ViewCategoryTab))]
    public static bool ViewCategoryTab(OptionsMenu __instance, int desiredCategory)
    {
        if ((int)__instance.currentViewedCategory == desiredCategory)
            return false;

        foreach (var obj in __instance.genericCategoryGameObjects)
            obj.SetActive(false);
        foreach (var obj in __instance.gameplayCategoryGameObjects)
            obj.SetActive(false);
        foreach (var obj in apCategoryGameObjects)
            obj.SetActive(false);

        __instance.currentViewedCategory = (OptionsMenu.OptionsCategory)desiredCategory;

        if (desiredCategory == 0)
            foreach (var obj in __instance.genericCategoryGameObjects)
                obj.SetActive(true);
        else if (desiredCategory == 1)
            foreach (var obj in __instance.gameplayCategoryGameObjects)
                obj.SetActive(true);
        else if (desiredCategory == 2)
            foreach (var obj in apCategoryGameObjects)
                obj.SetActive(true);

        __instance.scrollField.UpdateScrollField();
        __instance.ForceRebuildTexts();
        if(OurInputManager.isUsingDirectionalInput)
            UiButtonSystem.Instance.SetTargetButton(__instance.scrollField.GetTopButton());
        return false;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(OptionsButton), nameof(OptionsButton.SetupOptionButton))]
    public static bool SetupOptionButton(OptionsButton __instance, OptionsMenu.Option option)
    {
        if (option.type != (OptionsButton.Type)3)
            return true;

        __instance.buttonActionBool += option.toggleAction;
        __instance.SetToggleGraphic(true);
        __instance.myType = option.type;
        __instance.quickLocalizeOptionTitle.value = option.buttonName;
        __instance.quickLocalizeOptionTitle.UpdateText();
        __instance.objIndex.SetActive(false);
        __instance.objSlider.SetActive(false);
        __instance.objToggle.SetActive(true);
        __instance.imageOff.gameObject.SetActive(false);
        var buttonText = __instance.imageOn.transform.GetChild(0).GetComponent<QuickLocalizeText>();
        buttonText.value = option.localizedInfoText;
        buttonText.UpdateText();
        return false;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(OptionsMenu), nameof(OptionsMenu.SaveOptionSettings))]
    public static void SaveOptionSettings(OptionsMenu __instance, out Il2CppSystem.Collections.Generic.List<OptionsMenu.Option> __state)
    {
        __state = __instance.options;
        Il2CppSystem.Collections.Generic.List<OptionsMenu.Option> saveOptions = new();
        foreach (var option in __instance.options)
            if (option.type != (OptionsButton.Type)3)
                saveOptions.Add(option);
        __instance.options = saveOptions;
    }

    [HarmonyPostfix, HarmonyPatch(typeof(OptionsMenu), nameof(OptionsMenu.SaveOptionSettings))]
    public static void SaveOptionSettings_Postfix(OptionsMenu __instance, Il2CppSystem.Collections.Generic.List<OptionsMenu.Option> __state)
    {
        __instance.options = __state;
    }

    public static void OpenSaveDir(bool state)
    {
        if(Directory.Exists(Application.persistentDataPath))
        {
            Application.OpenURL(Application.persistentDataPath);
        }
    }
    
    public static void PromptDeleteAPSaves(bool state)
    {
        MenuManager.ManualInitialization(MenuManager.Instance.menu_ConfirmationPrompt);
        MenuManager.Instance.DisplayConfirmationPrompt("Delete AP Saves", "This will delete all AP saves.",
            (Il2CppSystem.Action)DeleteAPSaves);
    }

    public static void DeleteAPSaves()
    {
        if(Directory.Exists(Application.persistentDataPath))
            foreach (var folder in Directory.EnumerateDirectories(Application.persistentDataPath, "AP_*"))
                Directory.Delete(folder, true);
    }
}

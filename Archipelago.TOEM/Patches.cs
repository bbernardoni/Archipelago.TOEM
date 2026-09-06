using HarmonyLib;

namespace Archipelago.TOEM;

[HarmonyPatch(typeof(GameManager))]
internal class GameManager_Patch
{
    static public bool resetOnMenuRemoved = false;

    [HarmonyPrefix, HarmonyPatch(nameof(GameManager.StartGame))]
    public static void StartGame()
    {
        Plugin.Logger.LogInfo("GameManager.StartGame()");
        Plugin.Client.Connect();
    }

    [HarmonyPrefix, HarmonyPatch(nameof(GameManager.Update))]
    public static void Update()
    {
        Plugin.Game?.Update();
        Plugin.LocationManager?.Update();
        if (resetOnMenuRemoved && CommunityController.instance.onMenuRemovedFromStack != null)
        {
            CommunityController.instance.onMenuRemovedFromStack.Invoke();
            CommunityController.instance.onMenuRemovedFromStack = null;
            OurInputManager.playerHasControl = true;
            resetOnMenuRemoved = false;
        }
    }
}

[HarmonyPatch(typeof(SaveManager))]
internal class SaveManager_Patch
{
    static public string ItemIndexSaveKey = "ArchipelagoItemIndex";
    static public string UnlockedAreasCountSaveKey = "unlockedAreasCount";

    [HarmonyPrefix, HarmonyPatch(nameof(SaveManager.SaveGame))]
    public static void SaveGame()
    {
        Plugin.Logger.LogInfo("SaveManager.SaveGame()");
        SaveManager._GameSave_k__BackingField[ItemIndexSaveKey] = Plugin.State.ItemIndex;
        SaveManager._GameSave_k__BackingField[UnlockedAreasCountSaveKey] = Menus.MapMenu.Instance.unlockedAreasCount;
    }

    [HarmonyPrefix, HarmonyPatch(nameof(SaveManager.OnLoadDone))]
    public static void OnLoadDone()
    {
        Plugin.Logger.LogInfo("SaveManager.OnLoadDone()");
        if (SaveManager._GameSave_k__BackingField.HasKey(ItemIndexSaveKey))
        {
            Plugin.State.ItemIndex = SaveManager._GameSave_k__BackingField[ItemIndexSaveKey];
            Plugin.Logger.LogInfo($"{nameof(ItemIndexSaveKey)}: {Plugin.State.ItemIndex}");
        }
        Plugin.Game.UnlockRegions();
    }

    [HarmonyPrefix, HarmonyPatch(nameof(SaveManager.ResetGame))]
    public static void ResetGame()
    {
        Plugin.Logger.LogInfo("SaveManager.ResetGame()");
        Plugin.Game.SetupNewSave();
    }
}

[HarmonyPatch(typeof(Menus.MapMenu))]
internal class MapMenu_Patch
{
    [HarmonyPrefix, HarmonyPatch(nameof(Menus.MapMenu.LoadUnlockedRegions))]
    public static bool LoadUnlockedRegions(Menus.MapMenu __instance)
    {
        Plugin.Logger.LogInfo($"Menus.MapMenu.LoadUnlockedRegions()");
        if (!SaveManager._GameSave_k__BackingField.HasKey(SaveManager_Patch.UnlockedAreasCountSaveKey))
            return true;

        int unlockedAreasCount = SaveManager._GameSave_k__BackingField[SaveManager_Patch.UnlockedAreasCountSaveKey];
        __instance.ResetMap();
        for (int i = 1; i < unlockedAreasCount+1 && i < __instance.mapPaths.Count; i++)
        {
            __instance.mapPaths[i].UnlockRegion();
        }
        __instance.unlockedAreasCount = unlockedAreasCount;

        if (SaveManager._GameSave_k__BackingField.HasKey("Map Status"))
        {
            Menus.MapMenu.shouldUnlockNextRegion = SaveManager._GameSave_k__BackingField["Map Status"]["shouldUnlockNextRegion"].AsBool;
        }

        __instance.InitializeMap();
        return false;
    }
    
    [HarmonyPostfix, HarmonyPatch(nameof(Menus.MapMenu.LoadUnlockedRegions))]
    public static void LoadUnlockedRegions_Postfix()
    {
        Plugin.Logger.LogInfo($"Menus.MapMenu.LoadUnlockedRegions() Postfix");
        Plugin.Game.UnlockRegions();
        Plugin.Game.SetStampRequirements = true;
    }
}
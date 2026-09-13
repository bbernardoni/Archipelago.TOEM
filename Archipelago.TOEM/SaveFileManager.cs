using HarmonyLib;

namespace Archipelago.TOEM;

public class SaveFileManager
{
}

[HarmonyPatch(typeof(LaundryBearStorage))]
public class LaundryBearStorage_Patch
{
    [HarmonyPrefix, HarmonyPatch(nameof(LaundryBearStorage.CreateDefaultFolders))]
    public static void CreateDefaultFolders()
    {
        Plugin.Logger.LogInfo("LaundryBearStorage.CreateDefaultFolders()");
    }
    
    /*
    public unsafe void DeletePhotoFromDisk(int photoIndex, string saveFile, Il2CppSystem.Action<bool> onComplete)
    public unsafe void DeleteSave(Il2CppSystem.Action<Result> onComplete)
    public unsafe void DeleteFile(string path, Il2CppSystem.Action<Result> onComplete)
    public unsafe void DeleteFolders(Il2CppStringArray paths, Il2CppSystem.Action<Result> onComplete)
    public unsafe void CreateFolders(Il2CppStringArray paths, Il2CppSystem.Action<Result> onComplete)
    public unsafe void DisplayPlatformKeyboard(string initialText, Il2CppSystem.Action<string> onComplete)
    public unsafe void Initialize(Il2CppSystem.Action<Result> onComplete)
    public unsafe void CreateDefaultFolders(Il2CppSystem.Action<Result> onComplete)
    public unsafe void Load(string fileToLoad, Il2CppSystem.Action<Result, string> onComplete)
    public unsafe void LoadPhotosBytes(Il2CppReferenceArray<PhotoInfo> photosInfo, Il2CppSystem.Action<Result, Il2CppReferenceArray<PhotoInfo>> onComplete)
    public unsafe void LoadPhotoBytes(PhotoTypeToLoad photoTypeToLoad, string fileName, Il2CppSystem.Action<Result, Il2CppStructArray<byte>> onComplete)
    public unsafe void LoadSettings(Il2CppSystem.Action<Result, string> onComplete)
    public unsafe void OnQuit()
    public unsafe void SaveFile(string path, string saveFile, Il2CppSystem.Action<Result> onComplete)
    public unsafe void ReSavePhotoToDisk(string imagePath, Texture2D photoTexture2D, Il2CppSystem.Action<Result> onComplete)
    public unsafe void SaveAlbumPhoto(int photoIndex, Texture2D photoTexture2D, string saveFile, Il2CppSystem.Action<Result> onComplete)
    public unsafe void SaveCompendiumPhoto(Texture2D photoTexture, string photoName, string saveFile, Il2CppSystem.Action<Result> onComplete)
    public unsafe void SaveWorldPhoto(Texture2D photoToSave, string fileName, string saveFile, Il2CppSystem.Action<Result> onComplete)
    public unsafe void SaveSettings(string settingsString, Il2CppSystem.Action<Result> onComplete)
    public unsafe void DeleteDetachedPhotos()
    public unsafe void PerformDeletePhotosInFolderOperation(StringBuilder stringBuilder, Il2CppStringArray photosToCheckFor, PhotoTypeToLoad photoTypeToLoad)
    */
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
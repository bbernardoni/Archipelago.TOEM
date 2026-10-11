using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime;
using System;
using Quests;
using SimpleJSON;
using System.Collections.Generic;

namespace Archipelago.TOEM;

[HarmonyPatch]
public class SaveFileManager
{
    private bool _shouldStartGame = false;
    private static readonly string ItemIndexSaveKey = "ArchipelagoItemIndex";

    [HarmonyPrefix, HarmonyPatch(typeof(TitleScreenMenu), nameof(TitleScreenMenu.StartGame))]
    public static bool StartGame()
    {
        Plugin.Logger.LogInfo("TitleScreenMenu.StartGame()");
        if (!Plugin.Client.Connected)
        {
            Plugin.Client.Connect(true);
            return false;
        }
        return true;
    }
    
    public void ConnectSave(bool shouldStartGame)
    {
        _shouldStartGame = shouldStartGame;

        string saveFolder = GetAPSaveName();
        if(GetRootFolder() == saveFolder)
        {
            OnLoaded();
            return;
        }

        SetRootFolder(saveFolder);
        SaveManager.storage.m_storage.DirectoryExists("", (Action<LaundryBear.PlatformServices.StorageResult>)CheckDirectory);
    }

    public void OnLoaded()
    {
        Plugin.Logger.LogInfo($"SaveFileManager loaded {GetRootFolder()}");
        foreach (var region in GameManager.instance.regionData.regionInfo)
        {
            switch (region.region)
            {
                case Quest.QuestRegion.Home:
                    region.requiredStamps = Plugin.State.SlotData.Options.homelanda_stamp_requirement;
                    break;
                case Quest.QuestRegion.Forest:
                    region.requiredStamps = Plugin.State.SlotData.Options.oaklaville_stamp_requirement;
                    break;
                case Quest.QuestRegion.Harbor:
                    region.requiredStamps = Plugin.State.SlotData.Options.stanhamn_stamp_requirement;
                    break;
                case Quest.QuestRegion.City:
                    region.requiredStamps = Plugin.State.SlotData.Options.logcity_stamp_requirement;
                    break;
                case Quest.QuestRegion.Mountain:
                    region.requiredStamps = Plugin.State.SlotData.Options.kiiruberg_stamp_requirement;
                    break;
                case Quest.QuestRegion.Resort:
                    region.requiredStamps = Plugin.State.SlotData.Options.basto_stamp_requirement;
                    break;
            }
        }

        Plugin.Game.ConnectSave();
        if (_shouldStartGame)
        {
            MenuManager.Instance.menu_TitleScreen.GetComponent<TitleScreenMenu>().StartGame();
            _shouldStartGame = false;
        }
    }

    public void CheckDirectory(LaundryBear.PlatformServices.StorageResult result)
    {
        if (result != LaundryBear.PlatformServices.StorageResult.Success)
        {
            Plugin.Logger.LogInfo("SaveFileManager creating new save");
            SaveManager.ResetGame(true, false);
        }
        else
        {
            Plugin.Logger.LogInfo("SaveFileManager loading exiting save");
            SaveManager.LoadGame(null);
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(SaveManager.__c__DisplayClass59_0), nameof(SaveManager.__c__DisplayClass59_0._ResetGame_b__1))]
    public static void ResetGameFinished()
    {
        Plugin.SaveFileManager.OnLoaded();
    }

    [HarmonyPrefix, HarmonyPatch(typeof(SaveManager), nameof(SaveManager.OnLoadDone))]
    public static void OnLoadDone()
    {
        Plugin.Logger.LogInfo("SaveManager.OnLoadDone()");
        if(GetRootFolder() != "Save")
        {
            SceneTransitionController.Instance.SetupLastPlayedScenePathFromSave();
            
            if (SaveManager._GameSave_k__BackingField.HasKey(ItemIndexSaveKey))
            {
                Plugin.State.ItemIndex = SaveManager._GameSave_k__BackingField[ItemIndexSaveKey];
                Plugin.Logger.LogInfo($"{nameof(ItemIndexSaveKey)}: {Plugin.State.ItemIndex}");
            }

            Plugin.SaveFileManager.OnLoaded();
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(SaveManager), nameof(SaveManager.LoadGame))]
    public static void LoadGame()
    {
        if(GetRootFolder() == "Save" && Plugin.Client.Connected)
        {
            // Happens when returning to menu while connected
            SetRootFolder(GetAPSaveName());
            Plugin.SaveFileManager._shouldStartGame = false;
        }
    }

    // Region unlock overrides
    [HarmonyPrefix, HarmonyPatch(typeof(CommunityController), nameof(CommunityController.SaveStamps))]
    public static bool SaveStamps(CommunityController __instance)
    {
        JSONArray cardArray = new();
        foreach (var region in __instance.regionDataScriptableObject.regionInfo)
        {
            JSONObject cardRegionData = new();
            cardRegionData["RegionID"] = region.region.ToString();
            cardRegionData["isUnlocked"] = region.isUnlocked;
            if (region.region != Quest.QuestRegion.MountainTop)
            {
                cardRegionData["StampCount"] = region.currentStampCount;
                cardRegionData["HasRegionStamp"] = region.hasRegionStamp;
                cardRegionData["HasSightPhoto"] = region.hasSightPhoto;
                cardRegionData["AllQuestCompletedReaction"] = region.kioskyHasReactedOnAllQuestsCompleted;
                
                JSONArray positions = new();
                foreach (var postition in region.stampPositions)
                {
                    JSONArray positionArray = new();
                    positionArray.Add(Math.Round(postition.x, 2));
                    positionArray.Add(Math.Round(postition.y, 2));
                    positions.Add(positionArray);
                }
                cardRegionData["Positions"] = positions;
            }
            cardArray.Add(cardRegionData);
        }
        SaveManager._GameSave_k__BackingField["CommunityCard"] = cardArray;
        return false;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(CommunityController), nameof(CommunityController.SetUIButtonsShowState))]
    public static void SetUIButtonsShowState(CommunityController __instance, out Dictionary<Quest.QuestRegion, bool> __state)
    {
        Dictionary<Quest.QuestRegion, bool> unlockedDict = new();
        foreach (var region in __instance.regionDataScriptableObject.regionInfo)
        {
            unlockedDict[region.region] = region.isUnlocked;
            region.isUnlocked = true;
        }
        __state = unlockedDict;
    }

    [HarmonyPostfix, HarmonyPatch(typeof(CommunityController), nameof(CommunityController.SetUIButtonsShowState))]
    public static void SetUIButtonsShowState_Postfix(CommunityController __instance, Dictionary<Quest.QuestRegion, bool> __state)
    {
        foreach (var region in __instance.regionDataScriptableObject.regionInfo)
            region.isUnlocked = __state[region.region];
    }

    [HarmonyPrefix, HarmonyPatch(typeof(SaveManager), nameof(SaveManager.SaveGame))]
    public static void SaveGame()
    {
        Plugin.Logger.LogInfo("SaveManager.SaveGame()");
        SaveManager._GameSave_k__BackingField[ItemIndexSaveKey] = Plugin.State.ItemIndex;
    }
    
    public static string GetAPSaveName()
    {
        return $"AP_{Plugin.Client.GetSeedName()}_{Plugin.Client.GetCurrentPlayer()}";
    }

    // Get/Set Root save folder
    public static void SetRootFolder(string rootFolder)
    {
        // Reflection jank to get around platform specific storage
        var cppBase = (Il2CppObjectBase)SaveManager.storage.m_storage;
        IntPtr classPtr = IL2CPP.il2cpp_object_get_class(cppBase.Pointer);
        Il2CppSystem.Type cppType = Il2CppType.TypeFromPointer(classPtr);
        Type monoType = Type.GetType(cppType.AssemblyQualifiedName);
        var castedStorage = Activator.CreateInstance(monoType, cppBase.Pointer);

        var m_rootFolder = monoType.GetProperty("m_rootFolder");
        m_rootFolder.SetValue(castedStorage, rootFolder);
    }

    public static string GetRootFolder()
    {
        // Reflection jank to get around platform specific storage
        var cppBase = (Il2CppObjectBase)SaveManager.storage.m_storage;
        IntPtr classPtr = IL2CPP.il2cpp_object_get_class(cppBase.Pointer);
        Il2CppSystem.Type cppType = Il2CppType.TypeFromPointer(classPtr);
        Type monoType = Type.GetType(cppType.AssemblyQualifiedName);
        var castedStorage = Activator.CreateInstance(monoType, cppBase.Pointer);

        var m_rootFolder = monoType.GetProperty("m_rootFolder");
        return (string)m_rootFolder.GetValue(castedStorage);
    }

    // For debugging
    [HarmonyPrefix, HarmonyPatch(typeof(LaundryBearStorage), nameof(LaundryBearStorage.SaveFile))]
    public static void SaveFile(string path)
    {
        Plugin.Logger.LogInfo($"LaundryBearStorage.SaveFile({path})");
    }

    [HarmonyPrefix, HarmonyPatch(typeof(LaundryBearStorage), nameof(LaundryBearStorage.Load))]
    public static void Load(string fileToLoad)
    {
        Plugin.Logger.LogInfo($"LaundryBearStorage.Load({fileToLoad})");
    }

    // Take settings from base Save folder
    [HarmonyPrefix, HarmonyPatch(typeof(LaundryBearStorage), nameof(LaundryBearStorage.LoadSettings))]
    public static void LoadSettings(out string __state)
    {
        __state = GetRootFolder();
        SetRootFolder("Save");
    }

    [HarmonyPostfix, HarmonyPatch(typeof(LaundryBearStorage), nameof(LaundryBearStorage.LoadSettings))]
    public static void LoadSettings_Postfix(string __state)
    {
        SetRootFolder(__state);
    }

    [HarmonyPrefix, HarmonyPatch(typeof(LaundryBearStorage), nameof(LaundryBearStorage.SaveSettings))]
    public static void SaveSettings(out string __state)
    {
        __state = GetRootFolder();
        SetRootFolder("Save");
    }

    [HarmonyPostfix, HarmonyPatch(typeof(LaundryBearStorage), nameof(LaundryBearStorage.SaveSettings))]
    public static void SaveSettings_Postfix(string __state)
    {
        SetRootFolder(__state);
    }
}

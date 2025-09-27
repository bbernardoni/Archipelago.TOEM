using HarmonyLib;
using Photographing;
using Quests;
using Dialogue;

namespace Archipelago.TOEM;

[HarmonyPatch(typeof(CommunityController))]
internal class CommunityController_Patch
{
    static bool resetOnMenuRemoved = false;

    [HarmonyPatch(nameof(CommunityController.GetStamp))]
    [HarmonyPrefix]
    public static bool GetStamp(Quest completedQuest)
    {
        Plugin.Logger.LogInfo($"CommunityController.GetStamp({completedQuest})");
        if (Plugin.Client.Connected && Data.QuestToApLocationId.TryGetValue(completedQuest.jsonSaveKey, out var apLocation))
        {
            Plugin.Client.SendLocation((long)apLocation);
        }
        var node = TextboxController.currentDialogueGraph.currentNode;
        node.TryCast<SetQuestStatusNode>().ProceedAfterStampingCard();
        // Delegate isn't setup until after this call. Delay removing until next Update prefix.
        resetOnMenuRemoved = true;
        return false;
    }

    [HarmonyPatch(nameof(CommunityController.Update))]
    [HarmonyPrefix]
    public static void Update()
    {
        if (resetOnMenuRemoved)
        {
            CommunityController.instance.onMenuRemovedFromStack = null;
            resetOnMenuRemoved = false;
        }
    }
}

// [HarmonyPatch(typeof(RegionData.RegionInfo))]
// internal class RegionData_RegionInfo_Patch
// {
//     [HarmonyPatch(nameof(RegionData.RegionInfo.HasEnoughStampsForTicket))]
//     [HarmonyPrefix]
//     public static void HasEnoughStampsForTicket()
//     {
//         Plugin.Logger.LogInfo("RegionData.RegionInfo.HasEnoughStampsForTicket()");
//     }
// }

// [HarmonyPatch(typeof(PlayerController))]
// internal class PlayerController_Patch
// {
//     [HarmonyPatch(nameof(PlayerController.ChangePlayerState), typeof(PlayerController.StatesEnum))]
//     [HarmonyPrefix]
//     public static void ChangePlayerState()
//     {
//         Plugin.Logger.LogInfo("PlayerController.ChangePlayerState()");
//     }
// }

[HarmonyPatch(typeof(GameManager))]
internal class GameManager_Patch
{
    // [HarmonyPatch(nameof(GameManager.GetCurrentRegionInfo))]
    // [HarmonyPrefix]
    // public static void GetCurrentRegionInfo()
    // {
    //     Plugin.Logger.LogInfo("GameManager.GetCurrentRegionInfo()");
    // }

    [HarmonyPatch(nameof(GameManager.StartGame))]
    [HarmonyPrefix]
    public static void StartGame()
    {
        Plugin.Logger.LogInfo("GameManager.StartGame()");
        Plugin.Client.Connect();
    }

    [HarmonyPatch(nameof(GameManager.Update))]
    [HarmonyPrefix]
    public static void Update()
    {
        Plugin.Game?.Update();
    }
}

// [HarmonyPatch(typeof(SaveManager))]
// internal class SaveManager_Patch
// {
//     [HarmonyPatch(nameof(SaveManager.SaveGame))]
//     [HarmonyPrefix]
//     public static void SaveGame()
//     {
//         Plugin.Logger.LogInfo("SaveManager.SaveGame()");
//     }
// }

[HarmonyPatch(typeof(QuestManager))]
internal class QuestManager_Patch
{
    [HarmonyPatch(nameof(QuestManager.AddQuestStatus))]
    [HarmonyPrefix]
    public static void AddQuestStatus(Quest questToSave)
    {
        Plugin.Logger.LogInfo($"QuestManager.AddQuestStatus({questToSave.jsonSaveKey})");
        Plugin.Game.LastUpdatedQuest = questToSave?.jsonSaveKey;
    }
}

[HarmonyPatch(typeof(PhotoCompendium))]
internal class PhotoCompendium_Patch
{
    [HarmonyPatch(nameof(PhotoCompendium.AddToCompendium))]
    [HarmonyPrefix]
    public static void AddToCompendium(CompendiumPhotoTag tagToSave)
    {
        Plugin.Logger.LogInfo($"PhotoCompendium.AddToCompendium({tagToSave.creatureName})");
        if (Plugin.Client.Connected && Data.CreatureToApLocationId.TryGetValue(tagToSave.creatureName, out var apLocation))
        {
            Plugin.Client.SendLocation((long)apLocation);
        }
    }
}

[HarmonyPatch(typeof(PlayerInventory))]
internal class PlayerInventory_Patch
{
    [HarmonyPatch(nameof(PlayerInventory.AddItem))]
    [HarmonyPrefix]
    public static bool AddItem(Item_SO itemToAdd, int count, bool addedFromSaveFile)
    {
        Plugin.Logger.LogInfo($"PlayerInventory.AddItem({itemToAdd.jsonSaveKey}, {count}, {addedFromSaveFile})");
        bool itemFound = Data.ItemToApLocationId.TryGetValue(itemToAdd.jsonSaveKey, out var apLocation);
        if (!Plugin.Game.IsServerItem && Plugin.Client.Connected && itemFound)
        {
            Plugin.Client.SendLocation((long)apLocation);
        }
        bool continueFunction = Plugin.Game.IsServerItem || !itemFound;
        Plugin.Game.IsServerItem = false;
        return continueFunction;
    }
}

[HarmonyPatch(typeof(GetItemScreen))]
internal class GetItemScreen_Patch
{
    [HarmonyPatch(nameof(GetItemScreen.CheckCloseMenu))]
    [HarmonyPostfix]
    public static void CheckCloseMenu()
    {
        var equipmentPrompt = GetItemScreen.instance.equipmentPrompt;
        if (equipmentPrompt.active)
        {
            MenuManager.Instance.CloseMenu();
            equipmentPrompt.SetActive(false);
        }
    }
}

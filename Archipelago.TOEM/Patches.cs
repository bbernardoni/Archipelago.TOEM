using HarmonyLib;
using Photographing;
using Quests;

namespace Archipelago.TOEM;

[HarmonyPatch(typeof(CommunityController))]
internal class CommunityController_Patch
{
    [HarmonyPatch(nameof(CommunityController.SpawnStamp))]
    [HarmonyPrefix]
    public static void SpawnStamp()
    {
        Plugin.Logger.LogInfo($"CommunityController.SpawnStamp({Plugin.Game.LastUpdatedQuest})");
        if (Plugin.Client.Connected && Plugin.Game.LastUpdatedQuest != null && Data.QuestToApLocationId.TryGetValue(Plugin.Game.LastUpdatedQuest, out var apLocation))
        {
            Plugin.Client.SendLocation((long)apLocation);
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
    public static void AddItem(Item_SO itemToAdd, int count, bool addedFromSaveFile)
    {
        Plugin.Logger.LogInfo($"PlayerInventory.AddItem({itemToAdd.jsonSaveKey}, {count}, {addedFromSaveFile})");
        if (Plugin.Client.Connected && Data.ItemToApLocationId.TryGetValue(itemToAdd.jsonSaveKey, out var apLocation))
        {
            Plugin.Client.SendLocation((long)apLocation);
        }
    }
}

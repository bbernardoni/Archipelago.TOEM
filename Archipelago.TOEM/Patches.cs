using HarmonyLib;
using Photographing;
using Quests;
using Dialogue;
using System.Diagnostics;

namespace Archipelago.TOEM;

[HarmonyPatch(typeof(CommunityController))]
internal class CommunityController_Patch
{
    static public bool resetOnMenuRemoved = false;

    [HarmonyPatch(nameof(CommunityController.GetStamp))]
    [HarmonyPrefix]
    public static bool GetStamp(Quest completedQuest)
    {
        Plugin.Logger.LogInfo($"CommunityController.GetStamp({completedQuest})");
        bool include_basto = Plugin.State.SlotData?.Options.Include_Basto ?? true;
        bool found = Data.QuestToApLocationId.TryGetValue(completedQuest.jsonSaveKey, out var apLocation);
        if (!found || (!include_basto && apLocation >= ApLocationId.FirstBasto))
            return true;

        Plugin.Game.CheckLocation(apLocation);
        if (apLocation == ApLocationId.QuestExperienceToem && !include_basto)
        {
            Plugin.Game.SendCompletion();
        }

        // Delegate isn't setup until after this call. Delay removing until next Update prefix.
        resetOnMenuRemoved = true;
        return false;
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

[HarmonyPatch(typeof(GameManager))]
internal class GameManager_Patch
{
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
        if (CommunityController_Patch.resetOnMenuRemoved && CommunityController.instance.onMenuRemovedFromStack != null)
        {
            CommunityController.instance.onMenuRemovedFromStack.Invoke();
            CommunityController.instance.onMenuRemovedFromStack = null;
            OurInputManager.playerHasControl = true;
            CommunityController_Patch.resetOnMenuRemoved = false;
        }
    }
}

[HarmonyPatch(typeof(SaveManager))]
internal class SaveManager_Patch
{
    static string ItemIndexSaveKey = "ArchipelagoItemIndex";

    [HarmonyPatch(nameof(SaveManager.SaveGame))]
    [HarmonyPrefix]
    public static void SaveGame()
    {
        Plugin.Logger.LogInfo("SaveManager.SaveGame()");
        SaveManager._GameSave_k__BackingField[ItemIndexSaveKey] = Plugin.State.ItemIndex;
    }

    [HarmonyPatch(nameof(SaveManager.OnLoadDone))]
    [HarmonyPrefix]
    public static void OnLoadDone()
    {
        Plugin.Logger.LogInfo("SaveManager.OnLoadDone()");
        if (SaveManager._GameSave_k__BackingField.HasKey(ItemIndexSaveKey))
        {
            Plugin.State.ItemIndex = SaveManager._GameSave_k__BackingField[ItemIndexSaveKey];
            Plugin.Logger.LogInfo($"{nameof(ItemIndexSaveKey)}: {Plugin.State.ItemIndex}");
        }
    }

    [HarmonyPatch(nameof(SaveManager.ResetGame))]
    [HarmonyPrefix]
    public static void ResetGame()
    {
        Plugin.Logger.LogInfo("SaveManager.ResetGame()");
        Plugin.State.ClearSave();
        Plugin.Client.Disconnect();
    }
}

[HarmonyPatch(typeof(QuestManager))]
internal class QuestManager_Patch
{
    [HarmonyPatch(nameof(QuestManager.AddQuestStatus))]
    [HarmonyPrefix]
    public static void AddQuestStatus(Quest questToSave)
    {
        Plugin.Logger.LogInfo($"QuestManager.AddQuestStatus({questToSave.jsonSaveKey}), status: {questToSave.currentStatus}");
        bool include_basto = Plugin.State.SlotData?.Options.Include_Basto ?? true;
        if (questToSave.jsonSaveKey == "Kiosky Gate -Backend Quest" && questToSave.currentStatus == Quest.QuestStatus.Completed && include_basto)
        {
            Plugin.Game.SendCompletion();
        }
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
        bool include_basto = Plugin.State.SlotData?.Options.Include_Basto ?? true;
        bool found = Data.CreatureToApLocationId.TryGetValue(tagToSave.creatureName, out var apLocation);
        if (!found || (!include_basto && apLocation >= ApLocationId.FirstBasto))
            return;

        Plugin.Game.CheckLocation(apLocation);
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
        bool include_basto = Plugin.State.SlotData?.Options.Include_Basto ?? true;
        bool include_items = Plugin.State.SlotData?.Options.Include_Items ?? true;
        bool found = Data.ItemToApLocationId.TryGetValue(itemToAdd.jsonSaveKey, out var apLocation);
        if (addedFromSaveFile || Plugin.Game.IsServerItem || !found || !include_items ||
                (!include_basto && apLocation >= ApLocationId.FirstBasto))
            return true;

        Plugin.Game.CheckLocation(apLocation);
        return false;
    }
}

[HarmonyPatch(typeof(GetItemScreen))]
internal class GetItemScreen_Patch
{
    [HarmonyPatch(nameof(GetItemScreen.CheckCloseMenu))]
    [HarmonyPostfix]
    public static void CheckCloseMenu()
    {
        bool include_items = Plugin.State.SlotData?.Options.Include_Items ?? true;
        if (!include_items)
            return;

        var equipmentPrompt = GetItemScreen.instance.equipmentPrompt;
        if (equipmentPrompt.active)
        {
            MenuManager.Instance.CloseMenu();
            equipmentPrompt.SetActive(false);
        }
    }
}

[HarmonyPatch(typeof(ChestController))]
internal class ChestController_Patch
{
    [HarmonyPatch(nameof(ChestController.Start))]
    [HarmonyPrefix]
    public static bool Start(ChestController __instance)
    {
        Plugin.Logger.LogInfo($"ChestController.Start()");
        bool include_basto = Plugin.State.SlotData?.Options.Include_Basto ?? true;
        bool include_items = Plugin.State.SlotData?.Options.Include_Items ?? true;
        bool found = Data.ItemToApLocationId.TryGetValue(__instance.itemInside.jsonSaveKey, out var apLocation);
        if (!found || !include_items || (!include_basto && apLocation >= ApLocationId.FirstBasto))
            return true;

        if (Plugin.Client.IsLocationChecked((long)apLocation))
            OpenChest(__instance);
        return false;
    }

    private static void OpenChest(ChestController instance)
    {
        instance.anim?.Play(instance.chestOpenAnimHash, -1, float.NegativeInfinity);
        instance.onOpened?.Invoke();
        if (instance.myInteraction != null)
            instance.myInteraction.interactionActive = false;
    }
}

[HarmonyPatch(typeof(InventoryHasItem))]
internal class InventoryHasItem_Patch
{
    [HarmonyPatch(nameof(InventoryHasItem.ExecuteEvent), [])]
    [HarmonyPrefix]
    public static bool ExecuteEvent(InventoryHasItem __instance)
    {
        bool include_items = Plugin.State.SlotData?.Options.Include_Items ?? true;
        bool found = Data.ItemToApLocationId.TryGetValue(__instance.item.jsonSaveKey, out var apLocation);
        if (!found || !include_items)
            return true;
        if (apLocation != ApLocationId.ItemAwardMask && apLocation != ApLocationId.ItemGhostGlasses && apLocation != ApLocationId.ItemSandwich)
            return true;

        Plugin.Logger.LogInfo($"InventoryHasItem.ExecuteEvent() : {__instance.item.jsonSaveKey}");
        if (Plugin.Client.IsLocationChecked((long)apLocation))
        {
            if (!__instance.executeMoreThanOnce)
            {
                __instance.hasBeenTriggered = true;
            }
            __instance.hasItem.Invoke();
        }
        else
        {
            __instance.hasNotItem.Invoke();
        }
        return false;
    }
}

[HarmonyPatch(typeof(CheckItemNode))]
internal class CheckItemNode_Patch
{
    [HarmonyPatch(nameof(CheckItemNode.EvaluateConditions))]
    [HarmonyPrefix]
    public static bool EvaluateConditions(CheckItemNode __instance, ref bool __result)
    {
        bool include_items = Plugin.State.SlotData?.Options.Include_Items ?? true;
        if (!include_items)
            return true;

        foreach (var item in __instance.itemsToCheckFor)
        {
            bool found = Data.ItemToApLocationId.TryGetValue(item.jsonSaveKey, out var apLocation);
            if (found && (apLocation == ApLocationId.ItemTripod || apLocation == ApLocationId.ItemFlag ||
                    apLocation == ApLocationId.ItemSkiGoggles || apLocation == ApLocationId.ItemScarf))
            {
                __result = Plugin.Client.IsLocationChecked((long)apLocation);
                return false;
            }
        }

        return true;
    }
}
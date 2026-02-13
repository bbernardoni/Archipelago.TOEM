using HarmonyLib;
using Photographing;
using Quests;
using Dialogue;
using System.Text.RegularExpressions;

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
        bool include_basto = Plugin.State.SlotData?.Options.include_basto ?? true;
        bool found = Data.QuestToApLocationId.TryGetValue(completedQuest.jsonSaveKey, out var apLocation);
        if (!found || (!include_basto && apLocation >= ApLocationId.FirstBasto))
            return true;

        Plugin.Game.CheckLocation(apLocation);

        // Delegate isn't setup until after this call. Delay removing until next Update prefix.
        resetOnMenuRemoved = true;
        return false;
    }
}

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
    static public string ItemIndexSaveKey = "ArchipelagoItemIndex";
    static public string UnlockedAreasCountSaveKey = "unlockedAreasCount";

    [HarmonyPatch(nameof(SaveManager.SaveGame))]
    [HarmonyPrefix]
    public static void SaveGame()
    {
        Plugin.Logger.LogInfo("SaveManager.SaveGame()");
        SaveManager._GameSave_k__BackingField[ItemIndexSaveKey] = Plugin.State.ItemIndex;
        SaveManager._GameSave_k__BackingField[UnlockedAreasCountSaveKey] = Menus.MapMenu.Instance.unlockedAreasCount;
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
        Plugin.Game.UnlockRegions();
    }

    [HarmonyPatch(nameof(SaveManager.ResetGame))]
    [HarmonyPrefix]
    public static void ResetGame()
    {
        Plugin.Logger.LogInfo("SaveManager.ResetGame()");
        Plugin.Game.SetupNewSave();
    }
}

[HarmonyPatch(typeof(Menus.MapMenu))]
internal class MapMenu_Patch
{
    [HarmonyPatch(nameof(Menus.MapMenu.LoadUnlockedRegions))]
    [HarmonyPrefix]
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
    
    [HarmonyPatch(nameof(Menus.MapMenu.LoadUnlockedRegions))]
    [HarmonyPostfix]
    public static void LoadUnlockedRegions_Postfix()
    {
        Plugin.Logger.LogInfo($"Menus.MapMenu.LoadUnlockedRegions() Postfix");
        Plugin.Game.UnlockRegions();
        Plugin.Game.SetStampRequirements = true;
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
        bool include_basto = Plugin.State.SlotData?.Options.include_basto ?? true;
        bool found = Data.CreatureToApLocationId.TryGetValue(tagToSave.creatureName, out var apLocation);
        if (!found || (!include_basto && apLocation >= ApLocationId.FirstBasto))
            return;

        Plugin.Game.CheckLocation(apLocation);
    }
}

[HarmonyPatch(typeof(PlayerInventory))]
internal class PlayerInventory_Patch
{
    static public bool BastoTicketFound = false;

    [HarmonyPatch(nameof(PlayerInventory.AddItem))]
    [HarmonyPrefix]
    public static bool AddItem(Item_SO itemToAdd, int count, bool addedFromSaveFile)
    {
        if (addedFromSaveFile || Plugin.Game.IsServerItem)
            return true;

        Plugin.Logger.LogInfo($"PlayerInventory.AddItem({itemToAdd.jsonSaveKey}, {count}, {addedFromSaveFile})");
        bool include_basto = Plugin.State.SlotData?.Options.include_basto ?? true;
        bool include_items = Plugin.State.SlotData?.Options.include_items ?? true;
        bool include_cassettes = Plugin.State.SlotData?.Options.include_cassettes ?? true;
        bool found = Data.ItemToApLocationId.TryGetValue(itemToAdd.jsonSaveKey, out var apLocation);
        if (found && !include_items)
            return true;
        if (!found)
        {
            found = Data.CassetteToApLocationId.TryGetValue(itemToAdd.jsonSaveKey, out apLocation);
            if (found && !include_cassettes)
                return true;
        }
        if (!found || (!include_basto && apLocation >= ApLocationId.FirstBasto))
            return true;
        if (apLocation == ApLocationId.ItemEmptyBottle && Plugin.Client.IsLocationChecked((long)apLocation))
            return true;

        // Check ice creams individually
        if (apLocation == ApLocationId.ItemIceCreamBanakin)
        {
            foreach(var quest in GameManager.QuestDatabase.resortRegionQuests)
            {
                if (quest.currentStatus == Quest.QuestStatus.Completed)
                {
                    if (quest.jsonSaveKey == "Fruit - Banana")
                        Plugin.Game.CheckLocation(ApLocationId.ItemIceCreamBanakin);
                    else if (quest.jsonSaveKey == "Fruit - Pear")
                        Plugin.Game.CheckLocation(ApLocationId.ItemIceCreamMelonear);
                    else if (quest.jsonSaveKey == "Fruit - Bean")
                        Plugin.Game.CheckLocation(ApLocationId.ItemIceCreamBeanut);
                    else if (quest.jsonSaveKey == "Fruit - Orange")
                        Plugin.Game.CheckLocation(ApLocationId.ItemIceCreamOranganas);
                }
            }
        }
        else
        {
            Plugin.Game.CheckLocation(apLocation);
            if(apLocation == ApLocationId.ItemBastoTicket)
            {
                BastoTicketFound = true;
                PlayerInventory.onItemAdded.Invoke(itemToAdd);
            }
        }

        return false;
    }
    
    [HarmonyPatch(nameof(PlayerInventory.RemoveItem))]
    [HarmonyPrefix]
    public static bool RemoveItem(Item_SO itemToRemove, int count)
    {
        // Prevent pirate hat switching from removing an item that might be needed for the Cosplayer achievement
        if(itemToRemove.jsonSaveKey == "PirateHat" || itemToRemove.jsonSaveKey == "PaperHat")
            return false;
        return true;
    }
    
    [HarmonyPatch(nameof(PlayerInventory.ContainsItem))]
    [HarmonyPrefix]
    public static bool ContainsItem(Item_SO itemToCheck, ref bool __result)
    {
        // Make game determine if we have a cassette based on if location checked rather than if item is present
        bool include_cassettes = Plugin.State.SlotData?.Options.include_cassettes ?? true;
        if(include_cassettes && PlayMusic_Patch.CheckingCassette && itemToCheck.category == Item_SO.ItemCategory.Cassette){
            bool found = Data.CassetteToApLocationId.TryGetValue(itemToCheck.jsonSaveKey, out var apLocation);
            if (found)
            {
                __result = Plugin.Client.IsLocationChecked((long)apLocation);
                return false;
            }
        }
        return true;
    }
}

[HarmonyPatch(typeof(GetItemScreen))]
internal class GetItemScreen_Patch
{
    [HarmonyPatch(nameof(GetItemScreen.CheckCloseMenu))]
    [HarmonyPostfix]
    public static void CheckCloseMenu()
    {
        bool include_items = Plugin.State.SlotData?.Options.include_items ?? true;
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
        bool include_basto = Plugin.State.SlotData?.Options.include_basto ?? true;
        bool include_items = Plugin.State.SlotData?.Options.include_items ?? true;
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
        bool include_items = Plugin.State.SlotData?.Options.include_items ?? true;
        bool found = Data.ItemToApLocationId.TryGetValue(__instance.item.jsonSaveKey, out var apLocation);
        if (!found || !include_items)
            return true;
        if (apLocation != ApLocationId.ItemAwardMask && apLocation != ApLocationId.ItemGhostGlasses &&
                apLocation != ApLocationId.ItemSandwich && apLocation != ApLocationId.ItemFrisbee &&
                apLocation != ApLocationId.ItemFootCast && 
                !(apLocation == ApLocationId.ItemBastoTicket && __instance.name == "Note From Grandma - Resort"))
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

    [HarmonyPatch(nameof(InventoryHasItem.ExecuteEvent), [typeof(Item_SO)])]
    [HarmonyPrefix]
    public static bool ExecuteEvent(InventoryHasItem __instance, Item_SO addedItem)
    {
        bool include_items = Plugin.State.SlotData?.Options.include_items ?? true;
        bool found = Data.ItemToApLocationId.TryGetValue(__instance.item.jsonSaveKey, out var apLocation);
        if (!found || !include_items || __instance.hasBeenTriggered || __instance.item != addedItem)
            return true;
        if (apLocation != ApLocationId.ItemBastoTicket)
            return true;

        Plugin.Logger.LogInfo($"InventoryHasItem.ExecuteEvent({addedItem.jsonSaveKey}) : {__instance.item.jsonSaveKey}");
        if (Plugin.Client.IsLocationChecked((long)apLocation) || PlayerInventory_Patch.BastoTicketFound)
        {
            if (!__instance.executeMoreThanOnce)
            {
                __instance.hasBeenTriggered = true;
                // I don't think this is correct, but it also doesn't seem to cause issues
                PlayerInventory.onItemAdded -= (Action<Item_SO>)__instance.ExecuteEvent;
                PlayerInventory.onItemRemoved -= (Action<Item_SO>)__instance.ExecuteEvent;
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

[HarmonyPatch(typeof(QuestDependentEvent))]
internal class QuestDependentEvent_Patch
{
    [HarmonyPatch(nameof(QuestDependentEvent.DoQuestEvent))]
    [HarmonyPrefix]
    public static bool DoQuestEvent(QuestDependentEvent __instance, bool isEnableEvent)
    {
        bool include_items = Plugin.State.SlotData?.Options.include_items ?? true;
        if (!include_items || __instance.questToCheck == null || __instance.questToCheck.currentStatus == Quest.QuestStatus.Undiscovered)
            return true;

        long apLocation = 0;
        if (__instance.name == "Frisbee Quest B")
            apLocation = (long)ApLocationId.ItemFrisbee;
        else if (__instance.name == "Supreme Sandwich Event")
            apLocation = (long)ApLocationId.ItemSandwich;

        if (apLocation == 0)
                return true;

        Plugin.Logger.LogInfo($"QuestDependentEvent.DoQuestEvent() : {__instance.name}");
        if (Plugin.Client.IsLocationChecked(apLocation))
        {
            if (!isEnableEvent || __instance.executeCompletedOnEnable)
                __instance.onQuestCompleted.Invoke();
        }
        else
        {
            if (!isEnableEvent || __instance.executeOngoingOnEnable)
                __instance.onQuestOngoing.Invoke();
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
        bool include_items = Plugin.State.SlotData?.Options.include_items ?? true;
        if (include_items){
        foreach (var item in __instance.itemsToCheckFor)
        {
            bool found = Data.ItemToApLocationId.TryGetValue(item.jsonSaveKey, out var apLocation);
            if (found && (apLocation == ApLocationId.ItemTripod || apLocation == ApLocationId.ItemFlag ||
                    apLocation == ApLocationId.ItemSkiGoggles || apLocation == ApLocationId.ItemScarf ||
                    apLocation == ApLocationId.ItemBastoTicket))
            {
                    Plugin.Logger.LogInfo($"CheckItemNode.EvaluateConditions() : {item.jsonSaveKey}");
                    __result = Plugin.Client.IsLocationChecked((long)apLocation);
                    return false;
                }
            }
        }

        bool include_cassettes = Plugin.State.SlotData?.Options.include_cassettes ?? true;
        if (include_cassettes){
            foreach (var item in __instance.itemsToCheckFor)
            {
                bool found = Data.CassetteToApLocationId.TryGetValue(item.jsonSaveKey, out var apLocation);
                if (found && (apLocation == ApLocationId.TapeSquirrelHotel))
                {
                    Plugin.Logger.LogInfo($"CheckItemNode.EvaluateConditions() : {item.jsonSaveKey}");
                __result = Plugin.Client.IsLocationChecked((long)apLocation);
                return false;
                }
            }
        }

        return true;
    }
}

[HarmonyPatch(typeof(TheEndScreen))]
internal class TheEndScreen_Patch
{
    [HarmonyPatch(nameof(TheEndScreen.OnMenuOpen))]
    [HarmonyPrefix]
    public static void OnMenuOpen()
    {
        Plugin.Logger.LogInfo($"TheEndScreen.OnMenuOpen(), ResortEnd: {TheEndScreen.triggerResortEnding}");
        bool include_basto = Plugin.State.SlotData?.Options.include_basto ?? true;
        if (include_basto == TheEndScreen.triggerResortEnding)
        {
            Plugin.Game.SendCompletion();
        }
    }
}

[HarmonyPatch(typeof(Achievements.BaseAchievement))]
internal class BaseAchievement_Patch
{
    [HarmonyPatch(nameof(Achievements.BaseAchievement.CompleteAchievement))]
    [HarmonyPrefix]
    public static void CompleteAchievement(Achievements.BaseAchievement __instance, bool previouslyCompleted)
    {
        Plugin.Logger.LogInfo($"Achievements.BaseAchievement.CompleteAchievement({previouslyCompleted}): {__instance.name}");
        bool include_achievements = Plugin.State.SlotData?.Options.include_achievements ?? true;
        bool include_basto = Plugin.State.SlotData?.Options.include_basto ?? true;
        bool found = Data.CheevoToApLocationId.TryGetValue(__instance.name, out var apLocation);
        if (previouslyCompleted || !found || !include_achievements || (!include_basto && apLocation >= ApLocationId.FirstBasto))
            return;

        Plugin.Game.CheckLocation(apLocation);
    }
}

[HarmonyPatch(typeof(Achievements.Achievement_CompleteQuestRegion))]
internal class Achievement_CompleteQuestRegion_Patch
{
    [HarmonyPatch(nameof(Achievements.Achievement_CompleteQuestRegion.Progress))]
    [HarmonyPrefix]
    public static bool Progress(Achievements.Achievement_CompleteQuestRegion __instance)
    {
        bool include_achievements = Plugin.State.SlotData?.Options.include_achievements ?? true;
        bool include_basto = Plugin.State.SlotData?.Options.include_basto ?? true;
        bool found = Data.CheevoToApLocationId.TryGetValue(__instance.name, out var apLocation);
        if (!found || !include_achievements || (!include_basto && apLocation >= ApLocationId.FirstBasto))
            return true;

        Plugin.Logger.LogInfo($"Achievements.Achievement_CompleteQuestRegion.Progress(): {__instance.name}");
        var regionQuests = GameManager.QuestDatabase.GetQuestList(__instance.region);
        foreach (var quest in regionQuests)
        {
            if (!quest.isSubQuest && !quest.isBackendQuest && quest.currentStatus != Quest.QuestStatus.Completed)
                return false;
        }
        __instance.CompleteAchievement(__instance.completed);
        return false;
    }
}

[HarmonyPatch(typeof(TitleScreenMenu))]
internal class TitleScreenMenu_Patch
{
    [HarmonyPatch(nameof(TitleScreenMenu.Update))]
    [HarmonyPrefix]
    public static bool Update(TitleScreenMenu __instance)
    {
        if (__instance.menuState != TitleScreenMenu.MenuState.HasFadedIn || __instance.hasSaveFile)
            return true;
        if (!OurInputManager.Instance.PlayerPressedActionButtonDown())
            return true;
        return !HUD.MouseOverHUD();
    }
}

[HarmonyPatch(typeof(PlayMusic))]
internal class PlayMusic_Patch
{
    static public bool CheckingCassette = false;

    [HarmonyPatch(nameof(PlayMusic.Start))]
    [HarmonyPrefix]
    public static void Start()
    {
        CheckingCassette = true;
    }

    [HarmonyPatch(nameof(PlayMusic.Start))]
    [HarmonyPostfix]
    public static void Start_Postfix()
    {
        CheckingCassette = false;
    }

    [HarmonyPatch(nameof(PlayMusic.TriggerTrack))]
    [HarmonyPrefix]
    public static void TriggerTrack()
    {
        CheckingCassette = true;
    }

    [HarmonyPatch(nameof(PlayMusic.TriggerTrack))]
    [HarmonyPostfix]
    public static void TriggerTrack_Postfix()
    {
        CheckingCassette = false;
    }
}

[HarmonyPatch(typeof(SceneTransitionController))]
internal class SceneTransitionController_Patch
{
    [HarmonyPatch(nameof(SceneTransitionController.DoSceneTransition))]
    [HarmonyPrefix]
    public static void DoSceneTransition(SceneReference sceneName, ref int transitionNodeIndex, ref LoadingIndicator.LoadingType loadingType)
    {
        Plugin.Logger.LogInfo($"SceneTransitionController.DoSceneTransition({sceneName.scenePath}, {transitionNodeIndex}, {loadingType})");
        int entrance_randomization = Plugin.State.SlotData?.Options.entrance_randomization ?? 0;
        if(Plugin.Game.IsCmdTp || loadingType != LoadingIndicator.LoadingType.Standard || entrance_randomization == 0)
            return;
        
        var match = Regex.Match(sceneName.scenePath, @"Assets/Scenes/([A-Za-z]*)/([A-Za-z0-9_]*)\.unity");
        string regionName = match.Groups[1].Value;
        string sceneShortName = match.Groups[2].Value;
        ApConnectionId sourceEntrance = Data.SceneTransitionToEntrance[sceneShortName][transitionNodeIndex];
        Plugin.Logger.LogInfo($"Corresponding source entrance: {sourceEntrance}");
        ApConnectionId targetEntrance = (ApConnectionId)Plugin.State.SlotData.Transitions[(int)sourceEntrance];
        Plugin.Logger.LogInfo($"Randomized target entrance: {targetEntrance}");
        var scenePair = Data.EntranceToSceneTransition[targetEntrance];
        string targetSceneName = scenePair.Item1;
        int newTransitionNodeIndex = scenePair.Item2;
        Plugin.Logger.LogInfo($"Randomized scene: {targetSceneName} ({newTransitionNodeIndex})");

        string sceneDirectory = "";
        foreach (var (prefix, directory) in Data.RegionSceneName)
        {
            if (targetSceneName.StartsWith(prefix))
            {
                sceneDirectory = directory;
                break;
            }
        }
        if(targetSceneName == "CosmoGarden")
            sceneDirectory = "Mountain";
        sceneName.scenePath = $"Assets/Scenes/{sceneDirectory}/{targetSceneName}.unity";
        transitionNodeIndex = newTransitionNodeIndex;
        Plugin.Client.TraverseEntrance((int)sourceEntrance);
    }
}

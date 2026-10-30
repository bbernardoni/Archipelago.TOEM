using HarmonyLib;
using Quests;
using Dialogue;
using System.Collections.Generic;

namespace Archipelago.TOEM;

[HarmonyPatch(typeof(PlayerInventory))]
internal class PlayerInventory_Patch
{
    // Prevent pirate hat switching from removing an item that might be needed for the Cosplayer achievement
    [HarmonyPrefix, HarmonyPatch(nameof(PlayerInventory.RemoveItem))]
    public static bool RemoveItem(Item_SO itemToRemove, int count)
    {
        if(itemToRemove.jsonSaveKey == "PirateHat" || itemToRemove.jsonSaveKey == "PaperHat")
            return false;
        return true;
    }
    
    // Make game determine if we have a cassette based on if location checked rather than if item is present
    [HarmonyPrefix, HarmonyPatch(nameof(PlayerInventory.ContainsItem))]
    public static bool ContainsItem(Item_SO itemToCheck, ref bool __result)
    {
        if(!PlayMusic_Patch.CheckingCassette || itemToCheck.category != Item_SO.ItemCategory.Cassette)
            return true;
        if(!Plugin.State.GetLocationIDIfRandomized(itemToCheck.jsonSaveKey, out var apLocation))
            return true;
        __result = Plugin.Client.IsLocationChecked((long)apLocation);
        return false;
    }
}

[HarmonyPatch(typeof(ChestController))]
internal class ChestController_Patch
{
    [HarmonyPrefix, HarmonyPatch(nameof(ChestController.Start))]
    public static bool Start(ChestController __instance)
    {
        if(!Plugin.State.GetLocationIDIfRandomized(__instance.itemInside.jsonSaveKey, out var apLocation))
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
    static public bool BastoTicketFound = false;

    [HarmonyPrefix, HarmonyPatch(nameof(InventoryHasItem.ExecuteEvent), [])]
    public static bool ExecuteEvent(InventoryHasItem __instance)
    {
        if(!Plugin.State.GetLocationIDIfRandomized(__instance.item.jsonSaveKey, out var apLocation))
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

    [HarmonyPrefix, HarmonyPatch(nameof(InventoryHasItem.ExecuteEvent), [typeof(Item_SO)])]
    public static bool ExecuteEvent(InventoryHasItem __instance, Item_SO addedItem)
    {
        if(!Plugin.State.GetLocationIDIfRandomized(__instance.item.jsonSaveKey, out var apLocation))
            return true;
        if (apLocation != ApLocationId.ItemBastoTicket)
            return true;

        Plugin.Logger.LogInfo($"InventoryHasItem.ExecuteEvent({addedItem.jsonSaveKey}) : {__instance.item.jsonSaveKey}");
        if (Plugin.Client.IsLocationChecked((long)apLocation) || BastoTicketFound)
        {
            if (!__instance.executeMoreThanOnce)
            {
                __instance.hasBeenTriggered = true;
                // Super jank, but the only way that I could get working
                List<Il2CppSystem.Delegate> list = [.. PlayerInventory.onItemAdded.delegates];
                foreach (var action in list)
                {
                    if(action.Target == (Il2CppSystem.Object)__instance)
                    {
                        list.Remove(action);
                        PlayerInventory.onItemAdded.delegates = list.ToArray();
                        break;
                    }
                }
                list = [.. PlayerInventory.onItemRemoved.delegates];
                foreach (var action in list)
                {
                    if(action.Target == (Il2CppSystem.Object)__instance)
                    {
                        list.Remove(action);
                        PlayerInventory.onItemRemoved.delegates = list.ToArray();
                        break;
                    }
                }
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
    [HarmonyPrefix, HarmonyPatch(nameof(QuestDependentEvent.DoQuestEvent))]
    public static bool DoQuestEvent(QuestDependentEvent __instance, bool isEnableEvent)
    {
        if (!Plugin.State.GetIfRandomized(ApLocationId.ItemFrisbee) || __instance.questToCheck == null || __instance.questToCheck.currentStatus == Quest.QuestStatus.Undiscovered)
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
    [HarmonyPrefix, HarmonyPatch(nameof(CheckItemNode.EvaluateConditions))]
    public static bool EvaluateConditions(CheckItemNode __instance, ref bool __result)
    {
        foreach (var item in __instance.itemsToCheckFor)
        {
            if(Plugin.State.GetLocationIDIfRandomized(item.jsonSaveKey, out var apLocation) &&
                    (apLocation == ApLocationId.ItemTripod || apLocation == ApLocationId.ItemFlag ||
                    apLocation == ApLocationId.ItemSkiGoggles || apLocation == ApLocationId.ItemScarf ||
                    apLocation == ApLocationId.ItemBastoTicket || apLocation == ApLocationId.TapeSquirrelHotel))
            {
                Plugin.Logger.LogInfo($"CheckItemNode.EvaluateConditions() : {item.jsonSaveKey}");
                __result = Plugin.Client.IsLocationChecked((long)apLocation);
                return false;
            }
        }
        return true;
    }
}

[HarmonyPatch(typeof(CheckQuestStatusNode))]
internal class CheckQuestStatusNode_Patch
{
    [HarmonyPrefix, HarmonyPatch(nameof(CheckQuestStatusNode.EvaluateConditions))]
    public static bool EvaluateConditions(CheckQuestStatusNode __instance, ref bool __result)
    {
        if (!Plugin.State.GetIfRandomized(ApLocationId.ItemWatergun))
            return true;

        var questToCheck = __instance.questToCheck;
        if (questToCheck.jsonSaveKey == "Kiosky Gate -Backend Quest")
        {
            Plugin.Logger.LogInfo($"CheckQuestStatusNode.EvaluateConditions() : {questToCheck.jsonSaveKey}");
            if(questToCheck.currentStatus != Quest.QuestStatus.Completed ||
                    Plugin.Client.IsLocationChecked((long)ApLocationId.ItemWatergun))
                return true;
            
            // Force Undiscovered to get water popper location check
            __instance.SelectNextNodeInGraph("onFail");
            __result = true;
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(SetQuestStatusNode))]
internal class SetQuestStatusNode_Patch
{
    [HarmonyPrefix, HarmonyPatch(nameof(SetQuestStatusNode.TriggerNode))]
    public static bool TriggerNode(SetQuestStatusNode __instance)
    {
        if (!Plugin.State.GetIfRandomized(ApLocationId.ItemWatergun))
            return true;

        var questToUpdate = __instance.questToUpdate;
        if (questToUpdate.jsonSaveKey == "Kiosky Gate -Backend Quest")
        {
            Plugin.Logger.LogInfo($"SetQuestStatusNode.TriggerNode() : {questToUpdate.jsonSaveKey}");
            if(questToUpdate.currentStatus != Quest.QuestStatus.Completed)
                return true;

            // Don't downgrade quest from Completed if we needed to force Water popper get item
            __instance.SelectNextNodeInGraph();
            DialogueBaseNode.TriggerCurrentNode();
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(SaveKeyDependentEvent))]
internal class SaveKeyDependentEvent_Patch
{
    [HarmonyPrefix, HarmonyPatch(nameof(SaveKeyDependentEvent.Start))]
    public static void Start(SaveKeyDependentEvent __instance)
    {
        if (!Plugin.State.GetIfRandomized(ApLocationId.ItemGhostGlasses))
            return;

        if (Plugin.Client.IsLocationChecked((long)ApLocationId.ItemGhostGlasses))
        {
            __instance.SetSaveKey();
        }
    }
}

[HarmonyPatch(typeof(PlayMusic))]
internal class PlayMusic_Patch
{
    static public bool CheckingCassette = false;

    [HarmonyPrefix, HarmonyPatch(nameof(PlayMusic.Start))]
    public static void Start()
    {
        CheckingCassette = true;
    }

    [HarmonyPostfix, HarmonyPatch(nameof(PlayMusic.Start))]
    public static void Start_Postfix()
    {
        CheckingCassette = false;
    }

    [HarmonyPrefix, HarmonyPatch(nameof(PlayMusic.TriggerTrack))]
    public static void TriggerTrack()
    {
        CheckingCassette = true;
    }

    [HarmonyPostfix, HarmonyPatch(nameof(PlayMusic.TriggerTrack))]
    public static void TriggerTrack_Postfix()
    {
        CheckingCassette = false;
    }
}

[HarmonyPatch(typeof(Achievements.Achievement_CompleteQuestRegion))]
internal class Achievement_CompleteQuestRegion_Patch
{
    [HarmonyPrefix, HarmonyPatch(nameof(Achievements.Achievement_CompleteQuestRegion.Progress))]
    public static bool Progress(Achievements.Achievement_CompleteQuestRegion __instance)
    {
        if (!Plugin.State.GetLocationIDIfRandomized(__instance.name, out _))
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

[HarmonyPatch(typeof(TargetPracticeController))]
internal class TargetPracticeController_Patch
{
    // Force gate open
    [HarmonyPrefix, HarmonyPatch(nameof(TargetPracticeController.Start))]
    public static void Start(TargetPracticeController __instance)
    {
        bool force_gate = Plugin.Options.force_basto_gate_open;
        if (force_gate && __instance.saveKey == "Target Practice - resortBusstop_0")
            __instance.SaveTargetPractice();
    }
}
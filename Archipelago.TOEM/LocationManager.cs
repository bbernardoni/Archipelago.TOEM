using HarmonyLib;
using Photographing;
using Quests;
using System;
using System.Collections.Generic;

namespace Archipelago.TOEM;

[HarmonyPatch]
public class LocationManager
{
    public List<long> OutgoingLocations { get; private set; } = [];
    public bool PendingCompletion { get; private set; } = false;
    
    public void Update()
    {
        if (Plugin.Client.Connected)
        {
            SyncLocations();
            if (PendingCompletion)
            {
                Plugin.Client.SendCompletion();
            }
        }
    }

    public void CheckLocation(ApLocationId location)
    {
        if (Plugin.Client.Connected)
        {
            Plugin.Client.SendLocation((long)location);
        }
        else
        {
            OutgoingLocations.Add((long)location);
        }
    }

    public void SendCompletion()
    {
        if (Plugin.Client.Connected)
        {
            Plugin.Client.SendCompletion();
        }
        else
        {
            PendingCompletion = true;
        }
    }

    public void SyncLocations()
    {
        if (OutgoingLocations.Count == 0)
            return;

        bool include_basto = Plugin.State.SlotData?.Options.include_basto ?? true;
        bool include_items = Plugin.State.SlotData?.Options.include_items ?? true;
        bool include_cassettes = Plugin.State.SlotData?.Options.include_cassettes ?? true;
        Predicate<long> filter = loc =>
            (!include_basto && loc >= (long)ApLocationId.FirstBasto) ||
            (!include_items && Data.ItemToApLocationId.ContainsValue((ApLocationId)loc)) ||
            (!include_cassettes && Data.CassetteToApLocationId.ContainsValue((ApLocationId)loc));

        foreach (var loc in OutgoingLocations)
        {
            if (filter(loc) && Data.ApLocationIdToApItemId.TryGetValue((ApLocationId)loc, out var apItem))
            {
                Plugin.Game.GiveItem(apItem);
            }
        }
        OutgoingLocations.RemoveAll(filter);

        Plugin.Client.SyncLocations(OutgoingLocations);
        OutgoingLocations.Clear();
    }

    [HarmonyPrefix, HarmonyPatch(typeof(CommunityController), nameof(CommunityController.GetStamp))]
    public static bool Patch_GetStamp(Quest completedQuest)
    {
        Plugin.Logger.LogInfo($"CommunityController.GetStamp({completedQuest})");
        bool include_basto = Plugin.State.SlotData?.Options.include_basto ?? true;
        bool found = Data.QuestToApLocationId.TryGetValue(completedQuest.jsonSaveKey, out var apLocation);
        if (!found || (!include_basto && apLocation >= ApLocationId.FirstBasto))
            return true;

        Plugin.LocationManager.CheckLocation(apLocation);

        // Delegate isn't setup until after this call. Delay removing until next Update prefix.
        GameManager_Patch.resetOnMenuRemoved = true;
        return false;
    }
    
    [HarmonyPrefix, HarmonyPatch(typeof(PlayerInventory), nameof(PlayerInventory.AddItem))]
    public static bool Patch_AddItem(Item_SO itemToAdd, int count, bool addedFromSaveFile)
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
                        Plugin.LocationManager.CheckLocation(ApLocationId.ItemIceCreamBanakin);
                    else if (quest.jsonSaveKey == "Fruit - Pear")
                        Plugin.LocationManager.CheckLocation(ApLocationId.ItemIceCreamMelonear);
                    else if (quest.jsonSaveKey == "Fruit - Bean")
                        Plugin.LocationManager.CheckLocation(ApLocationId.ItemIceCreamBeanut);
                    else if (quest.jsonSaveKey == "Fruit - Orange")
                        Plugin.LocationManager.CheckLocation(ApLocationId.ItemIceCreamOranganas);
                }
            }
        }
        else
        {
            Plugin.LocationManager.CheckLocation(apLocation);
            if(apLocation == ApLocationId.ItemBastoTicket)
            {
                InventoryHasItem_Patch.BastoTicketFound = true;
                PlayerInventory.onItemAdded.Invoke(itemToAdd);
            }
        }

        return false;
    }
    
    [HarmonyPrefix, HarmonyPatch(typeof(Achievements.BaseAchievement), nameof(Achievements.BaseAchievement.CompleteAchievement))]
    public static void Patch_CompleteAchievement(Achievements.BaseAchievement __instance, bool previouslyCompleted)
    {
        Plugin.Logger.LogInfo($"Achievements.BaseAchievement.CompleteAchievement({previouslyCompleted}): {__instance.name}");
        bool include_achievements = Plugin.State.SlotData?.Options.include_achievements ?? true;
        bool include_basto = Plugin.State.SlotData?.Options.include_basto ?? true;
        bool found = Data.CheevoToApLocationId.TryGetValue(__instance.name, out var apLocation);
        if (previouslyCompleted || !found || !include_achievements || (!include_basto && apLocation >= ApLocationId.FirstBasto))
            return;

        Plugin.LocationManager.CheckLocation(apLocation);
    }

    [HarmonyPrefix, HarmonyPatch(typeof(PhotoCompendium), nameof(PhotoCompendium.AddToCompendium))]
    public static void Patch_AddToCompendium(CompendiumPhotoTag tagToSave)
    {
        Plugin.Logger.LogInfo($"PhotoCompendium.AddToCompendium({tagToSave.creatureName})");
        bool include_basto = Plugin.State.SlotData?.Options.include_basto ?? true;
        bool found = Data.CreatureToApLocationId.TryGetValue(tagToSave.creatureName, out var apLocation);
        if (!found || (!include_basto && apLocation >= ApLocationId.FirstBasto))
            return;

        Plugin.LocationManager.CheckLocation(apLocation);
    }

    [HarmonyPrefix, HarmonyPatch(typeof(TheEndScreen), nameof(TheEndScreen.OnMenuOpen))]
    public static void Patch_OnMenuOpen()
    {
        Plugin.Logger.LogInfo($"TheEndScreen.OnMenuOpen(), ResortEnd: {TheEndScreen.triggerResortEnding}");
        bool include_basto = Plugin.State.SlotData?.Options.include_basto ?? true;
        if (include_basto == TheEndScreen.triggerResortEnding)
        {
            Plugin.LocationManager.SendCompletion();
        }
    }
}

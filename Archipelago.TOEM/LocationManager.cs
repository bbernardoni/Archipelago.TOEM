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

        Plugin.Client.SyncLocations(OutgoingLocations);
        OutgoingLocations.Clear();
    }

    [HarmonyPrefix, HarmonyPatch(typeof(CommunityController), nameof(CommunityController.GetStamp))]
    public static bool Patch_GetStamp(Quest completedQuest)
    {
        Plugin.Logger.LogInfo($"CommunityController.GetStamp({completedQuest})");
        if (!Plugin.State.GetLocationIDIfRandomized(completedQuest.jsonSaveKey, out var apLocation))
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
        if (!Plugin.State.GetLocationIDIfRandomized(itemToAdd.jsonSaveKey, out var apLocation))
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
        if (!Plugin.State.GetLocationIDIfRandomized(__instance.name, out var apLocation))
            return;

        Plugin.LocationManager.CheckLocation(apLocation);
    }

    [HarmonyPrefix, HarmonyPatch(typeof(PhotoCompendium), nameof(PhotoCompendium.AddToCompendium))]
    public static void Patch_AddToCompendium(CompendiumPhotoTag tagToSave)
    {
        Plugin.Logger.LogInfo($"PhotoCompendium.AddToCompendium({tagToSave.creatureName})");
        if (!Plugin.State.GetLocationIDIfRandomized(tagToSave.creatureName, out var apLocation))
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

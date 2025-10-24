using System;
using System.Collections.Generic;
using Quests;
using UnityEngine;

namespace Archipelago.TOEM;

public class Game
{
    public const string Name = "TOEM: A Photo Adventure";

    public Queue<ApItemInfo> IncomingItems { get; private set; } = new();
    //public Queue<ApItemInfo> IncomingMessages { get; private set; } = new();
    public List<long> OutgoingLocations { get; private set; } = new();
    public bool PendingCompletion { get; private set; } = false;
    public bool IsServerItem { get; set; } = false;
    public bool UnlockRegions { get; set; } = false;

    public void Update()
    {
        if (IncomingItems.TryDequeue(out var item))
        {
            if (item.Index < Plugin.State.ItemIndex)
            {
                Plugin.Logger.LogDebug($"Ignoring previously obtained item {item.Id}");
            }
            else
            {
                Plugin.State.ItemIndex++;
                GiveItem((ApItemId)item.Id);
            }
        }
        if (UnlockRegions && GameManager.instance.regionData.regionInfo.Count > 0)
        {
            foreach (var region in GameManager.instance.regionData.regionInfo)
            {
                Plugin.Logger.LogInfo($"Setting {region.Name} unlocked");
                region.isUnlocked = true;
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
            UnlockRegions = false;
        }
        if (Plugin.Client.Connected)
        {
            SyncLocations();
            if (PendingCompletion)
            {
                Plugin.Client.SendCompletion();
            }
        }
    }

    public void LoadSave() { }

    public void SetupNewSave()
    {
        Plugin.State.ClearSave();
    }

    public void ConnectSave()
    {
        UnlockRegions = true;
        SyncLocations();
    }

    private void SyncLocations()
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
                GiveItem(apItem);
            }
        }
        OutgoingLocations.RemoveAll(filter);

        Plugin.Client.SyncLocations(OutgoingLocations);
        OutgoingLocations.Clear();
    }

    private void GiveItem(ApItemId apItemId)
    {
        Plugin.Logger.LogDebug($"Got item {apItemId}");
        if (apItemId <= ApItemId.LastStamp)
        {
            GiveStamp(Data.ApItemIdToQuestRegion[apItemId]);
        }
        else if (apItemId <= ApItemId.LastPhoto)
        {
            // GivePhoto
        }
        else if (apItemId <= ApItemId.LastCassette)
        {
            GiveGameItem(Data.ApItemIdToItem[apItemId]);
        }
    }

    private Vector2 GetStampPosition(int index)
    {
        var communityController = CommunityController.instance;
        var clampArea = communityController.clampArea;
        float stampX = clampArea.x + (index % 5) * (clampArea.y - clampArea.x) / 4f;
        float stampY = clampArea.z + (index / 5 % 4) * (clampArea.w - clampArea.z) / 3f;
        return new(stampX, stampY);
    }

    private void GiveStamp(Quest.QuestRegion stampRegion)
    {
        var region = GameManager.instance.GetRegionData(stampRegion);
        int stampIndex = region.currentStampCount;
        Vector2 stampPos = GetStampPosition(stampIndex);
        region.currentStampCount += 1;
        region.stampPositions.Add(stampPos);

        var communityController = CommunityController.instance;
        bool isViewingRegion = stampRegion == communityController.currentViewedRegion;
        if (communityController.allStampsList.Count < region.currentStampCount)
        {
            var newStamp = UnityEngine.Object.Instantiate(communityController.stampPrefab, Vector3.zeroVector, Quaternion.identity, communityController.stampContainer);
            communityController.allStampsList.Add(newStamp);
        }
        if (isViewingRegion)
        {
            var newStamp = communityController.allStampsList[stampIndex];
            newStamp.transform.localPosition = stampPos;
            newStamp.Active();
            int countToRequired = Math.Min(region.requiredStamps, region.currentStampCount);
            communityController.regionRequiredStamps.Text = $"{countToRequired}/{region.requiredStamps}";
            communityController.allRegionStampCount.Text = $"{region.currentStampCount}/{region.totalQuestCount}";
        }
    }

    private void GiveGameItem(string itemName)
    {
        var item = GameManager.ItemDatabase.GetItemByName(itemName);
        IsServerItem = true;
        if (itemName == "MiniGame Ticket")
            GameManager.PlayerInventory.AddItem(item, 6);
        else
            GameManager.PlayerInventory.AddItem(item);
        IsServerItem = false;
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
}

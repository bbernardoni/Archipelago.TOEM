using System;
using System.Collections.Generic;
using Quests;
using UnityEngine;

namespace Archipelago.TOEM;

public class Game
{
    public const string Name = "TOEM: A Photo Adventure";

    public Queue<ApItemInfo> IncomingItems { get; private set; } = new();
    public Queue<ApItemInfo> IncomingMessages { get; private set; } = new();
    public string LastUpdatedQuest { get; set; }
    public bool IsServerItem { get; set; } = false;

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
                GiveItem(item);
            }
        }
    }

    public void LoadSave() { }

    public void SetupNewSave()
    {
        Plugin.State.ClearSave();
    }

    public void ConnectSave() { }

    private void GiveItem(ApItemInfo itemInfo)
    {
        var apItemId = (ApItemId)itemInfo.Id;
        Plugin.Logger.LogDebug($"Got item {apItemId}");
        if (apItemId <= ApItemId.StampBasto)
        {
            GiveStamp(Data.ApItemIdToQuestRegion[apItemId]);
        }
        else if (apItemId <= ApItemId.PhotoWaterStrider)
        {
            // GivePhoto
        }
        else if (apItemId <= ApItemId.Beret)
        {
            GiveGameItem(Data.ApItemIdToItem[apItemId]);
        }
    }

    private void GiveStamp(Quest.QuestRegion stampRegion)
    {
        var region = GameManager.instance.GetRegionData(stampRegion);
        region.currentStampCount += 1;
        region.stampPositions.Add(Vector2.zeroVector);

        var communityController = CommunityController.instance;
        bool isViewingRegion = stampRegion == communityController.currentViewedRegion;
        while (communityController.allStampsList.Count < region.currentStampCount)
        {
            var newStamp = UnityEngine.Object.Instantiate(communityController.stampPrefab, Vector3.zeroVector, Quaternion.identity, communityController.stampContainer);
            newStamp.transform.localPosition = Vector3.zeroVector;
            communityController.allStampsList.Add(newStamp);
            if (isViewingRegion)
                newStamp.Active();
        }
        if (isViewingRegion)
        {
            int countToRequired = Math.Min(region.requiredStamps, region.currentStampCount);
            communityController.regionRequiredStamps.Text = $"{countToRequired}/{region.requiredStamps}";
            communityController.allRegionStampCount.Text = $"{region.currentStampCount}/{region.totalQuestCount}";
        }
    }

    private void GiveGameItem(string itemName)
    {
        var item = GameManager.ItemDatabase.GetItemByName(itemName);
        IsServerItem = true;
        GameManager.PlayerInventory.AddItem(item);
    }
}

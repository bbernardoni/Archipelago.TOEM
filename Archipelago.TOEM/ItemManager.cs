using System;
using System.Collections.Generic;
using Photographing;
using Quests;
using UnityEngine;
using HarmonyLib;

namespace Archipelago.TOEM;

[HarmonyPatch]
public class ItemManager
{
    public bool IsServerItem { get; set; } = false;

    private const float SoundCooldown = 1f;
    private static float LastStampSound = -SoundCooldown;
    private static float LastPhotoSound = -SoundCooldown;
    private static float LastItemSound = -SoundCooldown;
    

    [HarmonyPrefix, HarmonyPatch(typeof(GameManager), nameof(GameManager.Update))]
    public static void Update_Prefix()
    {
        Plugin.ItemManager.Update();
    }

    public void Update()
    {
        if (Plugin.State.IncomingItems.TryDequeue(out var item))
        {
            if (item.Index < Plugin.State.ItemIndex /*&& !HasItem((ApItemId)item.Id)*/)
            {
                Plugin.Logger.LogDebug($"Ignoring previously obtained item {item.Id}");
            }
            else
            {
                if(item.Index >= Plugin.State.ItemIndex)
                    Plugin.State.ItemIndex++;
                GiveItem((ApItemId)item.Id);
            }
        }
    }

    public void GiveItem(ApItemId apItemId)
    {
        Plugin.Logger.LogDebug($"Got item {apItemId}");
        ApItemGroup itemGroup = Data.GetItemGroup(apItemId);
        if (itemGroup == ApItemGroup.Stamp)
        {
            GiveStamp(Data.ApItemIdToQuestRegion[apItemId]);
        }
        else if (itemGroup == ApItemGroup.Photo)
        {
            GivePhoto();
        }
        else if (itemGroup == ApItemGroup.Item || itemGroup == ApItemGroup.Cassette)
        {
            GiveGameItem(Data.ApItemIdToItem[apItemId]);
        }
    }

    private bool HasItem(ApItemId apItemId)
    {
        // TODO
        /*
        if (apItemId <= ApItemId.LastStamp)
        {
            //GiveStamp(Data.ApItemIdToQuestRegion[apItemId]);
        }
        else if (apItemId <= ApItemId.LastPhoto)
        {
            return true;
        }
        else if (apItemId <= ApItemId.LastCassette)
        {
            //return GameManager.PlayerInventory.ContainsItem(Data.ApItemIdToItem[apItemId]);
            //GiveGameItem(Data.ApItemIdToItem[apItemId]);
        }
        */
        return true;
    }

    private Vector2 GetStampPosition(int index)
    {
        var communityController = CommunityController.instance;
        var clampArea = communityController.clampArea;
        float stampX = clampArea.x + (index % 5) * (clampArea.y - clampArea.x) / 4f;
        float stampY = clampArea.z + (index / 5 % 4) * (clampArea.w - clampArea.z) / 3f;
        return new(stampX, stampY);
    }

    public void GiveStamp(Quest.QuestRegion stampRegion)
    {
        RegionData.RegionInfo region;
        // Find region if stamp is progressive
        if(stampRegion == Quest.QuestRegion.Generic)
        {
            List<Quest.QuestRegion> questRegions = [
                Quest.QuestRegion.Home, Quest.QuestRegion.Forest, Quest.QuestRegion.Harbor,
                Quest.QuestRegion.City, Quest.QuestRegion.Mountain,
                ];
            if(Plugin.Options.include_basto)
                questRegions.Add(Quest.QuestRegion.Resort);
            foreach(Quest.QuestRegion questRegion in questRegions)
            {
                region = GameManager.instance.GetRegionData(questRegion);
                if(region.currentStampCount < region.requiredStamps)
                {
                    stampRegion = questRegion;
                    break;
                }
            }
            if(stampRegion == Quest.QuestRegion.Generic)
            {
                foreach(Quest.QuestRegion questRegion in questRegions)
                {
                    region = GameManager.instance.GetRegionData(questRegion);
                    if(region.currentStampCount < region.totalQuestCount)
                    {
                        stampRegion = questRegion;
                        break;
                    }
                }
                if(stampRegion == Quest.QuestRegion.Generic)
                    stampRegion = Quest.QuestRegion.Resort;
            }
        }

        region = GameManager.instance.GetRegionData(stampRegion);
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

        if(Time.time - LastStampSound > SoundCooldown)
        {
            communityController.gotStampJingleSound.PlaySound();
            communityController.stampAppearSound.PlaySound();
            communityController.stampCardSound.PlaySound();
            LastStampSound = Time.time;
        }
    }

    public void GivePhoto()
    {
        // Photos are filler items any way so we don't do anything
        if(Time.time - LastPhotoSound > SoundCooldown)
        {
            CompendiumConfirmMenu.instance.newCompendiumItemSound.PlaySound();
            LastPhotoSound = Time.time;
        }
    }

    public void GiveGameItem(string itemName)
    {
        var item = GameManager.ItemDatabase.GetItemByName(itemName);
        IsServerItem = true;
        if (itemName == "MiniGame Ticket")
            GameManager.PlayerInventory.AddItem(item, 6);
        else
            GameManager.PlayerInventory.AddItem(item);
        IsServerItem = false;
        if(Time.time - LastItemSound > SoundCooldown)
        {
            GetItemScreen.instance.getItemJingleSound.PlaySound();
            LastItemSound = Time.time;
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(GetItemScreen), nameof(GetItemScreen.SetupScreen))]
    public static bool Patch_SetupGetItemScreen(GetItemScreen __instance, Item_SO pickedUpItem)
    {
        if (!Plugin.State.GetLocationIDIfRandomized(pickedUpItem.jsonSaveKey, out var apLocation))
            return true;
        
        var itemInfo = Plugin.State.ScoutedLocations[(long)apLocation];
        string color = "00EEEE";
        if ((itemInfo.Flags & MultiClient.Net.Enums.ItemFlags.Trap) != 0)
            color = "FA8072";
        else if ((itemInfo.Flags & MultiClient.Net.Enums.ItemFlags.Advancement) != 0)
        {
            if ((itemInfo.Flags & MultiClient.Net.Enums.ItemFlags.NeverExclude) != 0)
                color = "rainbow";
            else
                color = "AF99EF";
        }
        else if ((itemInfo.Flags & MultiClient.Net.Enums.ItemFlags.NeverExclude) != 0)
            color = "6D8BE8";

        __instance.itemNameText.text = $"<w=sassy><c={color}>{itemInfo.Name}";
        string foundFor = itemInfo.IsLocal? "Your Item": $"Item for {itemInfo.PlayerName}";
        __instance.itemDescriptionText.text = $"<w=sassy>Found {foundFor} at {pickedUpItem.ToString()}";
        __instance.itemCountText.enabled = false;
        
        if (itemInfo.IsToem && Data.ApItemIdToItem.TryGetValue((ApItemId)itemInfo.Id, out var jsonSaveKey))
        {
            var itemObj = GameManager.ItemDatabase.GetItemByName(jsonSaveKey);
            __instance.itemSprite.sprite = itemObj.sprite;
            var equipableItemObj = itemObj.TryCast<EquipableItem>();
            if (equipableItemObj)
            {
                if (itemInfo.IsLocal)
                    __instance.equipableItemThatWasGivenToPlayer = equipableItemObj;
                foreach (var category in __instance.equipmentCategories)
                {
                    if (category.category == equipableItemObj.equipmentCategory)
                    {
                        __instance.itemCategoryImage.sprite = category.categorySprite;
                        break;
                    }
                }
            }
            else
            {
                foreach (var category in __instance.itemCategories)
                {
                    if (category.category == itemObj.category)
                    {
                        __instance.itemCategoryImage.sprite = category.categorySprite;
                        break;
                    }
                }
            }
        }
        else
        {
            var tex = new Texture2D(256, 256, TextureFormat.RGBA32, false);
            ImageConversion.LoadImage(tex, Plugin.AssetManager.ApLogoData);
            var ApLogo = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero);
            __instance.itemSprite.sprite = ApLogo;
            __instance.itemCategoryImage.sprite = null;
        }
        __instance.itemCategoryImage.enabled = __instance.itemCategoryImage.sprite != null;

        return false;
    }
}

using System;
using System.Collections.Generic;
using Photographing;
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
    public bool SetStampRequirements { get; set; } = false;
    public bool IsCmdTp { get; set; } = false;

    private const float SoundCooldown = 1f;
    private static float LastStampSound = -SoundCooldown;
    private static float LastPhotoSound = -SoundCooldown;
    private static float LastItemSound = -SoundCooldown;

    public void Update()
    {
        if (SetStampRequirements)
        {
            foreach (var region in GameManager.instance.regionData.regionInfo)
            {
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
            SetStampRequirements = false;
        }
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
        UnlockRegions();
        if(Plugin.Client.Connected)
        {
            SetStampRequirements = true;
            Plugin.Client.ResendItems();
        }
    }

    public void ConnectSave()
    {
        // casting doesn't work for some reason, so the workaround is a bit hacky
        TitleScreenMenu titleScreenMenu = MenuManager.Instance.menu_TitleScreen.gameObject.GetComponent<TitleScreenMenu>();
        if (titleScreenMenu.gameObject.active && Plugin.Client.GetNumLocationChecked()==0 && SaveManager.SaveExists())
        {
            //Bring up reset save menu
            titleScreenMenu.SetNewGamePromptState(true);
            if (Plugin.State.SlotData.Options.include_achievements)
                titleScreenMenu.keepAchievementsOption.ToggleOn(); // "On" means reset achievements
        }
        SetStampRequirements = true;
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
            GivePhoto();
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
        RegionData.RegionInfo region;
        // Find region if stamp is progressive
        if(stampRegion == Quest.QuestRegion.Generic)
        {
            List<Quest.QuestRegion> questRegions = [
                Quest.QuestRegion.Home, Quest.QuestRegion.Forest, Quest.QuestRegion.Harbor,
                Quest.QuestRegion.City, Quest.QuestRegion.Mountain,
                ];
            bool include_basto = Plugin.State.SlotData?.Options.include_basto ?? true;
            if(include_basto)
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

    private void GivePhoto()
    {
        // Photos are filler items any way so we don't do anything
        if(Time.time - LastPhotoSound > SoundCooldown)
        {
            CompendiumConfirmMenu.instance.newCompendiumItemSound.PlaySound();
            LastPhotoSound = Time.time;
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
        if(Time.time - LastItemSound > SoundCooldown)
        {
            GetItemScreen.instance.getItemJingleSound.PlaySound();
            LastItemSound = Time.time;
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

    public void UnlockRegions()
    {
        foreach (var region in GameManager.instance.regionData.regionInfo)
        {
            Plugin.Logger.LogInfo($"Setting {region.Name} unlocked");
            region.isUnlocked = true;
        }
    }

    public void ExecuteCommand(string command_str)
    {
        string[] command = command_str.Split(" ");
        if(command.Length == 0)
        {
            Client.ClientConsole.LogMessage("No command found");
        }
        else if(command[0] == "help")
        {
            Client.ClientConsole.LogMessage("TODO add help output");
        }
        else if(command[0] == "tp")
        {
            if(command.Length != 3)
                Client.ClientConsole.LogMessage("tp command takes three arguements");
            else
                TpCommand(command[1], int.Parse(command[2]));
        }
        else
        {
            Client.ClientConsole.LogMessage("Unknown command");
        }
    }

    public void TpCommand(string sceneName, int transitionNodeIndex)
    {
        Client.ClientConsole.LogMessage($"Teleporting to scene {sceneName} ({transitionNodeIndex})");
        string sceneDirectory = "";
        foreach (var (prefix, directory) in Data.RegionSceneName)
        {
            if (sceneName.StartsWith(prefix))
            {
                sceneDirectory = directory;
                break;
            }
        }
        if(sceneName == "CosmoGarden")
            sceneDirectory = "Mountain";
        string scenePath = $"Assets/Scenes/{sceneDirectory}/{sceneName}.unity";
        SceneReference sceneRef = new()
        {
            scenePath = scenePath
        };
        IsCmdTp = true;
        SceneTransitionController.Instance.DoSceneTransition(sceneRef, transitionNodeIndex, LoadingIndicator.LoadingType.Standard);
        IsCmdTp = false;
    }
}

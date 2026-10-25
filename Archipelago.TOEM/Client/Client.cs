using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.Models;

namespace Archipelago.TOEM.Client;

public class Client
{
    private const string MinArchipelagoVersion = "0.6.0";

    public bool Connected => _session?.Socket.Connected ?? false;
    private bool _attemptingConnection;
    private bool _shouldStartGame = false;

    private ArchipelagoSession _session;
    private bool _ignoreLocations;

    public void Connect(bool shouldStartGame = false)
    {
        if (Connected || _attemptingConnection)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(Plugin.State.Uri) || string.IsNullOrWhiteSpace(Plugin.State.SlotName))
        {
            Plugin.Logger.LogWarning("Connect aborted. Hostname or Player name is empty.");
            return;
        }

        try
        {
            _session = ArchipelagoSessionFactory.CreateSession(Plugin.State.Uri);
            SetupSession();
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError(e);
        }

        _shouldStartGame = shouldStartGame;
        TryConnect();
    }

    private void SetupSession()
    {
        _session.Socket.ErrorReceived += Session_ErrorReceived;
        _session.Socket.SocketClosed += Session_SocketClosed;
        _session.Items.ItemReceived += Session_ItemReceived;
        _session.Locations.CheckedLocationsUpdated += Session_CheckedLocationsUpdated;
        _session.MessageLog.OnMessageReceived += Session_OnMessageReceived;
    }

    private void TryConnect()
    {
        LoginResult loginResult;
        _attemptingConnection = true;
        _ignoreLocations = true;

        try
        {
            loginResult = _session.TryConnectAndLogin(
                Game.Name,
                Plugin.State.SlotName,
                ItemsHandlingFlags.AllItems,
                new(MinArchipelagoVersion),
                password: Plugin.State.Password,
                requestSlotData: false);
        }
        catch (Exception e)
        {
            loginResult = new LoginFailure(e.GetBaseException().Message);
        }

        if (loginResult is LoginFailure loginFailure)
        {
            _attemptingConnection = false;
            string failureMessage = "AP connection failed: " + string.Join("\n", loginFailure.Errors);
            Plugin.Logger.LogError(failureMessage);
            ClientConsole.LogMessage(failureMessage);
            _session = null;
            return;
        }

        string connectMessage = $"Successfully connected to {Plugin.State.Uri} as {Plugin.State.SlotName}";
        Plugin.Logger.LogInfo(connectMessage);
        ClientConsole.LogMessage(connectMessage);
        OnConnect();
    }

    private static bool VersionLessThan(string versionString1, string versionString2)
    {
        if (Version.TryParse(versionString1, out Version version1) && 
            Version.TryParse(versionString2, out Version version2))
        {
            return version1 < version2;
        }
        return true;
    }

    private void OnConnect()
    {
        var slotData = _session.DataStorage.GetSlotData<SlotData>();
        slotData.Version ??= "0.0.0";
        slotData.Minimum_version ??= slotData.Version[..slotData.Version.LastIndexOf(".")] + ".0";

        string invalidVersionMessage = "";
        if (VersionLessThan(slotData.Version, Plugin.MinimumVersion))
            invalidVersionMessage = $"Incompatible client version. The APWorld version ({slotData.Version}) used during generation is older than the minimum supported client version ({Plugin.MinimumVersion}). Please install client version {slotData.Version}.";
        if (VersionLessThan(MyPluginInfo.PLUGIN_VERSION, slotData.Minimum_version))
            invalidVersionMessage = $"Incompatible client version. Client version ({MyPluginInfo.PLUGIN_VERSION}) is older than the minimum supported version ({slotData.Minimum_version}) of the APWorld used during generation. Please install client version {slotData.Version}.";
        if(invalidVersionMessage != "")
        {
            Plugin.Logger.LogError(invalidVersionMessage);
            ClientConsole.LogMessage(invalidVersionMessage);
            Disconnect();
            return;
        }

        _session.DataStorage[GetDSKey("traversed_entrances")].Initialize(new List<int>());
        Plugin.State.SetupSession(slotData, _session.RoomState.Seed);
        Plugin.SaveFileManager.ConnectSave(_shouldStartGame);
        Plugin.UpdateConnectionInfo(Plugin.State.Uri, Plugin.State.SlotName, Plugin.State.Password);
        _ignoreLocations = false;
        _attemptingConnection = false;
    }

    public void Disconnect()
    {
        if (!Connected)
        {
            return;
        }

        string disconnectMessage = "Disconnecting from server.";
        Plugin.Logger.LogInfo(disconnectMessage);
        ClientConsole.LogMessage(disconnectMessage);

        _attemptingConnection = false;
        Task.Run(() => { _session.Socket.DisconnectAsync(); }).Wait();
        _session = null;
    }

    public void Session_SocketClosed(string reason)
    {
        string disconnectMessage = "Connection to Archipelago lost";
        if (reason != "")
            disconnectMessage += ": " + reason;
        Plugin.Logger.LogInfo(disconnectMessage);
        ClientConsole.LogMessage(disconnectMessage);
        _attemptingConnection = false;
        _session = null;
    }

    public void Session_ErrorReceived(Exception e, string message)
    {
        Plugin.Logger.LogError(message);
        if (e != null)
        {
            Plugin.Logger.LogError(e.ToString());
        }
    }

    public static void Session_OnMessageReceived(LogMessage message)
    {
        Plugin.Logger.LogMessage(message);
        ClientConsole.LogMessage(message.ToString());
    }

    public void SendLocation(long location)
    {
        if (!Connected)
        {
            Plugin.Logger.LogWarning($"Trying to send location {location} when there's no connection");
            return;
        }

        _session.Locations.CompleteLocationChecksAsync(location);
    }

    public bool IsLocationChecked(long location)
    {
        if (!Connected)
        {
            return false;
        }

        return _session.Locations.AllLocationsChecked.Contains(location);
    }

    public int GetNumLocationChecked()
    {
        if (!Connected)
        {
            return -1;
        }

        return _session.Locations.AllLocationsChecked.Count;
    }

    public bool SyncLocations(List<long> locations)
    {
        if (!Connected || locations == null || locations.Count == 0)
        {
            return false;
        }

        Plugin.Logger.LogInfo($"Sending location checks: {string.Join(", ", locations)}");
        _session.Locations.CompleteLocationChecksAsync(locations.ToArray());
        return true;
    }

    public Dictionary<long, ApItemInfo> ScoutAllLocations()
    {
        Dictionary<long, ApItemInfo> scoutData = Plugin.SaveFileManager.GetScoutData();
        if (scoutData != null)
            return scoutData;

        if (!Connected)
            return null;

        Plugin.Logger.LogInfo("Fetching scout data");
        List<long> locations = [.. _session.Locations.AllLocations];
        var scouts = _session.Locations.ScoutLocationsAsync(locations.ToArray()).ContinueWith(task =>
        {
            Dictionary<long, ApItemInfo> itemInfos = [];
            foreach (var entry in task.Result)
            {
                var itemName = entry.Value.ItemDisplayName;
                var isToem = entry.Value.ItemGame == Game.Name;
                var player = entry.Value.Player;
                var playerName = player.Alias ?? player.Name ?? $"Player #{player.Slot}";

                itemInfos[entry.Key] = new ApItemInfo
                {
                    Id = entry.Value.ItemId,
                    Name = itemName,
                    Flags = entry.Value.Flags,
                    Player = player,
                    PlayerName = playerName,
                    IsLocal = player == GetCurrentPlayer(),
                    LocationId = entry.Key,
                    IsToem = isToem,
                };
            }
            return itemInfos;
        });
        scouts.Wait();
        Plugin.SaveFileManager.SaveScoutData(scouts.Result);
        return scouts.Result;
    }

    public void SendCompletion()
    {
        if (!Connected)
        {
            return;
        }

        _session.SetGoalAchieved();
    }

    public void Session_ItemReceived(IReceivedItemsHelper helper)
    {
        var index = helper.Index;
        var item = helper.DequeueItem();
        EnqueueItem(item, index);
    }

    public void ResendItems()
    {
        Plugin.Game.IncomingItems.Clear();
        int index = 0;
        foreach(var item in _session.Items.AllItemsReceived)
        {
            index++;
            EnqueueItem(item, index);
        }
    }

    public void EnqueueItem(ItemInfo item, int index)
    {
        var itemName = item.ItemDisplayName;
        Plugin.Logger.LogInfo($"Received item #{index}: {item.ItemId} - {itemName}");
        var player = item.Player;
        var playerName = player.Alias ?? player.Name ?? $"Player #{player.Slot}";

        if (Plugin.Game.IncomingItems.All(incoming => incoming.Index != index))
        {
            Plugin.Game.IncomingItems.Enqueue(new()
            {
                Id = item.ItemId,
                Name = itemName,
                Flags = item.Flags,
                Player = player,
                PlayerName = playerName,
                IsLocal = player == GetCurrentPlayer(),
                LocationId = item.LocationId,
                Receiving = true,
                Index = index,
                IsToem = true,
            });
        }
    }

    public void Session_CheckedLocationsUpdated(ReadOnlyCollection<long> newCheckedLocations)
    {
        if (_ignoreLocations)
        {
            return;
        }

        Plugin.Logger.LogDebug($"New locations checked: {string.Join(", ", newCheckedLocations)}");
        /*
        // Not sure what this was intended for, maybe shortcutting awarding local items?
        // Commented out since it's not functional or necessary
        foreach (var id in newCheckedLocations)
        {
            if (Plugin.State.LocationInfos.TryGetValue(id, out var itemInfo))
            {
                Plugin.Logger.LogInfo($"Checked location: {id} - {itemInfo.Name} for {itemInfo.PlayerName}");
                if (!itemInfo.IsLocal)
                {
                    Plugin.Game.IncomingMessages.Enqueue(itemInfo);
                }
            }
            else
            {
                Plugin.Logger.LogWarning($"Scouting failed for location {id}");
                continue;
            }

        }
        */
    }

    public int GetCurrentPlayer()
    {
        if (!Connected)
        {
            return -1;
        }

        return _session.ConnectionInfo.Slot;
    }

    public int GetCurrentTeam()
    {
        if (!Connected)
        {
            return -1;
        }

        return _session.ConnectionInfo.Team;
    }

    public string GetSeedName()
    {
        if (!Connected)
        {
            return "";
        }

        return _session.RoomState.Seed;
    }

    public string GetDSKey(string key)
    {
        return $"toem_{GetCurrentPlayer()}_{GetCurrentTeam()}_{key}";
    }

    public void SendMessage(string message)
    {
        _session?.Say(message);
    }

    public void TraverseEntrance(int entrance)
    {
        if (!Connected)
            return;

        string key = GetDSKey("traversed_entrances");
        _session.DataStorage[key].GetAsync<List<int>>().ContinueWith(task => {
            var traversedEntrances = task.Result;
            if(!traversedEntrances.Contains(entrance))
            {
                traversedEntrances.Add(entrance);
                _session.DataStorage[key] = traversedEntrances;
            }
        });
    }

    public void SceneLoaded(int scene)
    {
        if (!Connected)
            return;

        _session.DataStorage[GetDSKey("scene")] = scene;
    }
}

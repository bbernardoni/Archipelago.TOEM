using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

    private ArchipelagoSession _session;
    private bool _ignoreLocations;

    public void Connect()
    {
        if (Connected || _attemptingConnection)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(Plugin.State.Uri) || string.IsNullOrWhiteSpace(Plugin.State.SlotName))
        {
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
            Plugin.Logger.LogError("AP connection failed: " + string.Join("\n", loginFailure.Errors));
            _session = null;
            return;
        }

        Plugin.Logger.LogInfo($"Successfully connected to {Plugin.State.Uri} as {Plugin.State.SlotName}");
        OnConnect();
    }

    private void OnConnect()
    {
        var slotData = _session.DataStorage.GetSlotData<SlotData>();
        Plugin.State.SetupSession(slotData, _session.RoomState.Seed);
        Plugin.Game.ConnectSave();
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

        _attemptingConnection = false;
        Task.Run(() => { _session.Socket.DisconnectAsync(); }).Wait();
        _session = null;
    }

    public void Session_SocketClosed(string reason)
    {
        Plugin.Logger.LogError("Connection to Archipelago lost: " + reason);
        Disconnect();
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
        if (!Connected)
        {
            return null;
        }

        List<long> locations = new(_session.Locations.AllLocations);
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

    public void SendMessage(string message)
    {
        _session?.Say(message);
    }

    public void TraverseEntrance(string entrance)
    {
        List<string> traversedEntrances = _session.DataStorage[Scope.Slot, "TraversedEntrances"];
        if(!traversedEntrances.Contains(entrance))
        {
            traversedEntrances.Add(entrance);
            _session.DataStorage[Scope.Slot, "TraversedEntrances"] = traversedEntrances;
        }
    }
}

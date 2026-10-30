using System.Collections.Generic;
using Archipelago.MultiClient.Net.Enums;

namespace Archipelago.TOEM;

public class ApItemInfo
{
    public long Id { get; set; }
    public string Name { get; set; }
    public ItemFlags Flags { get; set; }
    public int Player { get; set; }
    public string PlayerName { get; set; }
    public bool IsLocal { get; set; }
    public long LocationId { get; set; }
    public bool Receiving { get; set; }
    public int Index { get; set; }
    public bool IsToem { get; set; }
}

public class SlotOptions
{
    public bool include_basto { get; set; }
    public int entrance_randomization { get; set; }
    public bool include_items { get; set; }
    public bool include_cassettes { get; set; }
    public bool include_achievements { get; set; }
    public bool honk_attachment_early { get; set; }
    public bool force_basto_gate_open { get; set; }
    public bool progressive_stamps { get; set; }
    public int homelanda_stamp_requirement { get; set; }
    public int oaklaville_stamp_requirement { get; set; }
    public int stanhamn_stamp_requirement { get; set; }
    public int logcity_stamp_requirement { get; set; }
    public int kiiruberg_stamp_requirement { get; set; }
    public int basto_stamp_requirement { get; set; }
}

public class SlotData
{
    public string Version { get; set; }
    public string Minimum_version { get; set; }
    public SlotOptions Options { get; set; }
    public Dictionary<int, int> Transitions { get; set; }
}

public class State
{
    public string Uri;
    public string SlotName;
    public string Password;
    public string Seed;
    public SlotData SlotData;
    
    public Queue<ApItemInfo> IncomingItems { get; set; } = new();
    public Dictionary<long, ApItemInfo> ScoutedLocations { get; set; } = [];
    public List<long> OutgoingLocations { get; set; } = [];
    public bool PendingCompletion { get; set; } = false;
    public int ItemIndex { get; set; } = 1;

    public State()
    {
        Uri = "localhost";
        SlotName = "Player1";
    }

    public State(string uri, string slotName, string password)
    {
        Uri = uri;
        SlotName = slotName;
        Password = password;
    }

    public void UpdateConnection(string uri, string slotName, string password)
    {
        Uri = uri;
        SlotName = slotName;
        Password = password;
    }

    public void SetupSession(SlotData slotData, string roomSeed)
    {
        SlotData = slotData;
        Seed = roomSeed;
    }

    public bool GetLocationIDIfRandomized(string name, out ApLocationId apLocationId)
    {
        if(!Data.LocationToApLocationId.TryGetValue(name, out apLocationId))
            return false;
        return GetIfRandomized(apLocationId);
    }

    public bool GetIfRandomized(ApLocationId apLocationId)
    {
        if (!SlotData.Options.include_basto && apLocationId >= ApLocationId.FirstBasto)
            return false;

        ApLocationGroup locationGroup = Data.GetLocationGroup(apLocationId);
        if (locationGroup == ApLocationGroup.Item)
            return SlotData.Options.include_items;
        if (locationGroup == ApLocationGroup.Cassette)
            return SlotData.Options.include_cassettes;
        if (locationGroup == ApLocationGroup.Achievement)
            return SlotData.Options.include_achievements;
        return true;
    }

    public void ClearConnection()
    {
        Seed = "";
        SlotData = null;
        //LocationInfos.Clear();
        //CheckedLocations.Clear();
    }

    public void ClearSave()
    {
        ItemIndex = 1;
    }
}

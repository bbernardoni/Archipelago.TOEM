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
    public bool include_items { get; set; }
    public bool include_cassettes { get; set; }
    public bool include_achievements { get; set; }
    public int homelanda_stamp_requirement { get; set; }
    public int oaklaville_stamp_requirement { get; set; }
    public int stanhamn_stamp_requirement { get; set; }
    public int logcity_stamp_requirement { get; set; }
    public int kiiruberg_stamp_requirement { get; set; }
    public int basto_stamp_requirement { get; set; }
    public bool honk_attachment_early { get; set; }
}

public class SlotData
{
    public string Version { get; set; }
    public SlotOptions Options { get; set; }
}

public class State
{
    public string Uri;
    public string SlotName;
    public string Password;
    public string Seed;
    public SlotData SlotData;
    // Not sure what these were intended for
    //public Dictionary<long, ApItemInfo> LocationInfos = [];
    //public List<long> CheckedLocations { get; set; } = [];
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

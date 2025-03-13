using System.Collections.Generic;
using Archipelago.MultiClient.Net.Enums;
using Newtonsoft.Json;

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
    public bool IncludeAchievements { get; set; }
    public bool IncludeBasto { get; set; }
}

public class SlotData
{
    [JsonProperty("version")]
    public string Version { get; set; }

    [JsonProperty("options")]
    public SlotOptions Options { get; set; }
}

public class State
{
    public bool Valid;

    public string Uri;
    public string SlotName;
    public string Password;
    public string Seed;
    public SlotData SlotData;
    public Dictionary<long, ApItemInfo> LocationInfos = [];
    public List<long> CheckedLocations { get; set; } = [];
    public int ItemIndex { get; set; }
    public int HomeStamps { get; set; }
    public int OakStamps { get; set; }
    public int DockStamps { get; set; }
    public int CityStamps { get; set; }
    public int SnowStamps { get; set; }
    public int BastoStamps { get; set; }

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
        Valid = false;

        Seed = "";
        SlotData = null;
        LocationInfos.Clear();
        CheckedLocations.Clear();
    }

    public void ClearSave()
    {
        Valid = false;
        ItemIndex = 0;
        HomeStamps = 0;
        OakStamps = 0;
        DockStamps = 0;
        CityStamps = 0;
        SnowStamps = 0;
        BastoStamps = 0;
    }
}

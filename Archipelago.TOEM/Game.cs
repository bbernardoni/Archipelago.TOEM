using System.Collections.Generic;
using Quests;

namespace Archipelago.TOEM;

public class Game
{
    public const string Name = "TOEM";

    public Queue<ApItemInfo> IncomingItems { get; private set; } = new();
    public Queue<ApItemInfo> IncomingMessages { get; private set; } = new();
    public string LastUpdatedQuest { get; set; }

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
        switch (apItemId)
        {
            case ApItemId.StampHomelanda:
                // var region = GameManager.instance.GetRegionData(Quest.QuestRegion.Home);
                // region.currentStampCount += 1;
                break;
        }
    }
}

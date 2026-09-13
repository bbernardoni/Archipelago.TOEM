using HarmonyLib;

namespace Archipelago.TOEM;

[HarmonyPatch(typeof(GameManager))]
internal class GameManager_Patch
{
    static public bool resetOnMenuRemoved = false;

    [HarmonyPrefix, HarmonyPatch(nameof(GameManager.Update))]
    public static void Update()
    {
        Plugin.Game?.Update();
        Plugin.LocationManager?.Update();
        if (resetOnMenuRemoved && CommunityController.instance.onMenuRemovedFromStack != null)
        {
            CommunityController.instance.onMenuRemovedFromStack.Invoke();
            CommunityController.instance.onMenuRemovedFromStack = null;
            OurInputManager.playerHasControl = true;
            resetOnMenuRemoved = false;
        }
    }
}
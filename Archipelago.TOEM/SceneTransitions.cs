using HarmonyLib;
using System.Text.RegularExpressions;

namespace Archipelago.TOEM;

public class SceneManager
{
    // Is transistion comming from TP command
    public bool IsCmdTp { get; set; } = false;

    static bool SkiliftResetState { get; set; } = false;

    // Get full scenePath from sceneName
    public static string GetScenePath(string sceneName)
    {
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
        return $"Assets/Scenes/{sceneDirectory}/{sceneName}.unity";
    }

    // Tp to entrance connected to the give exit
    public void TpER(string scenePath, int transitionNodeIndex)
    {
        var match = Regex.Match(scenePath, @"Assets/Scenes/([A-Za-z]*)/([A-Za-z0-9_]*)\.unity");
        //string regionName = match.Groups[1].Value;
        string sceneShortName = match.Groups[2].Value;
        ApConnectionId sourceEntrance = Data.SceneTransitionToEntrance[sceneShortName][transitionNodeIndex];
        ApConnectionId targetEntrance = (ApConnectionId)Plugin.State.SlotData.Transitions[(int)sourceEntrance];
        Plugin.Logger.LogInfo($"ER: {sourceEntrance} -> {targetEntrance}");
        var scenePair = Data.EntranceToSceneTransition[targetEntrance];
        string targetSceneName = scenePair.Item1;
        int newTransitionNodeIndex = scenePair.Item2;
        Plugin.Logger.LogInfo($"Randomized scene: {targetSceneName} ({newTransitionNodeIndex})");

        Tp(targetSceneName, newTransitionNodeIndex);
        Plugin.Client.TraverseEntrance((int)sourceEntrance);
    }

    // Execute tp command
    public void TpCommand(string[] command)
    {
        if(command.Length != 3)
        {
            Client.ClientConsole.LogMessage("tp command takes two arguements '/tp <sceneName> <transitionNodeIndex>'");
            return;
        }

        string sceneName = command[1];
        if (!int.TryParse(command[2], out int transitionNodeIndex))
        {
            Client.ClientConsole.LogMessage("tp command's second argument must be an integer");
            return;
        }

        if (!Data.SceneTransitionToEntrance.ContainsKey(sceneName))
        {
            Client.ClientConsole.LogMessage("Unknown sceneName for tp command");
            return;
        }
        var sceneEntry = Data.SceneTransitionToEntrance[sceneName];
        if (!sceneEntry.ContainsKey(transitionNodeIndex))
        {
            Client.ClientConsole.LogMessage("Unknown transitionNodeIndex for tp command");
            Client.ClientConsole.LogMessage("Valid indices: "+string.Join(",", sceneEntry.Keys));
            return;
        }

        Client.ClientConsole.LogMessage($"Teleporting to scene {sceneName} ({transitionNodeIndex})");
        Tp(sceneName, transitionNodeIndex);
    }

    // Tp to entrance
    public void Tp(string sceneName, int transitionNodeIndex)
    {
        IsCmdTp = true;
        string scenePath = GetScenePath(sceneName);
        if(transitionNodeIndex < 0)
        {
            if(sceneName == "harborBusStop" || sceneName == "harborHydroplant")
                TpRaft(scenePath);
            if(sceneName == "mountainSkiCabin" || sceneName == "mountainSkiTop")
                TpSkiLift(scenePath);
        }
        else
        {
            SceneReference sceneRef = new()
            {
                scenePath = scenePath
            };
            SceneTransitionController.Instance.DoSceneTransition(sceneRef, transitionNodeIndex, LoadingIndicator.LoadingType.Standard);
        }
        IsCmdTp = false;
    }

    // Tp to raft
    public static void TpRaft(string scenePath)
    {
        RaftController.satAtBenchIndex = 0;
        var sitState = PlayerController.Instance.sitState;
        if(PlayerController.Instance.currentState != sitState)
        {
            sitState.sitTarget = PlayerController.Instance.transform;
            PlayerController.Instance.ChangePlayerState(sitState);
        }

        var companion = RaftController.GetLostDogCompanion();
        if (companion.isActive)
        {
            companion.companionControllerScript.SetFollowTarget(PlayerController.Instance.transform);
        }

        RaftController.isArrivingFromOtherSide = true;
        SceneReference sceneRef = new()
        {
            scenePath = scenePath
        };
        SceneTransitionController._Instance_k__BackingField.DoSceneTransitionEvent(sceneRef, LoadingIndicator.LoadingType.Standard);
    }

    // Tp to skilift
    public static void TpSkiLift(string scenePath)
    {
        var sitState = PlayerController.Instance.sitState;
        sitState.sitTarget = PlayerController.Instance.transform;
        PlayerController.Instance.ChangePlayerState(sitState);

        SkiliftController.CurrentState = SkiliftController.State.ArrivingOnOtherSide;
        SkiliftResetState = false;
        SceneReference sceneRef = new()
        {
            scenePath = scenePath
        };
        SceneTransitionController._Instance_k__BackingField.DoSceneTransitionEvent(sceneRef, LoadingIndicator.LoadingType.Standard);
    }

    public static bool ShouldER(LoadingIndicator.LoadingType loadingType)
    {
        int entrance_randomization = Plugin.State.SlotData?.Options.entrance_randomization ?? 0;
        return !Plugin.SceneManager.IsCmdTp && loadingType == LoadingIndicator.LoadingType.Standard && entrance_randomization != 0;
    }

    // Handle standard scene transitions
    [HarmonyPrefix, HarmonyPatch(typeof(SceneTransitionController), nameof(SceneTransitionController.DoSceneTransition))]
    public static bool Patch_DoSceneTransition(SceneReference sceneName, ref int transitionNodeIndex, ref LoadingIndicator.LoadingType loadingType)
    {
        Plugin.Logger.LogInfo($"SceneTransitionController.DoSceneTransition({sceneName.scenePath}, {transitionNodeIndex}, {loadingType})");
        if(!ShouldER(loadingType))
            return true;

        Plugin.SceneManager.TpER(sceneName.scenePath, transitionNodeIndex);
        return false;
    }

    // Handle raft and ski lift transitions (function also does Viking Express and Basto ending)
    [HarmonyPrefix, HarmonyPatch(typeof(SceneTransitionController), nameof(SceneTransitionController.DoSceneTransitionEvent))]
    public static bool Patch_DoSceneTransitionEvent(SceneReference sceneName, LoadingIndicator.LoadingType loadingType)
    {
        Plugin.Logger.LogInfo($"SceneTransitionController.DoSceneTransitionEvent({sceneName.scenePath}, {loadingType})");
        var match = Regex.Match(sceneName.scenePath, @"Assets/Scenes/([A-Za-z]*)/([A-Za-z0-9_]*)\.unity");
        string sceneShortName = match.Groups[2].Value;
        if(!ShouldER(loadingType) || sceneShortName == "resortDino")
            return true;

        if(sceneShortName == "harborBusStop" || sceneShortName == "harborHydroplant")
        {
            RaftController.isArrivingFromOtherSide = false;
            CameraController.Instance.ResetTarget();
            Cutscene.SetCutsceneState(false);
            OurInputManager.Instance.UpdateJoystickCursorState();
            
            var companion = RaftController.GetLostDogCompanion();
            if (companion.isActive)
            {
                companion.companionControllerScript.SetFollowTarget(null);
                companion.companionControllerScript.FollowPlayer();
            }
            FMOD.Studio.EventInstance.FMOD_Studio_EventInstance_SetParameterByID(RaftController.raftCutSceneEventInstance.handle, RaftController.raftCutsceneParameterID, 1.0f, false);
        }
        else if(sceneShortName == "mountainSkiCabin" || sceneShortName == "mountainSkiTop")
        {
            SkiliftResetState = true;
            CameraController.Instance.ResetTarget();
            Cutscene.SetCutsceneState(false);
        }
        Plugin.SceneManager.TpER(sceneName.scenePath, -1);
        return false;
    }

    // Reset SkiliftController on new Update if requested
    [HarmonyPostfix, HarmonyPatch(typeof(SkiliftController), nameof(SkiliftController.Update))]
    public static void Patch_SkiliftController_Update(SkiliftController __instance)
    {
        if (SkiliftResetState)
        {
            SkiliftController.CurrentState = SkiliftController.State.WaitingForLift;
            __instance.enabled = false;
            SkiliftResetState = false;
        }
    }
    
    // When ER arrives on raft before it's unlocked, temporarily unlock for RaftController.Start
    [HarmonyPrefix, HarmonyPatch(typeof(RaftController), nameof(RaftController.Start))]
    public static void Patch_RaftController_Start_Prefix(RaftController __instance, out bool __state)
    {
        __state = false;
        if (RaftController.isArrivingFromOtherSide && !__instance.raftKey.saveValueExist)
        {
            __instance.raftKey.saveValueExist = true;
            __state = true;
        }
    }

    // And relock at the end
    [HarmonyPostfix, HarmonyPatch(typeof(RaftController), nameof(RaftController.Start))]
    public static void Patch_RaftController_Start_Postfix(RaftController __instance, bool __state)
    {
        if (__state)
            __instance.raftKey.saveValueExist = false;
    }
}
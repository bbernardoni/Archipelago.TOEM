using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace Archipelago.TOEM.UI;

public class UIManager : MonoBehaviour
{
    private static UIManager Instance;

    public static void Init()
    {
        Plugin.Logger.LogDebug($"UIManager.Init()");
        ClassInjector.RegisterTypeInIl2Cpp<UIManager>();
        GameObject obj = new("AP_UI_Behaviour");
        DontDestroyOnLoad(obj);
        obj.hideFlags |= HideFlags.HideAndDontSave;
        Instance = obj.AddComponent<UIManager>();
    }

    public void Update()
    {
        Inputbox.UpdateAll();
        Text.UpdateAll();
        ConsoleUI.Update();
    }

    public void LateUpdate()
    {
        ConsoleUI.LateUpdate();
    }
}

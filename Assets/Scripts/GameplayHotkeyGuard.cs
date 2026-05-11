using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Skips gameplay hotkeys while the user is typing in a TMP input field.
/// </summary>
public static class GameplayHotkeyGuard
{
    public static bool IsTypingInTmpInputField()
    {
        if (EventSystem.current == null)
            return false;
        GameObject go = EventSystem.current.currentSelectedGameObject;
        if (go == null)
            return false;
        return go.GetComponent<TMP_InputField>() != null;
    }
}

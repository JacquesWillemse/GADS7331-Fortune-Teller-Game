using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Demon voice: same structural rules as the smoke-test reader but net harmful, ominous, and degrading.
/// Pair with <see cref="TarotReadingSmokeTest"/> for side-by-side LLM checks. Judge/ruling comes later.
/// </summary>
public class DemonTarotReader : MonoBehaviour
{
    /// <summary> UI target for demon prose; duel pipeline can mirror here if its own field is unset. </summary>
    public TMP_Text DemonOutputText => demonOutput;

    [SerializeField] private OllamaClient ollama;
    [SerializeField] private TarotCardPull cardPull;
    [SerializeField] private bool requestOnStart;
    [SerializeField] private bool listenForHotkey;
    [SerializeField] private Key demonTestKey = Key.D;
    [SerializeField] private TMP_Text demonOutput;
    [SerializeField] private bool logToConsole = true;

    [Header("Prompt tuning (optional)")]
    [Tooltip("Appended to the demon prompt so you can iterate in the Inspector without code changes.")]
    [SerializeField, TextArea(3, 12)] private string additionalDemonInstructions = "";

    private bool _requestInFlight;
    private readonly List<TarotCardData> _spreadBuffer = new List<TarotCardData>();

    private void Start()
    {
        if (requestOnStart)
            RequestFromPull();
    }

    private void Update()
    {
        if (!listenForHotkey)
            return;
        if (GameplayHotkeyGuard.IsTypingInTmpInputField())
            return;
        if (Keyboard.current == null)
            return;
        if (!Keyboard.current[demonTestKey].wasPressedThisFrame)
            return;
        RequestFromPull();
    }

    /// <summary>
    /// Builds the spread from <see cref="TarotCardPull"/> (same slice as smoke test).
    /// </summary>
    public void RequestFromPull()
    {
        if (cardPull == null)
        {
            LogLine("DemonTarotReader: assign TarotCardPull reference.");
            return;
        }
        if (ollama == null)
        {
            LogLine("DemonTarotReader: assign OllamaClient reference.");
            return;
        }
        if (!TarotPullSpreadBuilder.TryBuildSpreadForLlm(_spreadBuffer, cardPull))
        {
            LogLine("DemonTarotReader: could not build spread (ensure TarotCardPull ran and description texts are set).");
            return;
        }

        RequestDemonReading(_spreadBuffer);
    }

    /// <summary>
    /// Demon interpretation for an arbitrary spread (later: inject player text + agreement gate).
    /// </summary>
    public void RequestDemonReading(IReadOnlyList<TarotCardData> cards)
    {
        if (_requestInFlight)
        {
            LogLine("DemonTarotReader: request already in flight.");
            return;
        }
        if (ollama == null || cards == null || cards.Count == 0)
            return;

        string prompt = DemonTarotPrompts.BuildReadingPrompt(cards, additionalDemonInstructions);
        _requestInFlight = true;
        SetOutput("(Summoning demon reading…)");

        ollama.Generate(
            prompt,
            text =>
            {
                _requestInFlight = false;
                SetOutput(text);
                if (logToConsole)
                    Debug.Log("[Demon LLM]\n" + text);
            },
            err =>
            {
                _requestInFlight = false;
                SetOutput("Error: " + err);
                Debug.LogWarning("[Demon LLM] " + err);
            });
    }

    private void SetOutput(string text)
    {
        if (demonOutput != null)
            demonOutput.text = text;
    }

    private void LogLine(string msg)
    {
        if (logToConsole)
            Debug.Log(msg);
    }
}

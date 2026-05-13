using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
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
    [Tooltip("Optional extra labels (e.g. spirit view / cards canvas) that receive the same text as demonOutput.")]
    [SerializeField] private TMP_Text[] mirrorOutputs;
    [Tooltip("First line while waiting for the model. Ignored when RequestFromPull(skipInitialOutput: true) is used.")]
    [SerializeField, TextArea(1, 4)] private string initialLoadingMessage = "";
    [Tooltip("String.Format — {0} = error message from the client. If empty, only the raw error is shown.")]
    [SerializeField] private string errorMessageFormat = "";

    [SerializeField] private bool logToConsole = true;

    [Header("Events")]
    [Tooltip("Fires whenever the spirit/demon reply text is set (including loading and error lines).")]
    public UnityEvent<string> onResponseText;

    [Header("Two-pass reading")]
    [Tooltip("Pass 1: compact JSON outline. Pass 2: spoken curse bound to that outline. Falls back to one pass if outline JSON cannot be parsed.")]
    [SerializeField] private bool useTwoPassReading = true;

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

        RequestDemonReading(_spreadBuffer, skipInitialOutput: false);
    }

    /// <summary>Same as <see cref="RequestFromPull"/> but can skip the loading line (e.g. when <see cref="FortuneFlowController"/> sets spirit UI text first).</summary>
    public void RequestFromPull(bool skipInitialOutput)
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

        RequestDemonReading(_spreadBuffer, skipInitialOutput);
    }

    /// <summary>
    /// Demon interpretation for an arbitrary spread (later: inject player text + agreement gate).
    /// </summary>
    public void RequestDemonReading(IReadOnlyList<TarotCardData> cards, bool skipInitialOutput = false)
    {
        if (_requestInFlight)
        {
            LogLine("DemonTarotReader: request already in flight.");
            return;
        }
        if (ollama == null || cards == null || cards.Count == 0)
            return;

        _requestInFlight = true;
        if (!skipInitialOutput && !string.IsNullOrEmpty(initialLoadingMessage))
            SetOutput(initialLoadingMessage);

        StartCoroutine(CoRequestDemonReading(cards));
    }

    IEnumerator CoRequestDemonReading(IReadOnlyList<TarotCardData> cards)
    {
        try
        {
            string ok = null;
            string err = null;
            yield return StartCoroutine(DemonTarotTwoPass.CoGenerate(
                ollama,
                cards,
                useTwoPassReading,
                additionalDemonInstructions,
                s => ok = s,
                e => err = e,
                null));

            if (!string.IsNullOrEmpty(err))
            {
                SetOutput(FormatError(err));
                Debug.LogWarning("[Demon LLM] " + err);
                yield break;
            }

            SetOutput(ok ?? "");
            if (logToConsole)
                Debug.Log("[Demon LLM]\n" + ok);
        }
        finally
        {
            _requestInFlight = false;
        }
    }

    string FormatError(string err)
    {
        if (string.IsNullOrEmpty(errorMessageFormat))
            return err ?? "";
        try
        {
            return string.Format(errorMessageFormat, err);
        }
        catch (System.FormatException)
        {
            return err ?? "";
        }
    }

    private void SetOutput(string text)
    {
        ApplyTextTo(demonOutput, text);
        if (mirrorOutputs != null)
        {
            for (int i = 0; i < mirrorOutputs.Length; i++)
                ApplyTextTo(mirrorOutputs[i], text);
        }

        onResponseText?.Invoke(text);
    }

    static void ApplyTextTo(TMP_Text label, string text)
    {
        if (label == null)
            return;
        if (!label.gameObject.activeSelf)
            label.gameObject.SetActive(true);

        label.text = text;
        label.ForceMeshUpdate(true);

        if (!label.gameObject.activeInHierarchy)
            Debug.LogWarning(
                $"[DemonTarotReader] Text was set on \"{label.name}\" but it is not active in the hierarchy — a parent may be disabled (e.g. wrong canvas for the current camera).",
                label);
    }

    private void LogLine(string msg)
    {
        if (logToConsole)
            Debug.Log(msg);
    }
}

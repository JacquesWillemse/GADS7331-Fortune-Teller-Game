using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Chains: player text → demon gate (JSON) → if not agreed, demon reading → rule-based fortune duel score (no judge LLM).
/// Wire <see cref="playerReadingInput"/>, <see cref="ollama"/>, <see cref="cardPull"/>, optional UI texts, then call <see cref="RunDuel"/> from a button or hotkey.
/// </summary>
public class TarotReadingDuelPipeline : MonoBehaviour
{
    [SerializeField] private OllamaClient ollama;
    [SerializeField] private TarotCardPull cardPull;
    [SerializeField] private TMP_InputField playerReadingInput;

    [Header("Optional UI")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text gateReasonText;
    [SerializeField] private TMP_Text demonReadingText;
    [Tooltip("If Demon Reading Text is empty, assign the same Demon Tarot Reader used for solo D tests — duel demon output will be copied to its output label.")]
    [SerializeField] private DemonTarotReader demonReadingOutputFallback;
    [SerializeField] private TMP_Text judgeResultText;

    [Header("Demon reading (same extras as DemonTarotReader)")]
    [SerializeField, TextArea(2, 8)] private string additionalDemonInstructions = "";

    [Header("Input")]
    [SerializeField] private bool listenForHotkey;
    [SerializeField] private Key runDuelKey = Key.J;

    [Header("Fortune duel scoring")]
    [Tooltip("Legacy dev slider: 0–10 maps to 0–100 magical energy for FortuneDuelRubric when using this pipeline directly.")]
    [SerializeField, Range(0, 10)] private int playerEnergyBonusForJudge;
    [Tooltip("When totals tie, the player wins if true.")]
    [SerializeField] private bool tieRoundGoesToPlayer = true;

    [Header("Debug")]
    [Tooltip("Logs [Duel][Gate], full [Duel][Demon] body, and [Duel][Judge] (when those steps run).")]
    [SerializeField] private bool logGateAndJudgeToConsole = true;
    [Tooltip("Logs every status step (Gate…, Demon…, Judge…).")]
    [SerializeField] private bool logStatusToConsole;

    [Header("Events")]
    public UnityEvent<bool> onGateAgreedWithPlayer;
    public UnityEvent<string> onDemonReadingComplete;
    public UnityEvent<bool, string> onJudgeComplete;

    private readonly List<TarotCardData> _spread = new List<TarotCardData>();
    private bool _busy;

    private void Update()
    {
        if (!listenForHotkey || _busy)
            return;
        if (GameplayHotkeyGuard.IsTypingInTmpInputField())
            return;
        if (Keyboard.current == null || runDuelKey == Key.None)
            return;
        if (!Keyboard.current[runDuelKey].wasPressedThisFrame)
            return;
        RunDuel();
    }

    /// <summary>
    /// Run full duel from current <see cref="playerReadingInput"/> text and pulled cards.
    /// </summary>
    public void RunDuel()
    {
        if (_busy)
        {
            SetStatus("Duel already running.");
            return;
        }
        if (ollama == null || cardPull == null)
        {
            SetStatus("Assign OllamaClient and TarotCardPull.");
            return;
        }
        if (playerReadingInput == null)
        {
            SetStatus("Assign player TMP_InputField.");
            return;
        }

        string playerText = playerReadingInput.text?.Trim() ?? "";
        if (string.IsNullOrEmpty(playerText))
        {
            SetStatus("Enter the player's reading first.");
            return;
        }

        if (!TarotPullSpreadBuilder.TryBuildSpreadForLlm(_spread, cardPull))
        {
            SetStatus("Could not build spread from TarotCardPull.");
            return;
        }

        StartCoroutine(RunDuelCoroutine(playerText));
    }

    private IEnumerator RunDuelCoroutine(string playerReading)
    {
        _busy = true;
        ClearOutputs();

        // --- Gate ---
        SetStatus("Demon gate…");
        string gateRaw = null;
        string gateErr = null;
        yield return StartCoroutine(ollama.GenerateWait(
            DemonTarotPrompts.BuildGatePrompt(_spread, playerReading),
            s => gateRaw = s,
            e => gateErr = e));

        if (!string.IsNullOrEmpty(gateErr))
        {
            SetStatus("Gate error: " + gateErr);
            Debug.LogWarning("[Duel][Gate] error: " + gateErr);
            _busy = false;
            yield break;
        }

        bool agreed = false;
        string gateReason = "";
        if (TarotLlmJsonHelpers.TryParseDemonGate(gateRaw, out DemonGateJson gateJson))
        {
            agreed = gateJson.demon_agrees_with_player;
            gateReason = gateJson.reason ?? "";
        }
        else
        {
            agreed = InferAgreeFromRaw(gateRaw);
            gateReason = "(Could not parse gate JSON; guessed from text.)";
        }

        bool modelSaidAgree = agreed;
        if (agreed && DemonGatePositiveHeuristic.ShouldForceDemonDisagree(playerReading))
        {
            agreed = false;
            gateReason = "(Corrected: reading is net hopeful / releasing — demon will counter.) " + gateReason;
            if (logGateAndJudgeToConsole)
                Debug.Log("[Duel][Gate] Model agreed, but hope/release heuristic forced demon_agrees_with_player=false.");
        }

        if (gateReasonText != null)
            gateReasonText.text = gateReason;
        onGateAgreedWithPlayer?.Invoke(agreed);

        if (logGateAndJudgeToConsole)
        {
            Debug.Log($"[Duel][Gate] demon_agrees_with_player={agreed} (model={modelSaidAgree}) | reason: {gateReason}");
            if (agreed)
                Debug.Log("[Duel][Gate] Demon accepts the player's reading — no counter-reading, judge skipped.");
        }

        if (agreed)
        {
            SetStatus("Demon agrees with the player — no counter-reading, no judge.");
            if (demonReadingText != null)
                demonReadingText.text = "";
            if (judgeResultText != null)
                judgeResultText.text = "";
            _busy = false;
            yield break;
        }

        // --- Demon counter-reading ---
        SetStatus("Demon counter-reading…");
        string demonText = null;
        string demonErr = null;
        yield return StartCoroutine(ollama.GenerateWait(
            DemonTarotPrompts.BuildReadingPrompt(_spread, additionalDemonInstructions),
            s => demonText = s,
            e => demonErr = e));

        if (!string.IsNullOrEmpty(demonErr))
        {
            SetStatus("Demon error: " + demonErr);
            Debug.LogWarning("[Duel][Demon] error: " + demonErr);
            _busy = false;
            yield break;
        }

        ApplyDemonReadingToUi(demonText);
        if (logGateAndJudgeToConsole)
            Debug.Log("[Duel][Demon]\n" + demonText);
        onDemonReadingComplete?.Invoke(demonText);

        // --- Fortune duel score (deterministic rubric; no judge LLM) ---
        SetStatus("Scoring duel…");
        FortuneDuelScoreBreakdown duel = FortuneDuelRubric.Compute(
            _spread,
            playerReading,
            demonText ?? "",
            Mathf.Clamp(playerEnergyBonusForJudge * 10f, 0f, 100f));

        float energyUi = Mathf.Clamp(playerEnergyBonusForJudge * 10f, 0f, 100f);
        bool guaranteed = FortuneDuelRubric.IsGuaranteedPlayerWin(energyUi);
        bool playerWon = guaranteed
            || (!guaranteed && duel.PlayerTotal > duel.DemonTotal)
            || (!guaranteed && duel.PlayerTotal == duel.DemonTotal && tieRoundGoesToPlayer);

        string explanation = FortuneDuelRubric.BuildVerdictExplanationOneSentence(duel, playerWon, guaranteed, tieRoundGoesToPlayer);
        string detailLog = FortuneDuelRubric.FormatRationale(duel, playerWon, energyUi, guaranteed);

        if (judgeResultText != null)
        {
            judgeResultText.text =
                (playerWon ? "Winner: Player" : "Winner: Spirit") + "\n" + explanation;
        }

        SetStatus("Complete.");

        if (logGateAndJudgeToConsole)
        {
            string w = playerWon ? "Player" : "Spirit";
            Debug.Log($"[Duel][Judge] winner={w} | {explanation}\n{detailLog}");
        }

        onJudgeComplete?.Invoke(playerWon, explanation);
        _busy = false;
    }

    private static bool InferAgreeFromRaw(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return false;
        string t = raw.ToLowerInvariant().Replace(" ", "");
        if (t.Contains("\"demon_agrees_with_player\":true"))
            return true;
        if (t.Contains("\"demon_agrees_with_player\":false"))
            return false;
        return false;
    }

    private void ApplyDemonReadingToUi(string demonText)
    {
        ApplyTmp(demonReadingText, demonText);
        if (demonReadingOutputFallback != null && demonReadingOutputFallback.DemonOutputText != null &&
            demonReadingOutputFallback.DemonOutputText != demonReadingText)
            ApplyTmp(demonReadingOutputFallback.DemonOutputText, demonText);
    }

    static void ApplyTmp(TMP_Text t, string s)
    {
        if (t == null)
            return;
        if (!t.gameObject.activeSelf)
            t.gameObject.SetActive(true);
        t.text = s;
        t.ForceMeshUpdate(true);
    }

    private void ClearOutputs()
    {
        if (gateReasonText != null) gateReasonText.text = "";
        if (demonReadingText != null)
            demonReadingText.text = "";
        if (demonReadingOutputFallback != null && demonReadingOutputFallback.DemonOutputText != null &&
            demonReadingOutputFallback.DemonOutputText != demonReadingText)
            demonReadingOutputFallback.DemonOutputText.text = "";
        if (judgeResultText != null) judgeResultText.text = "";
    }

    private void SetStatus(string msg)
    {
        if (statusText != null)
            statusText.text = msg;
        if (logStatusToConsole)
            Debug.Log("[Duel] " + msg);
    }
}

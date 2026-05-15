using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Tent flow: draw 3 cards → read fortune (player text UI, no score) → spirit LLM starts only after Read Fortune → open Cards/Spirit view →
/// open Judge view and press <see cref="RenderVerdict"/> when ready.
/// Magical energy for the duel is chosen on the judge slider, then committed (subtracted from global run energy) when the player presses Read Fortune; <see cref="RenderVerdict"/> uses that committed amount.
/// Scoring uses <see cref="FortuneDuelRubric"/> (spread moral judge bias, theme rubric, energy); optional <see cref="useLlmJudgeProse"/> sends settled facts to Ollama for booth prose only. Assign the three <see cref="TMP_Text"/> outputs in the Inspector; all visible copy comes from <see cref="outputStrings"/>.
/// </summary>
public class FortuneFlowController : MonoBehaviour
{
    [Header("Outputs (assign 3 TextMeshPro UGUI labels)")]
    [Tooltip("Filled when the player presses Read Fortune (from the input field).")]
    public TMP_Text playerFortuneOutput;
    [Tooltip("Set to SpiritThinkingMessage when the spirit request starts (after Read Fortune); replaced when the model returns.")]
    public TMP_Text spiritFortuneOutput;
    [Tooltip("Optional mirror on Read Fortune, optional mirror when spirit responds, and the final verdict block from Render Verdict.")]
    public TMP_Text judgeOutput;

    [SerializeField] private TarotCardPull cardPull;
    [Tooltip("Optional — keeps GameManager.CardsDrawn in sync and receives round reset after Accept Verdict.")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private DemonTarotReader spiritReader;
    [Tooltip("Same OllamaClient as the spirit reader. If empty, uses spiritReader.Ollama when available.")]
    [SerializeField] private OllamaClient ollama;
    [SerializeField] private TMP_InputField playerFortuneInput;
    [SerializeField] private Slider magicalEnergySlider;
    [Tooltip("Shows the duel magical energy slider value (0–100). Optional.")]
    [SerializeField] private TMP_Text magicalEnergyValueText;

    [Header("Judge UI gate")]
    [Tooltip("Assign the same button that calls RenderVerdict (e.g. Make Judgement). Kept disabled until cards are drawn, fortune read, and spirit reply is present — blocks stray Submit / double wiring.")]
    [SerializeField] private Button renderVerdictButton;
    [Tooltip("Pull Cards — disabled after a spread is drawn until Accept Verdict resets the round.")]
    [SerializeField] private Button drawCardsButton;
    [Tooltip("Accept Verdict — enabled only after Make Judgement has run for this round (until accepted or reset).")]
    [SerializeField] private Button acceptJudgementButton;

    [Header("Player reading coach")]
    [Tooltip("If set, assigned to the TMP_InputField placeholder so players know to close with themes + moral lean twisted toward hope.")]
    [SerializeField, TextArea(3, 10)]
    private string playerFortuneClosingPlaceholderHint =
        "Close with one sentence tying the three card moods together and whether they lean kind, severe, or cruel toward the listener — resolve into hope or mercy (do not paste card titles).";

    [Tooltip("All user-visible strings for this flow. Leave formats empty to skip that write.")]
    [SerializeField] private FortuneFlowOutputStrings outputStrings = new FortuneFlowOutputStrings();

    [Tooltip("If true, when totals tie after scoring, the player wins.")]
    [SerializeField] private bool tieRoundGoesToPlayer = true;

    [Header("Judge prose (LLM)")]
    [Tooltip("When on, Make Judgement runs the rubric first, then Ollama writes 3–5 sentences using required keywords. Falls back to one-line rubric text if Ollama is missing or errors.")]
    [SerializeField] private bool useLlmJudgeProse = true;

    [Header("Events")]
    public UnityEvent<bool, string> onVerdictComplete;

    readonly List<TarotCardData> _spread = new List<TarotCardData>();
    string _playerFortuneForJudge = "";
    bool _cardsDrawn;
    bool _verdictAwaitingAccept;
    bool _lastPlayerWon;
    int _lastScoreMargin;
    bool _lastGuaranteedWin;

    /// <summary>Magical energy committed when the player pressed Read Fortune (deducted from global); used for <see cref="RenderVerdict"/> scoring.</summary>
    int _committedMagicalEnergyForDuel;
    bool _spiritReadingStartedThisRound;
    /// <summary>False from the moment a new pull starts until <see cref="ResetRoundAfterVerdictAccepted"/>.</summary>
    bool _drawCardsAllowed = true;
    bool _verdictProseInFlight;
    Coroutine _verdictProseCoroutine;

    const string DefaultSpiritThinkingMessage = "The spirit is thinking…";
    const string DefaultJudgeThinkingMessage = "The booth weighs the duel…";

    public bool CardsDrawn => _cardsDrawn;

    /// <summary>True after a successful <see cref="RenderVerdict"/> until <see cref="ResetRoundAfterVerdictAccepted"/> or a new draw.</summary>
    public bool VerdictAwaitingAccept => _verdictAwaitingAccept;

    /// <summary>Margin = player total − spirit total (positive means teller ahead).</summary>
    public bool TryGetLastVerdict(out bool playerWon, out int scoreMargin, out bool guaranteedWin)
    {
        playerWon = _lastPlayerWon;
        scoreMargin = _lastScoreMargin;
        guaranteedWin = _lastGuaranteedWin;
        return _verdictAwaitingAccept;
    }

    void Start()
    {
        ApplyPlayerFortunePlaceholderHint();
        RefreshDrawCardsButtonInteractable();
        RefreshRenderVerdictButtonInteractable();
    }

    void Awake()
    {
        if (outputStrings == null)
            outputStrings = new FortuneFlowOutputStrings();

        if (magicalEnergySlider != null)
        {
            magicalEnergySlider.minValue = 0f;
            magicalEnergySlider.maxValue = 100f;
            magicalEnergySlider.wholeNumbers = true;
            magicalEnergySlider.onValueChanged.AddListener(OnMagicalEnergySliderChanged);
            ResetMagicalEnergySliderForNewReading();
        }

        // Subscribe here (not OnEnable): if this object lives under the tent UI, switching to the card
        // camera disables the tent first and OnDisable would remove listeners before the LLM returns.
        if (cardPull != null)
            cardPull.onCardPullComplete.AddListener(OnCardPullComplete);
        if (spiritReader != null)
            spiritReader.onResponseText.AddListener(OnSpiritResponseText);
    }

    void OnDestroy()
    {
        if (_verdictProseCoroutine != null)
            StopCoroutine(_verdictProseCoroutine);
        if (cardPull != null)
            cardPull.onCardPullComplete.RemoveListener(OnCardPullComplete);
        if (spiritReader != null)
            spiritReader.onResponseText.RemoveListener(OnSpiritResponseText);
        if (magicalEnergySlider != null)
            magicalEnergySlider.onValueChanged.RemoveListener(OnMagicalEnergySliderChanged);
    }

    void OnSpiritResponseText(string text)
    {
        WriteTmp(spiritFortuneOutput, text, nameof(FortuneFlowController));
        if (judgeOutput != null && outputStrings != null && !string.IsNullOrEmpty(outputStrings.judgeMirrorSpiritOnResponseFormat))
        {
            string addition = SafeFormat(outputStrings.judgeMirrorSpiritOnResponseFormat, text);
            string existing = judgeOutput.text ?? "";
            if (string.IsNullOrEmpty(existing))
                WriteTmp(judgeOutput, addition, nameof(FortuneFlowController));
            else
                WriteTmp(judgeOutput, existing + "\n\n" + addition, nameof(FortuneFlowController));
        }

        RefreshRenderVerdictButtonInteractable();
    }

    void OnCardPullComplete()
    {
        _cardsDrawn = true;
        gameManager?.SetCardsDrawn(true);
        LogFlow(outputStrings != null ? outputStrings.logCardsReady : null);
        RefreshDrawCardsButtonInteractable();
        RefreshRenderVerdictButtonInteractable();
    }

    /// <summary>Wired to Draw Cards button. One pull per round; draw stays disabled until Accept Verdict.</summary>
    public void OnDrawCards()
    {
        if (cardPull == null)
        {
            LogFlow(outputStrings != null ? outputStrings.logAssignCardPull : null);
            return;
        }

        if (!_drawCardsAllowed)
        {
            LogFlow(!string.IsNullOrEmpty(outputStrings?.logDrawLockedUntilVerdict)
                ? outputStrings.logDrawLockedUntilVerdict
                : "You already pulled cards for this reading — accept the verdict to start the next customer.");
            return;
        }

        spiritReader?.CancelActiveReading();

        _drawCardsAllowed = false;
        RefreshDrawCardsButtonInteractable();

        RefundCommittedMagicalEnergyIfAny();
        _cardsDrawn = false;
        _verdictAwaitingAccept = false;
        _playerFortuneForJudge = "";
        _spiritReadingStartedThisRound = false;
        gameManager?.SetCardsDrawn(false);
        WriteTmp(playerFortuneOutput, "", nameof(FortuneFlowController));
        WriteTmp(spiritFortuneOutput, "", nameof(FortuneFlowController));
        WriteTmp(judgeOutput, "", nameof(FortuneFlowController));
        cardPull.HideCards();
        cardPull.ClearPullHistory();
        cardPull.CardPull();
        LogFlow(outputStrings != null ? outputStrings.logDrawing : null);
        RefreshDrawCardsButtonInteractable();
        RefreshRenderVerdictButtonInteractable();
    }

    /// <summary>Wired to Read Fortune — fills the player output (and optionally the judge mirror). No verdict.</summary>
    public void OnReadFortune()
    {
        string t = playerFortuneInput != null ? playerFortuneInput.text?.Trim() ?? "" : "";
        if (string.IsNullOrEmpty(t))
        {
            LogFlow(outputStrings != null ? outputStrings.logEnterFortuneFirst : null);
            return;
        }

        // Slider chooses how much global energy to commit for this reading; charged now so HUD updates immediately.
        RefundCommittedMagicalEnergyIfAny();
        int request = magicalEnergySlider != null ? Mathf.RoundToInt(magicalEnergySlider.value) : 0;
        request = Mathf.Clamp(request, 0, 100);
        if (gameManager != null && request > 0)
            _committedMagicalEnergyForDuel = gameManager.TrySpendMagicalEnergy(request);
        else
            _committedMagicalEnergyForDuel = 0;
        ResetMagicalEnergySliderForNewReading();

        _playerFortuneForJudge = t;
        WriteTmp(playerFortuneOutput, t, nameof(FortuneFlowController));
        if (judgeOutput != null && outputStrings != null && !string.IsNullOrEmpty(outputStrings.judgeMirrorPlayerOnReadFormat))
            WriteTmp(judgeOutput, SafeFormat(outputStrings.judgeMirrorPlayerOnReadFormat, t), nameof(FortuneFlowController));
        LogFlow(outputStrings != null ? outputStrings.logAfterReadFortune : null);
        if (_cardsDrawn)
            TryBeginSpiritReadingAfterFortune();
        RefreshRenderVerdictButtonInteractable();
    }

    /// <summary>Call when the Cards or Spirit view opens. Starts the spirit only if Read Fortune has already run this round.</summary>
    public void OnCardsViewOpened()
    {
        if (!_cardsDrawn)
        {
            LogFlow(outputStrings != null ? outputStrings.logDrawCardsFirst : null);
            return;
        }

        if (spiritReader == null)
        {
            LogFlow(outputStrings != null ? outputStrings.logAssignSpiritReader : null);
            return;
        }

        if (string.IsNullOrEmpty(_playerFortuneForJudge))
        {
            LogFlow(!string.IsNullOrEmpty(outputStrings?.logReadFortuneBeforeSpirit)
                ? outputStrings.logReadFortuneBeforeSpirit
                : "Read your fortune (Make Reading) before summoning the spirit — the spirit starts only after that.");
            return;
        }

        TryBeginSpiritReadingAfterFortune();
        RefreshRenderVerdictButtonInteractable();
    }

    void TryBeginSpiritReadingAfterFortune()
    {
        if (!_cardsDrawn || spiritReader == null)
            return;
        if (string.IsNullOrEmpty(_playerFortuneForJudge))
            return;
        if (_spiritReadingStartedThisRound)
            return;

        string thinking = ResolvedSpiritThinkingMessage();
        WriteTmp(spiritFortuneOutput, thinking, nameof(FortuneFlowController));
        spiritReader.RequestFromPull(skipInitialOutput: true);
        _spiritReadingStartedThisRound = true;
        LogFlow(outputStrings != null ? outputStrings.logSpiritRequested : null);
    }

    string ResolvedSpiritThinkingMessage()
    {
        if (outputStrings != null && !string.IsNullOrWhiteSpace(outputStrings.spiritThinkingMessage))
            return outputStrings.spiritThinkingMessage.Trim();
        return DefaultSpiritThinkingMessage;
    }

    /// <summary>Wired to a button on the Judge panel (e.g. "Render Verdict"). Uses player fortune, spirit output, and magical energy.</summary>
    public void RenderVerdict()
    {
        if (_verdictProseInFlight)
        {
            LogFlow("Verdict prose is still being written…");
            return;
        }

        if (renderVerdictButton != null && !renderVerdictButton.interactable)
        {
            LogFlow("Make Judgement is not available yet — finish the spirit reading first.");
            return;
        }

        if (_verdictAwaitingAccept)
        {
            LogFlow("Verdict already shown — accept it before rendering again.");
            return;
        }

        if (!_cardsDrawn)
        {
            LogFlow(outputStrings != null ? outputStrings.logJudgeNeedDraw : null);
            return;
        }

        if (string.IsNullOrEmpty(_playerFortuneForJudge))
        {
            LogFlow(outputStrings != null ? outputStrings.logJudgeNeedReadFortune : null);
            return;
        }

        string spirit = GetSpiritTextForScoring();
        if (string.IsNullOrEmpty(spirit))
        {
            LogFlow(outputStrings != null ? outputStrings.logJudgeNeedSpirit : null);
            return;
        }

        if (!TarotPullSpreadBuilder.TryBuildSpreadForLlm(_spread, cardPull))
        {
            LogFlow(outputStrings != null ? outputStrings.logJudgeSpreadFailed : null);
            return;
        }

        float energy = Mathf.Clamp(_committedMagicalEnergyForDuel, 0f, 100f);
        FortuneDuelScoreBreakdown duel = FortuneDuelRubric.Compute(_spread, _playerFortuneForJudge, spirit, energy);
        bool guaranteed = FortuneDuelRubric.IsGuaranteedPlayerWin(energy);
        bool playerWon;
        if (guaranteed)
            playerWon = true;
        else if (duel.PlayerTotal > duel.DemonTotal)
            playerWon = true;
        else if (duel.DemonTotal > duel.PlayerTotal)
            playerWon = false;
        else
            playerWon = tieRoundGoesToPlayer;

        _lastScoreMargin = duel.PlayerTotal - duel.DemonTotal;
        _lastPlayerWon = playerWon;
        _lastGuaranteedWin = guaranteed;

        string explanation = FortuneDuelRubric.BuildVerdictExplanationOneSentence(duel, playerWon, guaranteed, tieRoundGoesToPlayer);
        string rationale = FortuneDuelRubric.FormatRationale(duel, playerWon, energy, guaranteed);
        string winnerLabel = playerWon
            ? (outputStrings != null ? outputStrings.winnerPlayerLabel ?? "" : "")
            : (outputStrings != null ? outputStrings.winnerSpiritLabel ?? "" : "");

        OllamaClient client = ResolveOllamaClient();
        if (!useLlmJudgeProse || client == null)
        {
            FinishVerdictDisplay(_playerFortuneForJudge, spirit, winnerLabel, explanation, playerWon, rationale);
            return;
        }

        if (_verdictProseCoroutine != null)
            StopCoroutine(_verdictProseCoroutine);
        _verdictProseCoroutine = StartCoroutine(CoRenderVerdictProse(
            client, duel, playerWon, guaranteed, energy, winnerLabel, explanation, rationale,
            _playerFortuneForJudge, spirit));
    }

    OllamaClient ResolveOllamaClient()
    {
        if (ollama != null)
            return ollama;
        if (spiritReader != null && spiritReader.Ollama != null)
            return spiritReader.Ollama;
        return null;
    }

    IEnumerator CoRenderVerdictProse(
        OllamaClient client,
        FortuneDuelScoreBreakdown duel,
        bool playerWon,
        bool guaranteed,
        float energy,
        string winnerLabel,
        string explanation,
        string rationale,
        string playerFortune,
        string spiritText)
    {
        _verdictProseInFlight = true;
        RefreshRenderVerdictButtonInteractable();

        WriteTmp(judgeOutput, ResolvedJudgeThinkingMessage(), nameof(FortuneFlowController));

        string prompt = JudgeVerdictProsePrompts.BuildProsePrompt(
            _spread, duel, playerWon, guaranteed, energy, explanation, playerFortune, spiritText);

        string prose = null;
        string err = null;
        yield return client.StartCoroutine(client.GenerateWait(
            prompt,
            s => prose = s,
            e => err = e));

        _verdictProseInFlight = false;
        _verdictProseCoroutine = null;

        if (!string.IsNullOrEmpty(err) || string.IsNullOrWhiteSpace(prose))
        {
            LogFlow(outputStrings != null ? outputStrings.logJudgeProseFailed : null);
            Debug.LogWarning("[Fortune][Judge] Prose LLM failed; using rubric one-liner. " + err);
            FinishVerdictDisplay(playerFortune, spiritText, winnerLabel, explanation, playerWon, rationale);
            yield break;
        }

        FinishVerdictDisplay(playerFortune, spiritText, winnerLabel, prose.Trim(), playerWon, rationale);
    }

    void FinishVerdictDisplay(
        string playerFortune,
        string spiritText,
        string winnerLabel,
        string explanationOrProse,
        bool playerWon,
        string rationaleForLog)
    {
        _verdictAwaitingAccept = true;
        string block = BuildJudgeVerdictBlock(playerFortune, spiritText, winnerLabel, explanationOrProse, playerWon);
        WriteTmp(judgeOutput, block, nameof(FortuneFlowController));
        LogFlow(outputStrings != null ? outputStrings.logVerdictRendered : null);
        onVerdictComplete?.Invoke(playerWon, explanationOrProse);
        Debug.Log($"[Fortune][Judge] winner={(playerWon ? "Player" : "Spirit")} | customer-facing prose:\n{explanationOrProse}");
        Debug.Log($"[Fortune][Judge] rubric breakdown (console only):\n{rationaleForLog}");
        RefreshRenderVerdictButtonInteractable();
    }

    string ResolvedJudgeThinkingMessage()
    {
        if (outputStrings != null && !string.IsNullOrWhiteSpace(outputStrings.judgeThinkingMessage))
            return outputStrings.judgeThinkingMessage.Trim();
        return DefaultJudgeThinkingMessage;
    }

    /// <summary>Optional: force the duel slider from run meta-energy. Normally unused — the duel slider is player-set each reading.</summary>
    public void ApplyGameEnergyToDuelSlider(int energy0to100)
    {
        if (magicalEnergySlider != null)
            magicalEnergySlider.value = Mathf.Clamp(energy0to100, 0f, 100f);
    }

    void OnMagicalEnergySliderChanged(float _) => RefreshMagicalEnergyValueText();

    void RefreshMagicalEnergyValueText()
    {
        if (magicalEnergyValueText == null || magicalEnergySlider == null)
            return;
        magicalEnergyValueText.text = Mathf.RoundToInt(magicalEnergySlider.value).ToString();
    }

    /// <summary>0–100 duel commitment; call after each accepted verdict and on load.</summary>
    public void ResetMagicalEnergySliderForNewReading()
    {
        if (magicalEnergySlider == null)
            return;
        magicalEnergySlider.value = 0f;
        RefreshMagicalEnergyValueText();
    }

    /// <summary>Clears flow state for the next customer after resources are applied on Accept Verdict.</summary>
    public void ResetRoundAfterVerdictAccepted()
    {
        _verdictAwaitingAccept = false;
        _cardsDrawn = false;
        _playerFortuneForJudge = "";
        gameManager?.SetCardsDrawn(false);
        if (playerFortuneInput != null)
            playerFortuneInput.text = "";
        WriteTmp(playerFortuneOutput, "", nameof(FortuneFlowController));
        WriteTmp(spiritFortuneOutput, "", nameof(FortuneFlowController));
        WriteTmp(judgeOutput, "", nameof(FortuneFlowController));
        _committedMagicalEnergyForDuel = 0;
        _spiritReadingStartedThisRound = false;
        _drawCardsAllowed = true;
        _verdictProseInFlight = false;
        if (_verdictProseCoroutine != null)
        {
            StopCoroutine(_verdictProseCoroutine);
            _verdictProseCoroutine = null;
        }
        cardPull?.HideCards();
        spiritReader?.CancelActiveReading();
        ResetMagicalEnergySliderForNewReading();
        RefreshDrawCardsButtonInteractable();
        RefreshRenderVerdictButtonInteractable();
    }

    void RefundCommittedMagicalEnergyIfAny()
    {
        if (_committedMagicalEnergyForDuel <= 0)
            return;
        gameManager?.RefundMagicalEnergy(_committedMagicalEnergyForDuel);
        _committedMagicalEnergyForDuel = 0;
    }

    void RefreshDrawCardsButtonInteractable()
    {
        if (drawCardsButton == null)
            return;
        drawCardsButton.interactable = _drawCardsAllowed;
    }

    void RefreshRenderVerdictButtonInteractable()
    {
        if (renderVerdictButton != null)
        {
            if (_verdictAwaitingAccept || _verdictProseInFlight)
                renderVerdictButton.interactable = false;
            else
            {
                bool ready = _cardsDrawn
                    && !string.IsNullOrEmpty(_playerFortuneForJudge)
                    && !string.IsNullOrEmpty(GetSpiritTextForScoring());
                renderVerdictButton.interactable = ready;
            }
        }

        RefreshAcceptJudgementButtonInteractable();
    }

    void RefreshAcceptJudgementButtonInteractable()
    {
        if (acceptJudgementButton == null)
            return;
        bool canAccept = _verdictAwaitingAccept && (gameManager == null || !gameManager.GameComplete);
        acceptJudgementButton.interactable = canAccept;
    }

    string BuildJudgeVerdictBlock(string player, string spiritBody, string winnerLabel, string explanation, bool playerWon)
    {
        if (outputStrings == null)
            return JoinNonEmpty("\n", winnerLabel, explanation);

        if (!string.IsNullOrEmpty(outputStrings.judgeVerdictCompleteFormat))
            return SafeFormat4(outputStrings.judgeVerdictCompleteFormat, player, spiritBody, winnerLabel, explanation);

        string simple = playerWon
            ? (string.IsNullOrEmpty(outputStrings.judgePlayerWinVerdictFormat)
                ? null
                : SafeFormat(outputStrings.judgePlayerWinVerdictFormat, explanation))
            : (string.IsNullOrEmpty(outputStrings.judgeSpiritWinVerdictFormat)
                ? null
                : SafeFormat(outputStrings.judgeSpiritWinVerdictFormat, explanation));

        if (!string.IsNullOrEmpty(simple))
            return simple;

        return JoinNonEmpty("\n", winnerLabel, explanation);
    }

    static string JoinNonEmpty(string sep, params string[] parts)
    {
        if (parts == null || parts.Length == 0)
            return "";
        var list = new List<string>();
        for (int i = 0; i < parts.Length; i++)
        {
            if (!string.IsNullOrEmpty(parts[i]))
                list.Add(parts[i]);
        }
        return list.Count == 0 ? "" : string.Join(sep, list);
    }

    static string SafeFormat(string format, string arg0)
    {
        try
        {
            return string.Format(format, arg0);
        }
        catch (System.FormatException)
        {
            Debug.LogWarning($"[FortuneFlowController] String.Format failed for format string; check braces {{0}}.");
            return format;
        }
    }

    static string SafeFormat4(string format, string a0, string a1, string a2, string a3)
    {
        try
        {
            return string.Format(format, a0, a1, a2, a3);
        }
        catch (System.FormatException)
        {
            Debug.LogWarning($"[FortuneFlowController] String.Format failed for verdict format; need {{0}}{{1}}{{2}}{{3}} for player, spirit, winner label, explanation.");
            return format;
        }
    }

    string GetSpiritTextForScoring()
    {
        string thinking = ResolvedSpiritThinkingMessage();

        if (spiritFortuneOutput != null)
        {
            string s = spiritFortuneOutput.text?.Trim() ?? "";
            if (!string.IsNullOrEmpty(s) && s != thinking)
                return s;
        }

        if (spiritReader != null && spiritReader.DemonOutputText != null)
        {
            string d = spiritReader.DemonOutputText.text?.Trim() ?? "";
            if (!string.IsNullOrEmpty(d) && d != thinking)
                return d;
        }

        return "";
    }

    void LogFlow(string message)
    {
        if (!string.IsNullOrEmpty(message))
            Debug.Log("[FortuneFlow] " + message);
    }

    void ApplyPlayerFortunePlaceholderHint()
    {
        if (playerFortuneInput == null || string.IsNullOrWhiteSpace(playerFortuneClosingPlaceholderHint))
            return;
        if (playerFortuneInput.placeholder is TextMeshProUGUI tmp)
            tmp.text = playerFortuneClosingPlaceholderHint.Trim();
    }

    static void WriteTmp(TMP_Text label, string text, string context)
    {
        if (label == null)
            return;
        if (!label.gameObject.activeSelf)
            label.gameObject.SetActive(true);
        label.text = text;
        label.ForceMeshUpdate(true);
        if (!label.gameObject.activeInHierarchy)
            Debug.LogWarning(
                $"[{context}] TMP \"{label.name}\" is not active in the hierarchy — assign a label under the canvas that matches the active camera, or enable its parents.",
                label);
    }
}

[System.Serializable]
public class FortuneFlowOutputStrings
{
    [Header("Spirit")]
    [TextArea(1, 4)]
    [Tooltip("Written to Spirit output while the LLM runs after Read Fortune. If empty, a default line is used.")]
    public string spiritThinkingMessage;

    [Header("Judge prose (LLM)")]
    [TextArea(1, 4)]
    [Tooltip("Written to Judge output while verdict prose is generated after Make Judgement. If empty, a default line is used.")]
    public string judgeThinkingMessage;

    [Header("Judge mirror (optional)")]
    [TextArea(2, 8)]
    [Tooltip("If set, Read Fortune also updates Judge output via String.Format {{0}} = player text.")]
    public string judgeMirrorPlayerOnReadFormat;
    [TextArea(2, 8)]
    [Tooltip("If set, each spirit reply appends to Judge output via String.Format {{0}} = spirit text.")]
    public string judgeMirrorSpiritOnResponseFormat;

    [Header("Judge verdict (Render Verdict)")]
    [TextArea(2, 6)]
    [Tooltip("Shown as argument {{2}} in JudgeVerdictCompleteFormat, or as a line when using simple verdict formats.")]
    public string winnerPlayerLabel;
    [TextArea(2, 6)]
    [Tooltip("Shown as argument {{2}} in JudgeVerdictCompleteFormat, or as a line when using simple verdict formats.")]
    public string winnerSpiritLabel;
    [TextArea(4, 14)]
    [Tooltip("If set, Render Verdict sets Judge text to String.Format: {{0}} player fortune, {{1}} spirit text, {{2}} winner label, {{3}} one-line explanation.")]
    public string judgeVerdictCompleteFormat;
    [TextArea(2, 8)]
    [Tooltip("If JudgeVerdictCompleteFormat is empty and the player wins, String.Format {{0}} = one-line explanation.")]
    public string judgePlayerWinVerdictFormat;
    [TextArea(2, 8)]
    [Tooltip("If JudgeVerdictCompleteFormat is empty and the spirit wins, String.Format {{0}} = one-line explanation.")]
    public string judgeSpiritWinVerdictFormat;

    [Header("Flow log strings (console only)")]
    [TextArea(1, 3)] public string logDrawing;
    [TextArea(1, 3)] public string logDrawLockedUntilVerdict;
    [TextArea(1, 3)] public string logCardsReady;
    [TextArea(1, 3)] public string logAssignCardPull;
    [TextArea(1, 3)] public string logEnterFortuneFirst;
    [TextArea(1, 3)] public string logAfterReadFortune;
    [TextArea(1, 3)] public string logDrawCardsFirst;
    [TextArea(1, 3)] public string logReadFortuneBeforeSpirit;
    [TextArea(1, 3)] public string logAssignSpiritReader;
    [TextArea(1, 3)] public string logSpiritRequested;
    [TextArea(1, 3)] public string logJudgeNeedDraw;
    [TextArea(1, 3)] public string logJudgeNeedReadFortune;
    [TextArea(1, 3)] public string logJudgeNeedSpirit;
    [TextArea(1, 3)] public string logJudgeSpreadFailed;
    [TextArea(1, 3)] public string logJudgeProseFailed;
    [TextArea(1, 3)] public string logVerdictRendered;
}

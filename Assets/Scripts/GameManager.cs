using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Run resources and post-verdict loop. Wire <see cref="AcceptJudgement"/> to the Accept Judgement button after <see cref="FortuneFlowController.RenderVerdict"/>.
/// Customer count: teller (player) wins the duel → +1 customer; spirit wins → −1 customer (game over at 0).
/// </summary>
public class GameManager : MonoBehaviour
{
    const int CustomerDeltaOnTellerWin = 1;
    const int CustomerDeltaOnSpiritWin = -1;

    [Header("Starting values")]
    [SerializeField] private int startingEnergy = 100;
    [SerializeField] private int startingCustomers = 10;
    [SerializeField] private int maxEnergy = 100;

    [Header("Duel rewards")]
    [Tooltip("If the teller wins and (score margin >= this) OR energy was already max-win, add bonusEnergyOnLargeWin to energy (capped at maxEnergy).")]
    [SerializeField] private int largeWinMarginPoints = 12;
    [SerializeField] private int bonusEnergyOnLargeWin = 5;

    [Header("References")]
    [SerializeField] private FortuneFlowController fortuneFlow;

    [Header("Optional UI")]
    [Tooltip("HUD label for current run energy (0–100), not the duel slider.")]
    [SerializeField] private TMP_Text energyCountText;
    [Tooltip("HUD label for current customer count (e.g. CustomerUpdate TMP).")]
    [SerializeField] private TMP_Text customersCountText;

    [Header("Verdict delta flash (optional)")]
    [Tooltip("Shown for 2s after Accept Verdict with the energy change for that verdict (e.g. +5).")]
    [SerializeField] private TMP_Text energyUpdate;
    [Tooltip("Shown for 2s after Accept Verdict with the customer change for that verdict (e.g. +1, -1).")]
    [SerializeField] private TMP_Text customerUpdate;
    [SerializeField] private float verdictDeltaFlashSeconds = 2f;

    int _energy;
    int _customers;
    bool _cardsDrawn;
    bool _gameComplete;
    Coroutine _verdictDeltaFlashRoutine;

    public int Energy => _energy;
    public int Customers => _customers;
    public bool CardsDrawn => _cardsDrawn;
    public bool GameComplete => _gameComplete;

    public UnityEvent<int> onEnergyChanged;
    public UnityEvent<int> onCustomersChanged;
    public UnityEvent<bool> onGameCompleteChanged;

    void Awake()
    {
        ApplyStartingResources();
        if (energyUpdate != null)
            energyUpdate.enabled = false;
        if (customerUpdate != null)
            customerUpdate.enabled = false;
    }

    void OnDestroy()
    {
        if (_verdictDeltaFlashRoutine != null)
            StopCoroutine(_verdictDeltaFlashRoutine);
    }

    void ApplyStartingResources()
    {
        _energy = Mathf.Clamp(startingEnergy, 0, maxEnergy);
        _customers = Mathf.Max(0, startingCustomers);
        _cardsDrawn = false;
        _gameComplete = false;
        RaiseResources();
    }

    /// <summary>Wired from UI: apply verdict consequences and start the next customer round. Teller wins → +1 customer; spirit wins → −1 customer.</summary>
    public void AcceptJudgement()
    {
        if (_gameComplete)
        {
            Debug.Log("[GameManager] Game already complete — cannot accept verdict.");
            return;
        }

        if (fortuneFlow == null)
        {
            Debug.LogWarning("[GameManager] Assign FortuneFlowController.");
            return;
        }

        if (!fortuneFlow.TryGetLastVerdict(out bool playerWon, out int margin, out bool guaranteedWin))
        {
            Debug.Log("[GameManager] No verdict to accept — render judgement first.");
            return;
        }

        int energyBefore = _energy;
        int customersBefore = _customers;

        if (playerWon)
        {
            // Teller beat the spirit in the rubric duel — attract another customer.
            _customers = Mathf.Max(0, _customers + CustomerDeltaOnTellerWin);
            bool largeWin = guaranteedWin || margin >= largeWinMarginPoints;
            if (largeWin)
                _energy = Mathf.Clamp(_energy + bonusEnergyOnLargeWin, 0, maxEnergy);
        }
        else
        {
            // Spirit won — lose a customer.
            _customers = Mathf.Max(0, _customers + CustomerDeltaOnSpiritWin);
            if (_customers <= 0)
            {
                _customers = 0;
                SetGameComplete(true);
            }
        }

        int energyDelta = _energy - energyBefore;
        int customerDelta = _customers - customersBefore;

        RaiseResources();
        ShowVerdictDeltaFlash(energyDelta, customerDelta);
        fortuneFlow.ResetRoundAfterVerdictAccepted();
        _cardsDrawn = false;
        Debug.Log($"[GameManager] Verdict accepted. Energy={_energy} Customers={_customers} GameComplete={_gameComplete}");
    }

    public void SetCardsDrawn(bool value)
    {
        _cardsDrawn = value;
    }

    /// <summary>Spend up to <paramref name="amount"/> from run energy (clamped by current energy). Returns how much was actually deducted.</summary>
    public int TrySpendMagicalEnergy(int amount)
    {
        amount = Mathf.Max(0, amount);
        int spend = Mathf.Min(amount, _energy);
        if (spend <= 0)
            return 0;
        _energy -= spend;
        RaiseResources();
        return spend;
    }

    /// <summary>Restore run energy (e.g. when abandoning a round after Read Fortune re-commit or new draw).</summary>
    public void RefundMagicalEnergy(int amount)
    {
        amount = Mathf.Max(0, amount);
        if (amount <= 0)
            return;
        _energy = Mathf.Clamp(_energy + amount, 0, maxEnergy);
        RaiseResources();
    }

    void RaiseResources()
    {
        onEnergyChanged?.Invoke(_energy);
        onCustomersChanged?.Invoke(_customers);
        if (energyCountText != null)
            energyCountText.text = _energy.ToString();
        if (customersCountText != null)
            customersCountText.text = _customers.ToString();
    }

    void SetGameComplete(bool value)
    {
        if (_gameComplete == value)
            return;
        _gameComplete = value;
        onGameCompleteChanged?.Invoke(_gameComplete);
    }

    void ShowVerdictDeltaFlash(int energyDelta, int customerDelta)
    {
        if (energyUpdate == null && customerUpdate == null)
            return;
        if (_verdictDeltaFlashRoutine != null)
            StopCoroutine(_verdictDeltaFlashRoutine);
        _verdictDeltaFlashRoutine = StartCoroutine(VerdictDeltaFlashCo(energyDelta, customerDelta));
    }

    IEnumerator VerdictDeltaFlashCo(int energyDelta, int customerDelta)
    {
        string e = FormatSignedDelta(energyDelta);
        string c = FormatSignedDelta(customerDelta);

        if (energyUpdate != null)
        {
            energyUpdate.text = e;
            energyUpdate.enabled = true;
        }

        if (customerUpdate != null)
        {
            customerUpdate.text = c;
            customerUpdate.enabled = true;
        }

        yield return new WaitForSeconds(Mathf.Max(0.05f, verdictDeltaFlashSeconds));

        if (energyUpdate != null)
            energyUpdate.enabled = false;
        if (customerUpdate != null)
            customerUpdate.enabled = false;

        _verdictDeltaFlashRoutine = null;
    }

    static string FormatSignedDelta(int delta)
    {
        if (delta > 0)
            return "+" + delta;
        return delta.ToString();
    }

#if UNITY_EDITOR
    [ContextMenu("Debug / Reset run to starting resources")]
    void EditorResetRun()
    {
        SetGameComplete(false);
        ApplyStartingResources();
        fortuneFlow?.ResetRoundAfterVerdictAccepted();
    }
#endif
}

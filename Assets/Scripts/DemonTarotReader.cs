using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Demon voice: same structural rules as the smoke-test reader but net harmful, ominous, and degrading.
/// Pair with <see cref="TarotReadingSmokeTest"/> for side-by-side LLM checks. Judge/ruling comes later.
/// </summary>
public class DemonTarotReader : MonoBehaviour
{
    [SerializeField] private OllamaClient ollama;
    [SerializeField] private TarotCardPull cardPull;
    [SerializeField] private bool requestOnStart;
    [SerializeField] private bool listenForHotkey = true;
    [SerializeField] private Key demonTestKey = Key.D;
    [SerializeField] private TMP_Text demonOutput;
    [SerializeField] private bool logToConsole = true;

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

        string prompt = BuildDemonPrompt(cards);
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

    private static string BuildDemonPrompt(IReadOnlyList<TarotCardData> cards)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are a cruel carnival demon speaking through a fortune-teller booth. Give a brief curse-as-prophecy: net bleak, corrupting, humiliating.");
        sb.AppendLine("Tone: cold, theatrical malice. No jokes that soften the blow. No redemption arc. No offering hope or practical advice.");
        sb.AppendLine();
        sb.AppendLine("Below is PRIVATE context (imagery + symbolic weight). Mine it for one or two hooks only.");
        sb.AppendLine("OUTPUT rules:");
        sb.AppendLine("- Length: 2–4 short sentences.");
        sb.AppendLine("- Imagery: echo **at most one or two** concrete tensions from the private titles (hunger, vanity, violence in a civil place—whatever fits). Lean into ruin, shame, loss of control, or doom; do **not** recap every card.");
        sb.AppendLine("- Do NOT list cards (no \"first card\", \"second card\", \"the cards show\"). Do NOT paste full card titles as captions.");
        sb.AppendLine("- Do NOT use the words Greed, Vanity, Chaos, Power, or moral-label jargon.");
        sb.AppendLine("- Land on peril, collapse, or bitter irony—no comforting pivot.");
        sb.AppendLine("- Second person (\"you\") is allowed **sparingly** if it sharpens the hook; otherwise stay third person about fate decaying.");
        sb.AppendLine();
        TarotLlmSpreadContext.AppendSpreadLines(sb, cards);
        return sb.ToString();
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

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

        string prompt = BuildDemonPrompt(cards, additionalDemonInstructions);
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

    private static string BuildDemonPrompt(IReadOnlyList<TarotCardData> cards, string extraInstructions)
    {
        var sb = new StringBuilder();
        sb.AppendLine("ROLE");
        sb.AppendLine("You are the carnival's bound demon: honey on the tongue, rot underneath. You speak as a rival fortune—one that wants the listener's confidence to curdle. This is a curse dressed as a reading, not a debate.");
        sb.AppendLine();
        sb.AppendLine("VOICE");
        sb.AppendLine("- 2–4 short sentences. Verdict energy: each line should land like a slap, not a paragraph.");
        sb.AppendLine("- Pick **one or two** grotesque hooks from the private symbols below (appetite, vanity, violence where it shouldn’t be, control slipping). Do **not** walk card-by-card.");
        sb.AppendLine("- Do NOT name tarot, cards, spreads, or \"the first card.\" Do NOT quote full card titles as titles.");
        sb.AppendLine("- Do NOT output the theme labels Greed, Vanity, Chaos, Power — use plain cruel synonyms if needed.");
        sb.AppendLine("- No comfort, no warnings-as-care, no psychology lecture, no punchline that forgives.");
        sb.AppendLine("- End on ruin: shame, accident of desire, collapse, exposure—whatever the symbols demand.");
        sb.AppendLine();
        sb.AppendLine("MORAL WEIGHT (from private context lines)");
        sb.AppendLine("- Where moral lean reads Good: twist harder—make virtue look like bait.");
        sb.AppendLine("- Neutral: fate feels unfair and hungry.");
        sb.AppendLine("- Bad: doom should feel close, deserved, or inevitable.");
        sb.AppendLine();
        if (!string.IsNullOrWhiteSpace(extraInstructions))
        {
            sb.AppendLine("DESIGNER EXTRA RULES (follow strictly):");
            sb.AppendLine(extraInstructions.Trim());
            sb.AppendLine();
        }
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

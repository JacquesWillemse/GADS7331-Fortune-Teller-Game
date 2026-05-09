using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using TMPro;

/// <summary>
/// Dev smoke test: calls Ollama with card metadata and asks for the tuned positive carnival reading.
/// Spread data comes from <see cref="TarotCardPull"/> via <see cref="TarotPullSpreadBuilder"/>.
/// </summary>
public class TarotReadingSmokeTest : MonoBehaviour
{
    [SerializeField] private OllamaClient ollama;
    [SerializeField] private TarotCardPull cardPull;
    [FormerlySerializedAs("interpretOnStart")]
    [SerializeField] private bool requestOnStart;
    [FormerlySerializedAs("listenForInterpretHotkey")]
    [SerializeField] private bool listenForHotkey = true;
    [FormerlySerializedAs("interpretTestKey")]
    [SerializeField] private Key smokeTestKey = Key.I;
    [SerializeField] private TMP_Text interpretationOutput;
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
        if (!Keyboard.current[smokeTestKey].wasPressedThisFrame)
            return;
        RequestFromPull();
    }

    /// <summary>
    /// Builds the spread from <see cref="TarotCardPull"/> slot arrays (see <see cref="TarotPullSpreadBuilder"/>).
    /// </summary>
    public void RequestFromPull()
    {
        if (cardPull == null)
        {
            LogLine("TarotReadingSmokeTest: assign TarotCardPull reference.");
            return;
        }
        if (ollama == null)
        {
            LogLine("TarotReadingSmokeTest: assign OllamaClient reference.");
            return;
        }
        if (!TarotPullSpreadBuilder.TryBuildSpreadForLlm(_spreadBuffer, cardPull))
        {
            LogLine("TarotReadingSmokeTest: could not build spread (ensure TarotCardPull ran and description texts are set).");
            return;
        }

        RequestReading(_spreadBuffer);
    }

    /// <summary>
    /// Positive smoke-test reading for an arbitrary spread list.
    /// </summary>
    public void RequestReading(IReadOnlyList<TarotCardData> cards)
    {
        if (_requestInFlight)
        {
            LogLine("TarotReadingSmokeTest: request already in flight.");
            return;
        }
        if (ollama == null || cards == null || cards.Count == 0)
            return;

        string prompt = BuildPositivePrompt(cards);
        _requestInFlight = true;
        SetOutput("(Requesting smoke-test reading…)");

        ollama.Generate(
            prompt,
            text =>
            {
                _requestInFlight = false;
                SetOutput(text);
                if (logToConsole)
                    Debug.Log("[Tarot smoke]\n" + text);
            },
            err =>
            {
                _requestInFlight = false;
                SetOutput("Error: " + err);
                Debug.LogWarning("[Tarot smoke] " + err);
            });
    }

    private static string BuildPositivePrompt(IReadOnlyList<TarotCardData> cards)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Write a brief carnival-style fortune: net uplifting and forward-looking.");
        sb.AppendLine("Tone: plain, tight, theatrical where useful. No sarcasm.");
        sb.AppendLine();
        sb.AppendLine("Below is PRIVATE context (imagery + symbolic weight). Mine it for one or two hooks only.");
        sb.AppendLine("OUTPUT rules:");
        sb.AppendLine("- Length: 2–4 short sentences.");
        sb.AppendLine("- Imagery: echo **at most one or two** concrete tensions from the private titles (a leap toward appetite, a fragile crown of pride, a brawl in a quiet hall—whatever fits). One phrase may be sharp or ominous if it matches the image; do **not** recap every card.");
        sb.AppendLine("- Do NOT list cards (no \"first card\", \"second card\", \"the cards show\"). Do NOT paste full card titles as captions.");
        sb.AppendLine("- Do NOT use the words Greed, Vanity, Chaos, Power, or moral-label jargon.");
        sb.AppendLine("- After any bite of peril or appetite, land the closing lines on steadiness, choice, or hope so the overall reading stays net positive.");
        sb.AppendLine("- Second person (\"you\") is allowed **sparingly** if it fits a single vivid hook; otherwise stay third person or impersonal.");
        sb.AppendLine();
        TarotLlmSpreadContext.AppendSpreadLines(sb, cards);
        return sb.ToString();
    }

    private void SetOutput(string text)
    {
        if (interpretationOutput != null)
            interpretationOutput.text = text;
    }

    private void LogLine(string msg)
    {
        if (logToConsole)
            Debug.Log(msg);
    }
}

using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;

/// <summary>
/// Stage 1: sends pulled card metadata to Ollama and asks for a short positive interpretation.
/// Hook to UI or enable interpretOnStart / test key for manual testing.
/// </summary>
public class TarotCardInterpreter : MonoBehaviour
{
    [SerializeField] private OllamaClient ollama;
    [SerializeField] private TarotCards tarotDatabase;
    [SerializeField] private int drawCount = 3;
    [SerializeField] private bool interpretOnStart;
    [SerializeField] private KeyCode interpretTestKey = KeyCode.I;
    [SerializeField] private TMP_Text interpretationOutput;
    [SerializeField] private bool logToConsole = true;

    private bool _requestInFlight;

    private void Start()
    {
        if (interpretOnStart)
            RequestInterpretationFromDatabase();
    }

    private void Update()
    {
        if (interpretTestKey == KeyCode.None)
            return;
        if (!Input.GetKeyDown(interpretTestKey))
            return;
        RequestInterpretationFromDatabase();
    }

    /// <summary>
    /// Uses the first <see cref="drawCount"/> cards from the assigned database (same order as current UI test).
    /// </summary>
    public void RequestInterpretationFromDatabase()
    {
        if (tarotDatabase == null || tarotDatabase.cards == null || tarotDatabase.cards.Count == 0)
        {
            LogLine("TarotCardInterpreter: assign tarotDatabase with cards.");
            return;
        }
        if (ollama == null)
        {
            LogLine("TarotCardInterpreter: assign OllamaClient reference.");
            return;
        }

        int n = Mathf.Min(drawCount, tarotDatabase.cards.Count);
        var slice = new List<TarotCardData>(n);
        for (int i = 0; i < n; i++)
            slice.Add(tarotDatabase.cards[i]);

        RequestInterpretation(slice);
    }

    /// <summary>
    /// Interprets an arbitrary list of cards (for later integration with real draws).
    /// </summary>
    public void RequestInterpretation(IReadOnlyList<TarotCardData> cards)
    {
        if (_requestInFlight)
        {
            LogLine("TarotCardInterpreter: request already in flight.");
            return;
        }
        if (ollama == null || cards == null || cards.Count == 0)
            return;

        string prompt = BuildInterpretPrompt(cards);
        _requestInFlight = true;
        SetOutput("(Requesting interpretation…)");

        ollama.Generate(
            prompt,
            text =>
            {
                _requestInFlight = false;
                SetOutput(text);
                if (logToConsole)
                    Debug.Log("[Tarot LLM]\n" + text);
            },
            err =>
            {
                _requestInFlight = false;
                SetOutput("Error: " + err);
                Debug.LogWarning("[Tarot LLM] " + err);
            });
    }

    private static string BuildInterpretPrompt(IReadOnlyList<TarotCardData> cards)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are a carnival fortune teller in a light, theatrical tone.");
        sb.AppendLine("The player reads tarot-style cards for a customer. Each card has a title, a symbolic theme (Greed, Vanity, Chaos, Power), and a moral lean (Good, Neutral, Bad) describing how ominous or hopeful the card is.");
        sb.AppendLine("Write ONE short positive reading (3–5 sentences) that weaves the cards together and offers hope. Do not mention \"AI\" or rules. Stay in character.");
        sb.AppendLine();
        sb.AppendLine("Cards:");
        for (int i = 0; i < cards.Count; i++)
        {
            TarotCardData c = cards[i];
            sb.Append(i + 1).Append(". ").Append(c.cardName?.Trim() ?? "?");
            sb.Append(" | Theme: ").Append(string.IsNullOrEmpty(c.cardTheme) ? "?" : c.cardTheme);
            sb.Append(" | Moral lean: ").Append(c.cardMoral);
            sb.AppendLine();
        }
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

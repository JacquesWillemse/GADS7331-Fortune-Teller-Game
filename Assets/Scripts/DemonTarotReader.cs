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
        sb.AppendLine("You are the carnival's bound demon. You speak curses, not captions. Your job is to wound with prophecy—cold, cruel, final—not to retell the joke on each card or wink at its props.");
        sb.AppendLine();
        sb.AppendLine("OUTPUT FORMAT");
        sb.AppendLine("- Write exactly FOUR sentences total.");
        sb.AppendLine("- Sentences 1–3: one short sentence per private spread line below (cover all three lines; order may change for flow).");
        sb.AppendLine("- For each line, use ONLY its Theme and Moral lean as fuel for the curse. Treat the title as a private note about mood—do NOT restage its scenery, objects, or punchline setup in recognizable form.");
        sb.AppendLine("- Sentence 4: one final sentence only — a short, decisive overall curse. Last line of the reply. About 12–22 words, one clean doom, no punchline comedy.");
        sb.AppendLine("- Never omit sentence 4.");
        sb.AppendLine();
        sb.AppendLine("STYLE (sentences 1–3)");
        sb.AppendLine("- Ominous, brutal, prophetic. Invent fresh grotesque or social images (rot, collapse, exposure, betrayal, hunger, humiliation, cold dominion).");
        sb.AppendLine("- No witty recap of what the title literally depicts. If a sentence could be guessed by reading only the card caption, rewrite it.");
        sb.AppendLine("- No comfort, no remedy, no hopeful pivot.");
        sb.AppendLine();
        sb.AppendLine("STYLE (sentence 4)");
        sb.AppendLine("- Hammer-blow: one irreversible bad fate. Weave at least two dark threads from the spread (use everyday words, not tarot jargon).");
        sb.AppendLine();
        sb.AppendLine("STRICT — NO LITERAL JOKE RESTAGING (sentences 1–3)");
        sb.AppendLine("- Do not reuse or obvious-synonym the concrete subjects from the title lines (e.g. remotes, TVs, family power struggles, haircuts/mohawks, wrestling, restaurants, chefs, plates, animals, food counts, kitchens, weather wishes, retirement, etc.).");
        sb.AppendLine("- If any word or scene from a title would be recognizable to a player who just read that caption, delete it and replace with unrelated invented imagery that still matches the line's Theme + Moral lean.");
        sb.AppendLine();
        sb.AppendLine("RULES");
        sb.AppendLine("- Do not copy or paste title text from the private lines.");
        sb.AppendLine("- Do not say \"the cards\", \"the spread\", or \"first/second/third card\".");
        sb.AppendLine("- Do not output the words Greed, Vanity, Chaos, or Power.");
        sb.AppendLine();
        sb.AppendLine("GENDER — NEUTRAL (all four sentences)");
        sb.AppendLine("- The querent's gender is unknown. Card titles may mention a gendered figure for that card only — do NOT project that onto the person receiving the reading.");
        sb.AppendLine("- Do not use she/her/he/him/his, or gendered nouns (woman, man, lady, girl, boy) for the listener or fate's target.");
        sb.AppendLine("- Prefer: they/them, one, one's, you/your (second person is fine), or impersonal phrasing (the flesh, the bones, the crown, a hand, the seeker).");
        sb.AppendLine();
        sb.AppendLine("MORAL WEIGHT (from each line's moral lean)");
        sb.AppendLine("- Good: twist hope into bait.");
        sb.AppendLine("- Neutral: fate feels unfair and hungry.");
        sb.AppendLine("- Bad: harm feels close and deserved.");
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

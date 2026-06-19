using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Optional two-pass demon reading: JSON outline → prose curse. Falls back to single <see cref="DemonTarotPrompts.BuildReadingPrompt"/> if outline parse fails.
/// </summary>
public static class DemonTarotTwoPass
{
    public static IEnumerator CoGenerate(
        OllamaClient ollama,
        IReadOnlyList<TarotCardData> cards,
        bool twoPass,
        string additionalDemonInstructions,
        Action<string> onSuccess,
        Action<string> onError,
        string fortuneTellerReadingForBrevity = null,
        FortuneClientSpawner.WealthType clientWealth = FortuneClientSpawner.WealthType.Poor,
        RunExperienceConfig.SpiritCardKnowledge spiritKnowledge = RunExperienceConfig.SpiritCardKnowledge.Full)
    {
        if (ollama == null)
        {
            onError?.Invoke("OllamaClient is null.");
            yield break;
        }

        if (!twoPass || cards == null || cards.Count == 0)
        {
            yield return ollama.StartCoroutine(ollama.GenerateWait(
                DemonTarotPrompts.BuildReadingPrompt(cards, additionalDemonInstructions, fortuneTellerReadingForBrevity, clientWealth, spiritKnowledge),
                onSuccess,
                onError));
            yield break;
        }

        string outlineRaw = null;
        string err1 = null;
        yield return ollama.StartCoroutine(ollama.GenerateWait(
            DemonTarotPrompts.BuildReadingOutlinePrompt(cards, clientWealth, spiritKnowledge),
            s => outlineRaw = s,
            e => err1 = e));

        if (!string.IsNullOrEmpty(err1))
        {
            onError?.Invoke(err1);
            yield break;
        }

        DemonOutlineRoot parsed = null;
        if (DemonReadingOutlineParser.TryParse(outlineRaw, cards, out parsed, out string parseFail))
        {
            Debug.Log("[DemonTwoPass] Outline OK; running pass-2 prose.");
        }
        else if (DemonReadingOutlineParser.TrySynthesizeFromSpread(cards, out parsed))
        {
            Debug.LogWarning(
                "[DemonTwoPass] Model outline could not be parsed (" + parseFail + "); using spread-synthesized outline for pass 2. Raw (trimmed): " +
                TrimForLog(outlineRaw));
        }
        else
        {
            Debug.LogWarning(
                "[DemonTwoPass] Outline parse failed (" + parseFail + "); falling back to single-pass demon prompt. Raw (trimmed): " +
                TrimForLog(outlineRaw));
            yield return ollama.StartCoroutine(ollama.GenerateWait(
                DemonTarotPrompts.BuildReadingPrompt(cards, additionalDemonInstructions, fortuneTellerReadingForBrevity, clientWealth, spiritKnowledge),
                onSuccess,
                onError));
            yield break;
        }

        string outlineJson = JsonUtility.ToJson(parsed);
        string prosePrompt = DemonTarotPrompts.BuildReadingSecondPassProsePrompt(
            cards, outlineJson, additionalDemonInstructions, fortuneTellerReadingForBrevity, clientWealth, spiritKnowledge);
        yield return ollama.StartCoroutine(ollama.GenerateWait(prosePrompt, onSuccess, onError));
    }

    static string TrimForLog(string raw, int max = 400)
    {
        if (string.IsNullOrEmpty(raw))
            return "(empty)";
        string t = raw.Replace("\r\n", " ").Replace("\n", " ").Trim();
        return t.Length <= max ? t : t.Substring(0, max) + "…";
    }
}

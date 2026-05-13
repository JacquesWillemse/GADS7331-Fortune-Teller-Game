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
        string fortuneTellerReadingForBrevity = null)
    {
        if (ollama == null)
        {
            onError?.Invoke("OllamaClient is null.");
            yield break;
        }

        if (!twoPass || cards == null || cards.Count == 0)
        {
            yield return ollama.StartCoroutine(ollama.GenerateWait(
                DemonTarotPrompts.BuildReadingPrompt(cards, additionalDemonInstructions, fortuneTellerReadingForBrevity),
                onSuccess,
                onError));
            yield break;
        }

        string outlineRaw = null;
        string err1 = null;
        yield return ollama.StartCoroutine(ollama.GenerateWait(
            DemonTarotPrompts.BuildReadingOutlinePrompt(cards),
            s => outlineRaw = s,
            e => err1 = e));

        if (!string.IsNullOrEmpty(err1))
        {
            onError?.Invoke(err1);
            yield break;
        }

        if (!DemonReadingOutlineParser.TryParse(outlineRaw, out DemonOutlineRoot parsed))
        {
            Debug.LogWarning("[DemonTwoPass] Outline parse failed; falling back to single-pass demon prompt.");
            yield return ollama.StartCoroutine(ollama.GenerateWait(
                DemonTarotPrompts.BuildReadingPrompt(cards, additionalDemonInstructions, fortuneTellerReadingForBrevity),
                onSuccess,
                onError));
            yield break;
        }

        Debug.Log("[DemonTwoPass] Outline OK; running pass-2 prose.");

        string outlineJson = JsonUtility.ToJson(parsed);
        string prosePrompt = DemonTarotPrompts.BuildReadingSecondPassProsePrompt(
            cards, outlineJson, additionalDemonInstructions, fortuneTellerReadingForBrevity);
        yield return ollama.StartCoroutine(ollama.GenerateWait(prosePrompt, onSuccess, onError));
    }
}

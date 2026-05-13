# GADS7331 - Fortune Teller Game

## Project Overview
This Unity game is a narrative strategy experience where the player is a 10th generation carnival fortune teller bound by ancestral pacts. During each reading, the player gives a positive interpretation of drawn tarot cards while a demon AI attempts a negative interpretation to claim customer souls. A second AI judge decides the winner and drives consequences in the wider carnival simulation.

## Core Features
- Tarot reading duel: player vs demon AI
- Judge AI outcome arbitration
- Hidden card themes:
  - Greed
  - Vanity
  - Chaos
  - Power
- Carnival management layer (resources, reputation, time)
- Demon dance battle mini-game

## Current Prototype Status
- Card data stored in ScriptableObject assets.
- Scene currently supports a hardcoded 3-card draw for UI hookup testing.
- Deck will expand beyond 3 cards as development continues.
- **Stage 1 LLM:** `OllamaClient` + `TarotReadingSmokeTest` (positive prompt dev harness) and `DemonTarotReader` (negative demon prompt).
- **Demon reading (optional two-pass):** `DemonTarotTwoPass` coordinates pass 1 (JSON outline via `DemonTarotPrompts.BuildReadingOutlinePrompt`) and pass 2 (prose via `BuildReadingSecondPassProsePrompt`). Parsed with `DemonReadingOutlineParser`; invalid outline → automatic **single-pass** fallback (`BuildReadingPrompt`). Toggle on **`DemonTarotReader`** (**Use Two Pass Reading**) and on **`TarotReadingDuelPipeline`** (**Use Two Pass Demon Reading**). Console: `[DemonTwoPass] Outline OK…` vs outline parse fallback warning.
- **Duel pipeline:** `TarotReadingDuelPipeline` — player text (`TMP_InputField`) → optional demon **gate** (JSON) → if not agreed, demon **reading** (shared `DemonTarotPrompts`, optionally two-pass above) → **verdict** via deterministic **`FortuneDuelRubric`** (no judge LLM in this script; `onJudgeComplete` fires with winner + one-line explanation after scoring; omitted when the demon **agrees at the gate** and the run ends early). Console `[Duel][Judge]` matches that step. Optional hotkey **J** or UI button calling `RunDuel()`. `onGateAgreedWithPlayer(bool)` fires after gate; `onDemonReadingComplete` after demon text when a counter-reading ran.

## Ollama (local LLM) — test setup
1. Install [Ollama](https://ollama.com) and run `ollama serve` (default `http://127.0.0.1:11434`).
2. Pull a model, e.g. `ollama pull llama3.2` (or set `OllamaClient`’s model name to match whatever you installed).
3. Add `OllamaClient`. Optionally add `TarotReadingSmokeTest` and/or `DemonTarotReader` (same GameObject or separate).
4. Assign **`TarotCardPull`** on both `TarotReadingSmokeTest` and `DemonTarotReader`. Link **Ollama** to `OllamaClient`; assign `TMP_Text` if desired. **`TarotPullSpreadBuilder`** matches themes by **card title** to `tarotDatabase` entries (works with random pulls). No edits required on `TarotCardPull` itself.
5. Play Mode: enable **Listen For Hotkey** on Smoke Test / Demon Reader if you want **I** / **D** (off by default so typing in `TMP_InputField` does not fire them). Hotkeys are ignored while a `TMP_InputField` is focused. Or use **Request On Start** on either component.
6. **Duel:** Add `TarotReadingDuelPipeline`, assign `OllamaClient`, `TarotCardPull`, and a **TMP_InputField** for the player’s reading. Hook a UI **Button** to `RunDuel()` or enable **Listen For Hotkey** (**J**). Optional: `statusText`, `gateReasonText`, `demonReadingText`, `judgeResultText`. If **Demon Reading Text** is empty, assign **Demon Reading Output Fallback** to your **`DemonTarotReader`** component so duel demon prose goes to the same label as solo **D**. With **Log Gate And Judge To Console**, the full demon body also prints as `[Duel][Demon]`. UnityEvents: `onGateAgreedWithPlayer`, `onDemonReadingComplete`, `onJudgeComplete`.

### Wiring `TarotReadingDuelPipeline` on **AIManager** (typical setup)
1. Select **AIManager** → **Add Component** → **Tarot Reading Duel Pipeline**.
2. **Ollama:** drag the same **Ollama Client** on this object (or the reference you already use).
3. **Card Pull:** drag **GameManager**’s **Tarot Card Pull** (same reference as Smoke Test / Demon Reader).
4. **Player Reading Input:** drag the **Player Input** object’s **TMP_InputField** component (the text field under *Player Input Background*).
5. **Make Reading button:** in the **Button** component → **On Click ()** → add a slot → drag **AIManager** → choose **TarotReadingDuelPipeline → RunDuel** (no argument). Leave **Listen For Hotkey** off if you only use the button.
6. **Console:** leave **Log Gate And Judge To Console** checked (default). You will see `[Duel][Gate] …` after every gate, an extra line when the demon **accepts** and skips the judge, and `[Duel][Judge] winner=…` after a full duel. Turn on **Log Status To Console** if you also want step spam (`Gate…`, `Demon…`, etc.).
7. **Smoke Test / Demon Reader:** keep them for **I** / **D** quick tests; the duel pipeline does **not** replace them—it runs the full chain when you click **Make Reading**.

## Planned AI Architecture (Ollama)
- Local LLM runtime via Ollama
- Three AI roles:
  - Player-side positive interpretation assistant (optional support mode)
  - Demon negative interpretation agent
  - Ancient-object judge agent
- Rule: if player interpretation is already negative, demon agrees and judge phase is skipped.

## Gameplay Loop
1. Customer enters tent
2. Customer presents problem/personality
3. Tarot cards are drawn
4. Player interprets cards positively
5. Demon AI interprets negatively
6. Judge AI decides strongest interpretation
7. Outcome updates world/resources/reputation
8. Downtime with demon (dialogue/deals)
9. Next customer

## Repository Documentation
- `PLAN.md` — development roadmap and milestones (keep **Current State** in sync with the repo).
- `RUNNING_LOG.md` — chronological implementation notes (append a dated section per meaningful session).
- `requirements.txt` — feature and technical requirements snapshot (update when behavior or constraints change).
- `PLAYER_DUEL_SCORING_GUIDE.md` — **player-facing**: how the fortune duel is scored (themes, morals, word lists, prediction checklist); matches `FortuneDuelRubric`.

## Getting Started (Unity)
1. Open project in Unity Hub.
2. Load `SampleScene`.
3. Ensure tarot data asset is assigned to the game manager component.
4. Run scene to verify card text/image UI hookups.

## Notes
- This project is under active iteration.
- Documentation files are intended to be updated continuously as milestones are implemented.

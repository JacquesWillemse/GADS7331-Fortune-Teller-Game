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
- **Stage 1 LLM:** `OllamaClient` + `TarotCardInterpreter` call Ollama with the first N cards (name, theme, moral) and return a short positive reading for testing. Demon and judge agents are not wired yet.

## Ollama (local LLM) — test setup
1. Install [Ollama](https://ollama.com) and run `ollama serve` (default `http://127.0.0.1:11434`).
2. Pull a model, e.g. `ollama pull llama3.2` (or set `OllamaClient`’s model name to match whatever you installed).
3. In Unity, add an `OllamaClient` component and a `TarotCardInterpreter` component (e.g. on `GameManager` or an empty test object).
4. Assign the same `TarotCards` asset as `TarotCardInterpreter.tarotDatabase`, link `ollama` to the `OllamaClient`, optionally assign a `TMP_Text` for output.
5. Enter Play Mode and press **I** (default) to request an interpretation, or enable **Interpret On Start** on `TarotCardInterpreter` for an automatic request.

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
- `PLAN.md` - development roadmap and milestones
- `RUNNING_LOG.md` - chronological implementation notes
- `requirements.txt` - feature and system requirements snapshot

## Getting Started (Unity)
1. Open project in Unity Hub.
2. Load `SampleScene`.
3. Ensure tarot data asset is assigned to the game manager component.
4. Run scene to verify card text/image UI hookups.

## Notes
- This project is under active iteration.
- Documentation files are intended to be updated continuously as milestones are implemented.

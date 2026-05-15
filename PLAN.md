# Fortune Teller vs Demon - Development Plan

## Vision
Build a Unity narrative strategy game where the player gives a positive tarot reading, a demon AI gives a negative reading, and a judge AI decides the winning interpretation. Outcomes affect carnival resources and progression.

## Core Pillars
- Fortune teller reading duel (player vs demon AI)
- Carnival management simulation
- Demon dance battle mini-game

## Current State (May 2026)
- **Scenes:** `MainMenu` → `MainScene`; production flow on `FortuneFlowController`.
- Tarot card ScriptableObject data with hidden themes and morals; 3-card draw for UI prototype.
- **Spirit:** starts on **draw complete** via `DemonTarotReader` + optional `DemonTarotTwoPass` (outline → prose).
- **Player fortune:** typed in UI on Read Fortune; magical energy committed from slider.
- **Judge:** `FortuneDuelRubric` (instant) + optional `JudgeVerdictProsePrompts` (Ollama prose + winner line).
- **Dev harnesses:** `TarotReadingSmokeTest`, `TarotReadingDuelPipeline` (gate + spirit + rubric experiments).

## Gameplay Loop (Target)
1. Customer enters tent
2. Customer presents problem/personality
3. Tarot cards are drawn
4. Player interprets cards positively
5. Demon AI interprets cards negatively
6. Judge AI decides strongest interpretation
7. Outcome updates resources/reputation/world state
8. Downtime scene with demon dialogue/deals
9. Next customer

## AI Behavior Rules
- Demon AI default behavior: negative interpretation to sabotage player.
- Exception: if player gives a negative interpretation, demon should agree and skip judge decision.
- Judge AI receives:
  - Drawn cards + hidden themes
  - Player interpretation
  - Demon interpretation
  - Customer context
  Then returns winner + reason.

## Technical Milestones

### Milestone 1 - Card/Data Foundation
- Expand deck support to 30 cards (start campaign with 10 unlocked).
- Add card data fields:
  - title
  - description
  - theme (Greed, Vanity, Chaos, Power)
  - image sprite
  - optional favor bias (player, demon, neutral)
- Remove hardcoded draw count and use configurable value.

### Milestone 2 - LLM Integration (Ollama)
- Build `OllamaClient` service in Unity (initial version done: `/api/generate`).
- `TarotReadingSmokeTest` for positive prompt harness; `DemonTarotReader` for inverted demon prompt (same card slice).
- Demon prompts centralized in `DemonTarotPrompts` (single-pass, gate, outline pass, second-pass prose); optional `DemonTarotTwoPass` orchestration with JSON outline parse + fallback.
- Build prompt templates:
  - `PlayerReaderPrompt`
  - `DemonReaderPrompt` (largely superseded by `DemonTarotPrompts` in prototype)
  - `JudgePrompt`
- Add timeout, retry, and fallback response handling.

### Milestone 3 - Duel Resolution
- Implement interpretation turn system.
- Add demon "agree and skip judge" path.
- Add deterministic scoring schema for judge consistency:
  - theme alignment
  - tone alignment with role
  - relevance to customer problem
  - clarity/persuasiveness

### Milestone 4 - Carnival Management
- Track resources:
  - gullible customers
  - money quota
  - reputation
  - magic energy
  - time
- Apply post-reading effects to resources.

### Milestone 5 - Dance Battle
- Implement DDR-style mini-game.
- Reward outcomes:
  - customer soul saved / lost
  - golden ticket wish granted
  - magic energy refill in demon-only challenges

### Milestone 6 - Balancing and Content
- Author full starter card set.
- Add customer archetypes and influence/richness modifiers.
- Add demon downtime events and deal system.

## Immediate Next Tasks
1. Refactor card model to support full metadata and sprite references cleanly.
2. Build an abstraction layer for local LLM calls (`ILLMService`) before direct scene wiring.
3. Implement first end-to-end vertical slice:
   - one customer
   - card draw
   - player input
   - demon response (single- or two-pass, as toggled)
   - judge result
   - resource update
4. Tune two-pass outline reliability (prompt strictness vs parser relaxation) if playtests show frequent fallback.

## Risks and Mitigations
- Risk: LLM responses vary too much.
  - Mitigation: strict prompt format + lightweight scoring rubric + capped output size.
- Risk: Unity main thread stalls during inference.
  - Mitigation: async requests + loading states + response timeout.
- Risk: narrative tone drift.
  - Mitigation: system prompt constraints and role conditioning per agent.

## Repository documentation

| File | Purpose |
|------|---------|
| `HIGH_CONCEPT.md` | Ideation, LLM role, why local Ollama |
| `ollama-plan.md` | Model, inference timing, data flow, prompts, risks |
| `setup.md` | Unity + Ollama install, scenes, troubleshooting |
| `refinements-changes.md` | Scope changes and decision log |
| `README.md` | Overview, dependencies, credits, AI tools |
| `PLAN.md` | This roadmap |
| `RUNNING_LOG.md` | Session engineering notes |
| `requirements.txt` | Requirements baseline |
| `PLAYER_DUEL_SCORING_GUIDE.md` | Player-facing scoring |
| `prompts-used.md` | Prompt archive (tested prompts, iterations) |

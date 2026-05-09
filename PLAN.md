# Fortune Teller vs Demon - Development Plan

## Vision
Build a Unity narrative strategy game where the player gives a positive tarot reading, a demon AI gives a negative reading, and a judge AI decides the winning interpretation. Outcomes affect carnival resources and progression.

## Core Pillars
- Fortune teller reading duel (player vs demon AI)
- Carnival management simulation
- Demon dance battle mini-game

## Current State (May 2026)
- Unity scene has a working test hookup for drawing and showing 3 cards.
- Tarot card ScriptableObject data exists and includes hidden themes.
- Card count is intentionally hardcoded to 3 for UI testing.
- No Ollama integration yet.
- No judge pipeline yet.

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
- Build prompt templates:
  - `PlayerReaderPrompt`
  - `DemonReaderPrompt`
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
   - demon response
   - judge result
   - resource update

## Risks and Mitigations
- Risk: LLM responses vary too much.
  - Mitigation: strict prompt format + lightweight scoring rubric + capped output size.
- Risk: Unity main thread stalls during inference.
  - Mitigation: async requests + loading states + response timeout.
- Risk: narrative tone drift.
  - Mitigation: system prompt constraints and role conditioning per agent.

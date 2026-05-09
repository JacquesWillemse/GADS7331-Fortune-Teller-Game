# Running Log

## 2026-05-09

### Session Summary
- Reviewed current Unity prototype and design direction.
- Confirmed card draw UI wiring is intentionally hardcoded to 3 cards for testing.
- Confirmed requirement that final game supports a larger deck (target 30, starter 10).
- Captured complete narrative and gameplay loop from design notes.
- Established project documentation baseline.

### Verified Project Facts
- `TarotCards` ScriptableObject model is present.
- `ListOfTarotCards.asset` contains sample card entries and hidden themes.
- `TarotCardPull` currently pulls first 3 cards and assigns text/image UI references.
- Scene has card description and card image references connected.

### Decisions Logged
- Treat 3-card logic as temporary UI test scaffolding.
- Build AI flow around three roles: Player interpretation, Demon interpretation, Judge resolution.
- Enforce demon-agreement shortcut when player response is already negative.
- Maintain documentation artifacts continuously:
  - `PLAN.md`
  - `README.md`
  - `RUNNING_LOG.md`
  - `requirements.txt`

### Next Engineering Actions
1. Refactor card data model for scalable deck and richer metadata.
2. Add Ollama integration service layer.
3. Implement first vertical slice of player vs demon vs judge resolution.
4. Connect outcome resolution to resource/reputation changes.

---

## 2026-05-09 (later)

### Stage 1 — Ollama card interpretation (no demon/judge yet)
- Added `OllamaClient.cs`: POST to `/api/generate`, non-streaming, configurable base URL and model.
- Added `TarotCardInterpreter.cs`: builds prompt from card title, theme, and `TarotMoral`; optional Play Mode trigger (**I**) or interpret-on-start; optional `TMP_Text` output.
- Documented setup steps in `README.md`.

### How to validate
- Ollama running locally with a model matching `OllamaClient.model`.
- Scene: add components + references; press **I** or use interpret-on-start; confirm Console / UI shows a coherent positive reading.

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

### Fix: Input System only project
- Replaced legacy `Input.GetKeyDown` with `UnityEngine.InputSystem.Keyboard` and `Key` binding so Play Mode works when **Active Input Handling** is set to **Input System Package** (or Both).

### Tarot interpretation prompt (Stage 1)
- Tightened positive-only tone (no ominous or negative predictions for this role).
- Customer-facing reply must not name cards, themes, or moral labels; model uses spread only as private context.
- Output tuning (2–4 sentences): allow **one or two** imagery hooks from the spread (no card-by-card recap, no theme keywords); optional sparing “you”; a sharp hook may echo peril, then lines resolve toward net hopeful steadiness.

### LLM spread source → TarotCardPull (no edits to user script)
- `TarotReadingSmokeTest` / `DemonTarotReader` reference **`TarotCardPull`**. **`TarotPullSpreadBuilder`** reads **`cardDescriptions`**, **`cardMorality`**, and caps by **`cardImages`** length; theme text uses **`tarotDatabase.cards[i]`** when parallel to the pull. **`TarotCardPull.cs`** left as the author wrote it.

### Demon prompt iteration
- Rewrote default demon prompt: carnival-bound predator voice, verdict-like lines, moral-lean handling (Good twist harder / Neutral cruel fate / Bad inevitable doom).
- Added Inspector **Additional Demon Instructions** (`DemonTarotReader`) for tuning without code edits.
- Demon prompt: forbid echoing card titles / distinctive phrases from private lines — require fresh metaphors only (stops lines like “20 succulent meals…” mirroring the card text).
- Tightened structure to reduce generic poetic filler: exactly 3 lines (vice -> consequence -> final doom verdict), with explicit ban on soft abstractions and direct card-scene references.
- Added spread-balance constraint: demon output must use at least two symbolic hooks from different cards, ideally split across sentence 1 and sentence 2, then fused in sentence 3.
- Added semantic-anchor rule: each line must stay traceable to card meaning (anchor-level), while still banning lexical/title overlap (word-level).
- Fixed prompt leakage from scaffolding tokens (`A`, `B`, `A+B`, `private symbol`) by removing those labels from instructions and explicitly forbidding meta/planning words in output.
- Added anti-generic patch: each sentence must include a concrete sensory image, and final output now bans rubric words (`vice`, `consequence`, `anchor`, etc.) to avoid checklist-sounding prose.
- Major demon prompt redesign: switched to theme-group sentence generation (shared-theme cards reinforce in one line; unique themes get one line each) plus one final overall negative prediction.
- Demon prompt: mandatory short closing verdict line (12–22 words), must weave ≥2 thematic threads in plain language; never omit; never use DB theme tokens Greed/Vanity/Chaos/Power verbatim.
- Reverted demon **body** to prior 3-sentence curse structure (two hooks + fuse, sensory, strict no-title-echo); kept **sentence 4** as mandatory closing verdict. Removed theme-per-sentence grouping that caused literal card props (cheese, remote, etc.).
- Demon prompt simplified again to match early “3 lines + verdict” intent: **3 sentences** = one per private spread line (any order), classic booth tone; **sentence 4** = short decisive closing curse only. Stripped long fuse/hook/no-echo scaffolding that was steering the model wrong.
- Demon prompt fix: model was restaging card jokes (remote, hair, kitchen). Body lines must curse from **Theme + Moral lean** only; strict ban on recognizable title props/scenes in sentences 1–3; sentence 4 remains overall doom hammer.
- Demon prompt: **gender-neutral** output — no she/her/he/him or gendered nouns for the querent; card gender in titles must not be projected onto the listener (`they/them`, `one`, `you/your`, or impersonal phrasing).

### Duel pipeline (player → gate → demon → judge)
- Added `DemonTarotPrompts` (shared demon reading + gate prompts), `TarotJudgePrompts`, `TarotLlmJsonHelpers` (gate/judge JSON DTOs + extract), `TarotReadingDuelPipeline` (coroutine chain, UnityEvents).
- `OllamaClient.GenerateWait` for sequential LLM steps.
- `DemonTarotReader` now calls `DemonTarotPrompts.BuildReadingPrompt` (single source of truth).
- `TarotReadingDuelPipeline`: optional **Log Status To Console**; default **Log Gate And Judge To Console** logs `[Duel][Gate]` / accept-skip / `[Duel][Judge]` lines.
- Demon **gate** prompt tightened: agree only on net cruel/harmful readings; hopeful reframes (vanity/power named but landing on hope) → demon does **not** agree. **GameplayHotkeyGuard**: no I/D/J while `TMP_InputField` focused; Smoke/Demon **Listen For Hotkey** default **off**.
- Added `DemonGatePositiveHeuristic`: if model sets agree=true but player text hits hope/release/prosper patterns, pipeline **forces disagree** and logs correction. Gate prompt includes few-shot calibration lines.
- Heuristic expanded: `will find you`, `fortune will`, `luck will`; bare `fortune` (not `misfortune`); combo **let go/release + blessing outcome**; handles typos like \"goof fortune will find you\".
- Duel: optional **Demon Reading Output Fallback** → `DemonTarotReader` so demon text shows when duel-only `TMP_Text` unset; console `[Duel][Demon]` log. Judge prompt asks for 4–7 sentence rationale with concrete spread-tied comparison.
- Judge prompt fix: model had called the demon **protective** (role inversion). Prompt now locks FORTUNE TELLER = help only, DEMON = harm only; forbids treating bleakness or \"warnings\" as automatic demon win; rationale sentences 1–2 must start with fixed prefixes.
- Fixed spread-theme mapping for random pulls in `TarotPullSpreadBuilder`: theme now resolves by matching UI title to database card name instead of slot index.

### Rename + demon reader
- Renamed `TarotCardInterpreter` → `TarotReadingSmokeTest` (same script GUID preserved for Unity references). Serialized fields migrated with `FormerlySerializedAs` where renamed.
- Added `DemonTarotReader` (default hotkey **D**): same spread slice and structural rules, inverted to net harmful/doom-laden output.
- Shared spread lines in `TarotLlmSpreadContext` for prompt consistency.

---

## 2026-05-11

### Two-pass demon / spirit reading (outline → prose)
- **Goal:** Stabilize themes, anchors, and character across the five-sentence curse without pasting booth-style lane labels — pass 1 returns compact JSON; pass 2 writes spoken prose bound to that outline.
- **New:** `DemonReadingOutline.cs` — `DemonOutlineRoot` / `DemonLineOutline`; `DemonReadingOutlineParser.TryParse` (strip to first `{`…last `}`, `JsonUtility.FromJson`, validate `theme_family` ∈ greed|vanity|chaos|power, anchors, optional `line` index).
- **New:** `DemonTarotTwoPass.cs` — `CoGenerate`: if two-pass off or empty spread → single `BuildReadingPrompt`; else outline prompt → parse → on failure **fallback** to single pass (warning log); else `JsonUtility.ToJson(parsed)` + `BuildReadingSecondPassProsePrompt` + second generate. Nested waits use `ollama.StartCoroutine(ollama.GenerateWait(...))`.
- **`DemonTarotPrompts`:** `BuildReadingOutlinePrompt` (pass 1), `BuildReadingSecondPassProsePrompt` (pass 2, embeds canonical outline JSON).
- **`DemonTarotReader`:** `[SerializeField] bool useTwoPassReading` (default on); `RequestDemonReading` drives `StartCoroutine` → `StartCoroutine(DemonTarotTwoPass.CoGenerate(...))`; `_requestInFlight` cleared in `finally`.
- **`TarotReadingDuelPipeline`:** `[SerializeField] bool useTwoPassDemonReading` (default on); demon step uses `DemonTarotTwoPass.CoGenerate` instead of a lone `BuildReadingPrompt` + `GenerateWait`.
- **Tradeoffs:** Two Ollama round-trips when two-pass is on; if the model returns unparseable outline JSON, behavior falls back to the original single-pass demon prompt.

### Documentation baseline (same session)
- Synced **`PLAN.md`** (current state: Ollama, duel prototype, two-pass demon — removed stale “no Ollama / no judge” lines), **`README.md`** (two-pass toggles and script names), **`requirements.txt`** (two-pass + fallback), and this **`RUNNING_LOG.md`** per the standing decision to maintain `PLAN.md` / `README.md` / `RUNNING_LOG.md` / `requirements.txt` together.

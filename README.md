# GADS7331 — Fortune Teller Game

Unity 6 narrative strategy prototype: a carnival fortune teller duels a bound **spirit** over each three-card reading. A **rule-based rubric** decides the winner; **Ollama** (local LLM) writes the spirit’s curse and the tent’s verdict announcement.

---

## Quick start

1. Install [Ollama](https://ollama.com), run `ollama serve`, and `ollama pull llama3.2` (or match the model name on **AIManager → Ollama Client**).
2. Open the project in **Unity 6000.3.14f1** (Unity Hub).
3. Play **`Assets/Scenes/MainMenu.unity`** (or **`MainScene.unity`** directly).
4. **Draw Cards** → wait for spirit text → type fortune → **Read Fortune** → **Make Judgement** → **Accept Verdict**.

Full install, specs, and troubleshooting: **`setup.md`**.

---

## Core gameplay (MainScene)

| Step | Action | LLM? |
|------|--------|------|
| 1 | Draw Cards (3-card spread) | Spirit starts when pull finishes |
| 2 | Read Fortune (typed reading + magical energy commit) | No |
| 3 | Make Judgement | Rubric instant; optional judge **prose** via Ollama |
| 4 | Accept Verdict | Updates customers / energy (`GameManager`) |

- **Hidden card data:** themes (Greed, Vanity, Chaos, Power) and morals (Good, Neutral, Bad).
- **Scoring rules (players):** `PLAYER_DUEL_SCORING_GUIDE.md` — matches `FortuneDuelRubric.cs`.
- **Win/lose:** More teller favor than spirit → +1 customer; spirit wins → −1 customer (game over at 0).

---

## AI architecture

| Component | Role |
|-----------|------|
| `OllamaClient` | HTTP client to local Ollama |
| `DemonTarotReader` + `DemonTarotTwoPass` | Spirit curse (optional outline → prose) |
| `FortuneFlowController` | Main tent flow, rubric verdict, optional judge prose |
| `FortuneDuelRubric` | **Authoritative scoring** (no LLM) |
| `JudgeVerdictProsePrompts` | Tent voice announcement only |
| `TarotReadingDuelPipeline` | **Dev harness** — gate + spirit + rubric (optional) |

Details: **`ollama-plan.md`**. Prompt archive: **`prompts-used.md`**.

---

## Key scripts

| Script | Purpose |
|--------|---------|
| `FortuneFlowController` | Draw / read / judge / accept flow |
| `GameManager` | Energy, customers, verdict consequences |
| `TarotCardPull` | Card UI and pull |
| `DemonTarotPrompts` | Spirit and gate prompts |
| `MainMenuController` | Load MainScene from menu |
| `BookManager` | In-game rulebook pages |

---

## Project documentation

| Document | Contents |
|----------|----------|
| **`HIGH_CONCEPT.md`** | Ideation, fantasy, why local LLM |
| **`ollama-plan.md`** | Model, inference timing, data flow, prompts, risks |
| **`setup.md`** | Install Unity + Ollama, run scenes, troubleshooting |
| **`refinements-changes.md`** | Scope and decision log |
| `PLAN.md` | Roadmap and milestones |
| `RUNNING_LOG.md` | Session-by-session engineering notes |
| `requirements.txt` | Requirements baseline |
| `PLAYER_DUEL_SCORING_GUIDE.md` | Player-facing duel scoring |
| **`prompts-used.md`** | Prompt archive (tested prompts, success/fail examples, iterations) |

---

## Dependencies

### Engine and packages (see `Packages/manifest.json`)

- Unity **6000.3.14f1**
- Universal Render Pipeline **17.3.0**
- Input System **1.19.0**
- uGUI **2.0.0**
- TextMesh Pro (bundled with project templates)

### External

- **Ollama** — local LLM runtime ([ollama.com](https://ollama.com))
- Default model in project: **`llama3.2`**

---

## Scenes

| Scene | Purpose |
|-------|---------|
| `Assets/Scenes/MainMenu.unity` | Start screen → loads game |
| `Assets/Scenes/MainScene.unity` | Fortune tent gameplay |
| `Assets/Scenes/SampleScene.unity` | Older test scene (not in build) |

---

## AI tools used

| Tool | Use in project |
|------|----------------|
| **Ollama** (`llama3.2` or configured model) | Spirit curse generation; optional judge verdict prose |
| **Cursor / AI-assisted coding** | Implementation, refactors, documentation drafts |
| **Unity Copilot / IDE assist** | Optional; not required to build |

No paid cloud LLM API is required for the shipped prototype flow.

---

## Credits

- **Course:** GADS7331 (Game Arts & Design), Part 2 — Fortune Teller Game project  
- **Tarot card content:** Project ScriptableObjects (`ListOfTarotCards`, `BookPages`)  
- **UI assets:** Includes third-party **Dark UI** pack (`Assets/Dark UI/`) — see pack documentation  
- **TextMesh Pro** — Unity  

---

## Repository status

Active prototype. Documentation intended to stay in sync with `FortuneFlowController` and `MainScene` wiring.  
If behavior and docs disagree, prefer **code** and file **`refinements-changes.md`** for latest decisions.

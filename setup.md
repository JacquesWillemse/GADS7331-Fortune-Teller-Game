# Technical setup guide

Complete setup for developing and running **GADS7331 Fortune Teller Game** on Windows (project tested on Windows 10/11).

---

## Requirements

### Unity

| Item | Version |
|------|---------|
| Unity Editor | **6000.3.14f1** (Unity 6) |
| Render pipeline | URP (`com.unity.render-pipelines.universal` 17.3.0) |
| Input | Input System package (`com.unity.inputsystem` 1.19.0) |
| UI | uGUI + TextMesh Pro (included in project) |

Open the project via **Unity Hub** → **Add** → select repo folder `GADS7331-Fortune-Teller-Game`.

### Ollama (local LLM)

| Item | Recommendation |
|------|----------------|
| Ollama | Latest from [ollama.com](https://ollama.com) |
| Model | `llama3.2` (or change `OllamaClient.model` to match any pulled model) |
| RAM | **8 GB+** system RAM minimum; **16 GB+** comfortable for llama3.2 |
| GPU | Optional; Ollama uses GPU when available for faster inference |
| Network | First `ollama pull` needs internet; gameplay is localhost-only |

### Disk

- Unity project + Library cache: allow **10+ GB** on first open.
- Ollama models: ~2–5 GB per model.

---

## Install Ollama

1. Download and install Ollama for Windows.
2. Open PowerShell or Terminal:
   ```bash
   ollama serve
   ```
   (Often runs automatically as a background service after install.)
3. Pull the model:
   ```bash
   ollama pull llama3.2
   ```
4. Verify:
   ```bash
   ollama list
   curl http://127.0.0.1:11434/api/tags
   ```

Default API base: **`http://127.0.0.1:11434`** — must match `OllamaClient` on **AIManager** in `MainScene`.

---

## Run the game

### Build settings

Scenes in order (already configured in `ProjectSettings/EditorBuildSettings.asset`):

1. `Assets/Scenes/MainMenu.unity` — entry / Start game  
2. `Assets/Scenes/MainScene.unity` — fortune tent gameplay  

### Play Mode

1. Open **`MainMenu`** or **`MainScene`** (use **MainMenu** to test full flow).
2. Press **Play**.
3. Ensure **Ollama** is running before drawing cards (spirit starts after draw).

### Main scene wiring (checklist)

On **AIManager** (or objects referenced by `FortuneFlowController`):

- **Ollama Client** — `OllamaClient` component  
- **Spirit Reader** — `DemonTarotReader` → same Ollama, `TarotCardPull`  
- **Fortune Flow Controller** — outputs, buttons, `GameManager`, energy slider  

Typical player flow:

1. **Draw Cards**  
2. Wait for spirit text (or switch to spirit view)  
3. Type fortune → **Read Fortune**  
4. **Make Judgement** → wait for tent verdict if LLM judge prose enabled  
5. **Accept Verdict**  

### Optional dev components

- `TarotReadingSmokeTest` — positive prompt test  
- `TarotReadingDuelPipeline` — alternate duel harness (gate + spirit + rubric); not required for main UI  

---

## Troubleshooting

| Symptom | Likely cause | Fix |
|---------|--------------|-----|
| Spirit never updates | Ollama not running | Start `ollama serve`, check URL |
| `Connection refused` | Wrong port / firewall | Use `127.0.0.1:11434`, allow localhost |
| `Empty response from Ollama` | Model name mismatch | Set `OllamaClient.model` to `ollama list` name |
| Very long wait | Two-pass spirit + large model | Normal; use smaller model or disable two-pass on `DemonTarotReader` |
| `[DemonTwoPass] Outline parse failed` | Model JSON sloppy | Usually still works via spread-synthesized outline or single-pass fallback |
| Make Judgement instant, no prose | `useLlmJudgeProse` off or no Ollama | Enable on `FortuneFlowController`; assign `ollama` |
| Accept button disabled | Verdict not rendered yet | Complete Make Judgement first |
| Draw Cards disabled | Already pulled this round | Accept Verdict to reset |

---

## Repository layout (docs)

| File | Purpose |
|------|---------|
| `README.md` | Project overview, install summary, credits |
| `HIGH_CONCEPT.md` | Design intent and why local LLM |
| `ollama-plan.md` | Inference timing, data flow, prompts |
| `refinements-changes.md` | Change and decision log |
| `PLAN.md` | Roadmap / milestones |
| `RUNNING_LOG.md` | Detailed session notes (archived style) |
| `PLAYER_DUEL_SCORING_GUIDE.md` | In-game scoring booklet |
| `prompts-used.md` | Prompt archive (course requirement) |

---

## Building a standalone player

1. **File → Build Settings**  
2. Add **MainMenu** (index 0) and **MainScene** (index 1) if missing  
3. Build for target platform  

**Note:** Standalone builds still need **Ollama running on the player’s machine** for spirit/judge LLM features unless you replace `OllamaClient` with a remote API.

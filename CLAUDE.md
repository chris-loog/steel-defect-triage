# Production Forecast Copilot

Weekend portfolio MVP for a Data & AI Engineer role. Keep it simple; this is not an enterprise system.

## What it does
Production planning for a manufacturing line (ERP/MES context, digital-twin style). Historical hourly
production output (units produced) → Python forecast of the next 24 hours → ASP.NET Core backend → web UI
with chart + table → LLM explains the forecast for a production planner. This is NOT an energy project.

## Architecture
- `forecasting/` – Python: FastAPI + pandas + scikit-learn. `train.py` trains, `app.py` serves the forecast.
- `backend/ForecastCopilot.Api/` – ASP.NET Core (MVC or Razor Pages, .NET 10). Calls Python via a typed
  HttpClient (`IHttpClientFactory`). Hosts the UI (Chart.js).
- `data/` – simulated hourly production CSV. `docs/PROGRESS.md` – stage tracker (read it first when resuming).
  `docs/PROJECT.md` – project documentation (data source, features, how to run); keep it updated per stage.
- Docker Compose runs both services. Azure deployment is optional (last stage).

## Key design rules
- Data: simulated production line (Python "digital twin" simulator: shifts, planned maintenance, breakdowns,
  changeovers, scrap), because no public real hourly production-output data exists. Always label it as simulated.
  No energy, load or weather data.
- Model: RandomForest/GradientBoosting. Features: hour, day of week, lag 24h, lag 168h. Target: next 24 hourly
  units produced.
  Always compare against the naive baseline "same hour yesterday" (MAE).
- The LLM never produces numbers. It gets structured forecast JSON and writes a short factual summary
  for a production planner (peak/low output hours, shift effects, unusual dips, change vs. previous day).
- LLM provider behind a small interface; default is local Ollama. Configure via env vars (see `.env.example`).
- No auth, user management, microservices or advanced ML/UI. No unnecessary abstractions.

## How we work
- The user is a junior dev learning .NET, Python forecasting, Azure, LLMs and Claude Code. Teach as you go.
- Explain each command or edit in 1–2 lines before doing it. Ask before changing the architecture.
- From Stage 3 on, remind the user to use plan mode at the start of each stage; no edits until the plan is approved.
- Boilerplate: write it. Core logic (features, training, HttpClient integration, LLM prompt/interface):
  explain first, implement in small steps, highlight the key lines, sometimes ask a check question.
- Stage flow: explain → plan → small steps → verify (build/test/curl) → user reviews diff in Rider →
  commit → update `docs/PROGRESS.md` → suggest `/clear`.
- Git: user does all git work in Rider; don't give git how-tos. One branch per stage (`stage-NN-name`), leave changes uncommitted, conventional
  commits (`feat:`, `fix:`, `docs:`, `chore:`, `test:`), PR into `main` at the end of each stage.
- Teach one Claude Code feature at a time when it becomes useful (1–3 line tip).

## Security
- Never commit secrets or print secret values. Use `.env` (gitignored) + `.env.example`.
- No `sudo` from Claude: give the user the command, then verify without sudo.
- Ask before deleting files, rewriting git history, or creating paid cloud resources.
- Point out security mistakes immediately.

## Efficiency (usage is limited)
- Short replies. Read narrowly (grep/glob first). Don't re-read edited files or echo code back.
- Quiet flags and small output (`pip install -q`, `dotnet build -v q`, `tail -n 50`, `head` on CSVs).
- No subagents or background tasks unless asked. Batch questions to the user.
- Keep this file under ~60 lines.

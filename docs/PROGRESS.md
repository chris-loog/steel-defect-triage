# Progress

**Current stage:** 8 – LLM (done; waiting for commit + PR) → next: 9 – Docker
**Next step:** Stage 9 in plan mode: Dockerfiles for Python API + .NET app, Docker Compose (Ollama stays on the
host → backend needs `Llm__Ollama__BaseUrl=http://host.docker.internal:11434`).

## Stages
- [x] 1 – Environment setup
- [x] 2 – Repository setup
- [x] 3 – Data
- [x] 4 – Forecasting
- [x] 5 – Python API
- [x] 6 – .NET application
- [x] 7 – UI
- [x] 8 – LLM
- [ ] 9 – Docker
- [ ] 10 – Quality (tests, CI)
- [ ] 11 – GitHub README
- [ ] 12 – Azure (optional)

## Environment (verified)
Ubuntu 26.04 · .NET SDK 10.0.112 · Python 3.14 (venv ok) · Docker Engine (apt, docker group, no sudo)
+ Compose v5.5 · gh (logged in as chris-loog) · Ollama 0.34 with `qwen3.5`.
Azure CLI not installed yet – install in Stage 12.

## Decisions
- Docker: apt Docker Engine only (snap removed; Docker Desktop not used).
- LLM: local Ollama (systemd service) with `qwen3.5`, `think:false`; config section `Llm` / env `Llm__*`.
  C# computes all facts (`ForecastFacts`), LLM only phrases them; summary loaded async via `/Home/Summary`.
- GitHub repo `chris-loog/production-forecast-copilot` is **private** until polished.
- No PostgreSQL for now – CSV data. Add Postgres in Docker later only if useful.
- IDE: JetBrains Rider (replaces VS Code). User handles git (review, commit, push, PR merge) themselves.
- Rider plugins: Claude Code [Beta], Azure Toolkit for Rider, .env files support; Docker + Git + Database tools are bundled.
  Python: check Marketplace for the Python plugin in Rider; otherwise edit `forecasting/` in PyCharm.

- Domain: **production planning**, not energy. Target = hourly units produced by one production line.
- Data: simulated production line ("digital twin" simulator), because no public real hourly production data was found.
  UCI steel-plant energy dataset was tried and dropped (energy ≠ production).
- Simulator: 3 shifts Mon–Fri, Sat early only, 2 years hourly, seed 42 (see PROJECT.md §3).
- Model: RandomForest (test MAE 10.3 vs. baseline 40.6/16.3); `model.joblib` gitignored, `metrics.json` committed.
- Backend: ASP.NET Core **MVC** + `ProductionForecastCopilot.slnx` at repo root; config key `ForecastApi:BaseUrl`.
- UI: chart data embedded as JSON in the page (not fetched); Chart.js 4.5.1 vendored locally; KPIs computed in C#.
- Exploration in a Jupyter notebook (`forecasting/notebooks/`), outputs kept so GitHub shows plots.

## Open issues
- None.

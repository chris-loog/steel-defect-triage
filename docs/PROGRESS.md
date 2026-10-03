# Progress

**Current stage:** 6 – .NET application (done; waiting for commit + PR) → next: 7 – UI
**Next step:** Stage 7 in plan mode: Chart.js chart + table on `/`, fetching `GET /api/forecast`
(.NET on port 5088, Python on 8000; see PROJECT.md §6).

## Stages
- [x] 1 – Environment setup
- [x] 2 – Repository setup
- [x] 3 – Data
- [x] 4 – Forecasting
- [x] 5 – Python API
- [x] 6 – .NET application
- [ ] 7 – UI
- [ ] 8 – LLM
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
- LLM: local Ollama; exact model chosen in Stage 8 (`qwen3.5` available).
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
- Exploration in a Jupyter notebook (`forecasting/notebooks/`), outputs kept so GitHub shows plots.

## Open issues
- None.

# Progress

**Current stage:** 3 – Data (done; waiting for commit + PR) → next: 4 – Forecasting
**Next step:** Stage 4 in plan mode: features (hour, day of week, lag 24h, lag 168h), train RF/GB in `train.py`,
compare with baseline MAE 40.6 (same hour yesterday; lag 168h = 16.3).
Python venv (`forecasting/.venv`) and pinned `forecasting/requirements.txt` are set up.

## Stages
- [x] 1 – Environment setup
- [x] 2 – Repository setup
- [x] 3 – Data
- [ ] 4 – Forecasting
- [ ] 5 – Python API
- [ ] 6 – .NET application
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
- Exploration in a Jupyter notebook (`forecasting/notebooks/`), outputs kept so GitHub shows plots.

## Open issues
- None.

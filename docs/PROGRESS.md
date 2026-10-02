# Progress

**Current stage:** 3 – Data (not started)
**Next step:** Stage 3 – Data (create/obtain hourly demand dataset, explore with pandas). Start in plan mode.

## Stages
- [x] 1 – Environment setup
- [x] 2 – Repository setup
- [ ] 3 – Data
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

## Open issues
- None.

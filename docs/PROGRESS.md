# Progress

**Current stage:** 11 – GitHub README (done; waiting for commit + PR) → next: 12 – Azure (optional)
**Next step:** Stage 12 in plan mode: decide whether to deploy to Azure (paid resources – ask first).

## Stages
- [x] 1 – Environment setup
- [x] 2 – Repository setup
- [x] 3 – Data
- [x] 4 – Forecasting
- [x] 5 – Python API
- [x] 6 – .NET application
- [x] 7 – UI
- [x] 8 – LLM
- [x] 9 – Docker
- [x] 10 – Quality (tests, CI)
- [x] 11 – GitHub README
- [ ] 12 – Azure (optional)

## Environment (verified)
Ubuntu 26.04 · .NET SDK 10.0.112 · Python 3.14 (venv ok) · Docker Engine (apt, docker group, no sudo)
+ Compose v5.5 · gh (logged in as chris-loog) · Ollama 0.34 with `qwen3.5`.
Azure CLI not installed yet – install in Stage 12.

## Decisions
- Docker: apt Docker Engine only (snap removed; Docker Desktop not used).
- Docker Compose: `forecast-api` + `web`; Ollama stays on the host, bound to docker0 IP `172.17.0.1:11434`
  (systemd override, not exposed to LAN); containers use `host.docker.internal` (host-gateway).
  Local dev uses `172.17.0.1` too (`appsettings.Development.json`, `OLLAMA_HOST` in `~/.bashrc`).
- Python image trains the model at build time; `requirements.txt` = runtime, `requirements-dev.txt` = + notebook.
- LLM: local Ollama (systemd service) with `qwen3.5`, `think:false`; config section `Llm` / env `Llm__*`.
  C# computes all facts (`ForecastFacts`), LLM only phrases them; summary loaded async via `/Home/Summary`.
- GitHub repo `chris-loog/production-forecast-copilot` is **private** until polished.
- README: Mermaid diagram, MIT license, no screenshot until the UI is final.
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
- Tests: pytest (9: features + API via `TestClient`) and xUnit (6: `ForecastFacts`); no LLM calls in tests.
  `requirements-test.txt` = runtime + pytest + httpx2 (CI installs this, skips Jupyter).
- CI: GitHub Actions `ci.yml` on PRs + pushes to `main`; jobs `python` (trains model, then pytest) and `dotnet`.
- Learning: check questions go to `QUESTIONS.md` (gitignored, local only); user answers them after the project.
- Exploration in a Jupyter notebook (`forecasting/notebooks/`), outputs kept so GitHub shows plots.

## Open issues
- None.

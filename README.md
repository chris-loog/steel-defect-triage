# Steel Defect Triage Copilot

[![CI](https://github.com/chris-loog/production-forecast-copilot/actions/workflows/ci.yml/badge.svg)](https://github.com/chris-loog/production-forecast-copilot/actions/workflows/ci.yml)
![.NET 10](https://img.shields.io/badge/.NET-10-512BD4)
![Python 3.14](https://img.shields.io/badge/Python-3.14-3776AB)
![License: MIT](https://img.shields.io/badge/License-MIT-green)

An automatic surface-inspection system finds defects on steel plates. This app classifies each defect into one of
7 fault types, lets a quality engineer pick a confidence threshold that splits a batch into **auto-classified** and
**manual review**, and has an LLM write a short handover note about the batch. It's a small end-to-end Data & AI
project: a Python classifier, an ASP.NET Core web app and a local LLM.

> [!NOTE]
> **Real data:** [UCI Steel Plates Faults](https://doi.org/10.24432/C5J88N) by M. Buscema, S. Terzi and W. Tastle
> (2010), licensed [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/). It contains 1,941 defects with 27
> geometric and luminosity features. See [`data/steel_plates/README.md`](data/steel_plates/README.md).

## What it does
1. **Classifies** each defect (Bumps, Dirtiness, K_Scatch, Other_Faults, Pastry, Stains, Z_Scratch) with a
   scikit-learn GradientBoosting model served by FastAPI. Each prediction comes with a confidence score.
2. **Triages** a batch of 50 defects in an ASP.NET Core MVC page. It shows a fault-mix chart, a confidence dot strip
   and a threshold slider. Moving the slider shows how many defects need manual review and the error rate to expect
   on the auto-classified ones (measured on the held-out test set).
3. **Writes a handover note:** a local LLM (Ollama) turns precomputed facts into a headline and three short sections:
   which defects to review first, what to watch out for (confused fault types, less reliable classes, an unusual
   mix), and what moving the threshold would change.

The demo batches are random samples from the held-out test set (the dataset has no timestamps or production
batches), so the page can also mark wrong predictions.

## Architecture
```mermaid
flowchart LR
    DATA[("data/steel_plates<br/>UCI, CC BY 4.0")]
    subgraph compose["Docker Compose"]
        PY["forecast-api<br/>Python · FastAPI<br/>scikit-learn"]
        WEB["web<br/>ASP.NET Core MVC<br/>typed HttpClient · Chart.js"]
    end
    LLM["Ollama on host<br/>qwen3.5"]
    USER(["Quality engineer<br/>browser"])

    DATA --> PY
    PY -- "/batch · /threshold-stats · /model-info" --> WEB
    WEB -- "facts JSON only" --> LLM
    LLM -- "structured note (JSON)" --> WEB
    WEB --> USER
```

**The LLM never produces numbers.** C# (`TriageFacts`, `SummaryFacts`) computes everything: fault mix, review queue
with runner-up classes, confusion groups, per-class reliability and the threshold trade-off. The LLM only phrases
those facts, using structured output (JSON schema) for a fixed note format. A `NumberGuard` then flags any number in
the note that isn't in the facts. If Ollama is down, the rest of the page still works.

## Model
Stratified 80/20 split (seed 42, 389 test defects). Model chosen by 5-fold cross-validated macro F1 on the training set.

| Model (held-out test set) | Accuracy | Macro F1 |
|---|---|---|
| Baseline: most frequent class | 0.347 | 0.074 |
| RandomForest | 0.784 | 0.786 |
| **GradientBoosting (used)** | **0.789** | **0.789** |

- **Leakage check:** the absolute plate-position features (`X/Y_Minimum/Maximum`) added only 0.01–0.03 macro F1,
  only through Y, and with no physical explanation, so they were dropped as likely line-specific. 22 features remain.
- **Hardest classes:** Pastry, Dirtiness and Bumps are often confused with Other_Faults.
- **Threshold trade-off (test set):**

  | Threshold | Auto-classified | Errors among auto-classified |
  |---|---|---|
  | 0.6 | 80 % | 15.2 % |
  | 0.7 | 68 % | 10.9 % |
  | 0.8 | 57 % | 8.2 % |

Exploration: [`forecasting/notebooks/02_steel_explore.ipynb`](forecasting/notebooks/02_steel_explore.ipynb).

## Tech stack
- **ML service:** Python 3.14, pandas, scikit-learn, FastAPI, Jupyter (exploration)
- **Backend / UI:** ASP.NET Core MVC (.NET 10), typed `HttpClient` via `IHttpClientFactory`, Chart.js
- **LLM:** Ollama (local, no API key, data stays on the machine) behind a small `ILlmClient` interface
- **Ops:** Docker Compose, GitHub Actions CI, pytest, xUnit

## Quick start
Requirements: Docker with Compose, and [Ollama](https://ollama.com) on the host with `ollama pull qwen3.5`.
Containers reach Ollama on the host via `host.docker.internal`, so Ollama must listen on the docker0 IP
(`OLLAMA_HOST=172.17.0.1:11434`).

```bash
docker compose up --build -d
# UI → http://localhost:5088    Python API docs → http://localhost:8000/docs
docker compose down
```

The Python image trains the model at build time. Without Docker: in `forecasting/` create a venv, install
`requirements-dev.txt`, run `python train.py` and `uvicorn app:app --port 8000`; then
`dotnet run --project backend/ForecastCopilot.Api`. Configuration via environment variables: see `.env.example`.

## Tests & CI
```bash
cd forecasting && .venv/bin/pytest -q             # features, leakage decision, API contract
dotnet test ProductionForecastCopilot.slnx -v q   # facts, NumberGuard, prompt, controller (in-memory)
```
The controller tests run the whole web app in memory (`WebApplicationFactory`) with a fake Python API and a fake
LLM. GitHub Actions runs both suites on every pull request and on every push to `main`. Tests never call the LLM.

## Repository layout
```
data/steel_plates/           UCI Steel Plates Faults (unchanged) + attribution README
forecasting/                 features.py, train.py, app.py (FastAPI), model/ (metrics, test predictions), tests/, notebooks/
backend/ForecastCopilot.Api/ ASP.NET Core MVC app: API client, triage/summary facts, LLM client, NumberGuard, UI
backend/ForecastCopilot.Api.Tests/  xUnit tests
compose.yaml                 runs forecast-api + web
```

## Limitations & next steps
- One public dataset from one line: the results show the method, not real plant performance.
- No timestamps, so "batches" are samples from the test set rather than real production batches.
- `NumberGuard` catches invented numbers, but not a real number attached to the wrong defect. The note still needs a
  human read.
- Next: rename the repo and solution to match v2, then an optional Azure deployment.

The first version (hourly output forecast on simulated data) is kept under the git tag `v1`.

## License
Code: [MIT](LICENSE). Data: UCI Steel Plates Faults, CC BY 4.0, Buscema, Terzi & Tastle (2010),
doi:[10.24432/C5J88N](https://doi.org/10.24432/C5J88N).

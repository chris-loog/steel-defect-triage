# Production Forecast Copilot – Project Documentation

> Living document. Updated at the end of each stage. Stage status lives in [`PROGRESS.md`](PROGRESS.md).

## 1. Purpose
A small portfolio MVP showing an end-to-end Data & AI workflow in a **manufacturing / ERP context**:
forecast the next 24 hours of a plant's activity and let an LLM explain the forecast in plain language,
the way a production planner would see it next to their ERP/MES data (digital-twin style view of the plant).

## 2. Domain & target
- **Signal:** hourly electrical load of a steel plant (`Usage_kWh`). Plant load follows production activity
  (shifts, weekdays, idle periods), so it serves as a real-world proxy for production output.
- **Forecast target:** the next 24 hourly `Usage_kWh` values.
- **Why not weather-driven data (solar, wind, grid demand)?** Those series are driven mainly by weather,
  not by production schedules, so they don't fit an ERP/production story or our time-based features.

## 3. Data source
| | |
|---|---|
| Dataset | Steel Industry Energy Consumption |
| Origin | DAEWOO Steel Co. Ltd, Gwangyang, South Korea |
| Period | 2018-01-01 – 2018-12-31, 15-minute intervals (35,040 rows) → resampled to hourly (~8,760 rows) |
| Download | https://archive.ics.uci.edu/dataset/851/steel+industry+energy+consumption |
| License | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) |
| Citation | V E, S., Shin, C., & Cho, Y. (2021). *Steel Industry Energy Consumption* [Dataset]. UCI Machine Learning Repository. https://doi.org/10.24432/C52G8C |

**Original columns:** `date`, `Usage_kWh`, `Lagging_Current_Reactive.Power_kVarh`,
`Leading_Current_Reactive_Power_kVarh`, `CO2(tCO2)`, `Lagging_Current_Power_Factor`,
`Leading_Current_Power_Factor`, `NSM` (seconds from midnight), `WeekStatus`, `Day_of_week`,
`Load_Type` (Light / Medium / Maximum load).

**Limitation:** only one year of data, so yearly seasonality can't be learned. Daily and weekly
patterns (what our features target) are well covered.

## 4. Architecture
```
data/ (CSV) → forecasting/ (Python: pandas, scikit-learn, FastAPI)
            → backend/ForecastCopilot.Api (ASP.NET Core, typed HttpClient, Chart.js UI)
            → LLM (local Ollama by default) writes a short factual summary
```
Docker Compose runs both services. Azure deployment is optional.

## 5. Forecasting approach
- Model: RandomForest / GradientBoosting.
- Features: hour of day, day of week, lag 24h, lag 168h.
- Baseline: "same hour yesterday". The model must beat it on MAE (mean absolute error).
- The LLM never produces numbers; it only describes the structured forecast JSON.

## 6. How to run
_Filled in from Stage 4 onward._

## 7. Stage notes
### Stage 3 – Data
_In progress: download, clean (15-min → hourly), explore in a Jupyter notebook._

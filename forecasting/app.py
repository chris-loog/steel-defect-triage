"""FastAPI service: next-24h production forecast from the trained model (simulated production data).

Run: uvicorn app:app --reload --port 8000   (train first: python train.py)
"""
import json
from datetime import datetime
from pathlib import Path

import joblib
import pandas as pd
from fastapi import FastAPI
from pydantic import BaseModel

from features import FEATURES, make_features

ROOT = Path(__file__).resolve().parent
DATA = ROOT.parent / "data" / "production_hourly.csv"
MODEL_DIR = ROOT / "model"
TARGET = "units_produced"
HORIZON = 24  # hours

if not (MODEL_DIR / "model.joblib").exists():
    raise RuntimeError("model/model.joblib not found - run `python train.py` first.")
model = joblib.load(MODEL_DIR / "model.joblib")
metrics = json.loads((MODEL_DIR / "metrics.json").read_text())


# Response models = the JSON contract the .NET backend will deserialize (see /docs).
class ForecastPoint(BaseModel):
    timestamp: datetime
    units_predicted: int
    units_same_hour_yesterday: int  # naive baseline = actual output 24 h earlier


class MaeInfo(BaseModel):
    model: float
    baseline_same_hour_yesterday: float


class ForecastResponse(BaseModel):
    data_source: str
    model: str
    mae: MaeInfo
    last_actual_timestamp: datetime
    forecast: list[ForecastPoint]


def load_units() -> pd.Series:
    return pd.read_csv(DATA, parse_dates=["timestamp"], index_col="timestamp")[TARGET]


def forecast_next_24h(units: pd.Series) -> list[ForecastPoint]:
    # Append 24 empty future hours; their lag_24/lag_168 features point back into known history.
    future = pd.date_range(units.index.max() + pd.Timedelta(hours=1), periods=HORIZON, freq="h")
    extended = units.reindex(units.index.append(future))
    X = make_features(extended).loc[future, FEATURES]
    predicted = model.predict(X).round().clip(min=0).astype(int)
    return [
        ForecastPoint(timestamp=ts, units_predicted=pred, units_same_hour_yesterday=int(lag))
        for ts, pred, lag in zip(future, predicted, X["lag_24"])
    ]


app = FastAPI(title="Production Forecast API", description="Forecast on SIMULATED production line data.")


@app.get("/health")
def health() -> dict:
    return {"status": "ok"}


@app.get("/forecast")
def forecast() -> ForecastResponse:
    units = load_units()
    return ForecastResponse(
        data_source=metrics["data"],
        model=metrics["model"],
        mae=MaeInfo(
            model=metrics["mae"][metrics["model"]],
            baseline_same_hour_yesterday=metrics["mae"]["baseline_same_hour_yesterday"],
        ),
        last_actual_timestamp=units.index.max(),
        forecast=forecast_next_24h(units),
    )

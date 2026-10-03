"""FastAPI service for the steel-plate fault classifier: /predict, demo /batch and /threshold-stats what-if.

Run: uvicorn app:app --reload --port 8000   (train first: python train.py)
"""
import json
from pathlib import Path

import joblib
import pandas as pd
from fastapi import FastAPI, Query
from pydantic import BaseModel

from features import FEATURES, load_steel_data

ROOT = Path(__file__).resolve().parent
MODEL_DIR = ROOT / "model"

if not (MODEL_DIR / "model.joblib").exists():
    raise RuntimeError("model/model.joblib not found - run `python train.py` first.")
model = joblib.load(MODEL_DIR / "model.joblib")
metrics = json.loads((MODEL_DIR / "metrics.json").read_text())
# Held-out test defects (never seen in training): source of demo batches and of the threshold what-if.
test_predictions = pd.read_csv(MODEL_DIR / "test_predictions.csv")
X_all, _ = load_steel_data()
X_test = X_all.loc[test_predictions["row"], FEATURES]


# Request/response models = the JSON contract for the .NET backend (see /docs).
class Defect(BaseModel):
    """One defect as the inspection system measures it: the 22 model features (names as in the UCI data)."""
    Pixels_Areas: float
    X_Perimeter: float
    Y_Perimeter: float
    Sum_of_Luminosity: float
    Minimum_of_Luminosity: float
    Maximum_of_Luminosity: float
    Length_of_Conveyer: float
    TypeOfSteel_A300: float
    Steel_Plate_Thickness: float
    Edges_Index: float
    Empty_Index: float
    Square_Index: float
    Outside_X_Index: float
    Edges_X_Index: float
    Edges_Y_Index: float
    Outside_Global_Index: float
    LogOfAreas: float
    Log_X_Index: float
    Log_Y_Index: float
    Orientation_Index: float
    Luminosity_Index: float
    SigmoidOfAreas: float


class Prediction(BaseModel):
    predicted_fault: str
    confidence: float  # highest class probability
    probabilities: dict[str, float]  # fault type -> probability


class BatchDefect(Prediction):
    row: int  # line number in Faults.NNA
    true_fault: str  # known because the batch comes from the labelled test set


class ThresholdStat(BaseModel):
    threshold: float
    n_auto: int  # test defects with confidence >= threshold
    auto_share: float
    manual_share: float
    error_rate: float | None  # wrong among auto-classified; None if nothing is auto-classified

app = FastAPI(title="Steel Defect Triage API", description="Fault-type classifier on UCI Steel Plates Faults data.")


@app.get("/health")
def health() -> dict:
    return {"status": "ok"}


@app.get("/model-info")
def model_info() -> dict:
    """Training metrics of the loaded model (metrics.json written by train.py)."""
    return metrics


def predict(X: pd.DataFrame) -> list[Prediction]:
    """Classify defects: fault = class with the highest probability, confidence = that probability."""
    proba = model.predict_proba(X[FEATURES])  # one row per defect, columns in model.classes_ order
    return [
        Prediction(
            predicted_fault=model.classes_[p.argmax()],
            confidence=round(float(p.max()), 4),
            probabilities={c: round(float(v), 4) for c, v in zip(model.classes_, p)},
        )
        for p in proba
    ]


@app.post("/predict")
def predict_defects(defects: list[Defect]) -> list[Prediction]:
    """Classify defects sent by the inspection system (raw features, as a real line would call it)."""
    return predict(pd.DataFrame([d.model_dump() for d in defects], columns=FEATURES))


@app.get("/batch")
def batch(size: int = Query(50, ge=1, le=len(test_predictions)), seed: int | None = None) -> list[BatchDefect]:
    """Demo batch: random held-out test defects, classified now. Same seed -> same batch."""
    sample = test_predictions.sample(n=size, random_state=seed)
    predictions = predict(X_test.loc[sample["row"]])
    return [
        BatchDefect(row=row, true_fault=true, **p.model_dump())
        for row, true, p in zip(sample["row"], sample["true_fault"], predictions)
    ]


THRESHOLDS = [round(0.30 + 0.05 * i, 2) for i in range(14)]  # 0.30 ... 0.95 (lowest possible confidence is 1/7)


def threshold_stats(predictions: pd.DataFrame, thresholds: list[float]) -> list[ThresholdStat]:
    """What-if per threshold: confidence >= threshold -> auto-classified, else manual review."""
    stats = []
    for t in thresholds:
        auto = predictions[predictions["confidence"] >= t]
        auto_share = len(auto) / len(predictions)
        stats.append(ThresholdStat(
            threshold=t,
            n_auto=len(auto),
            auto_share=round(auto_share, 4),
            manual_share=round(1 - auto_share, 4),
            error_rate=round(1 - auto["correct"].mean(), 4) if len(auto) else None,
        ))
    return stats


# Computed once: the test set never changes while the API runs.
THRESHOLD_STATS = threshold_stats(test_predictions, THRESHOLDS)


@app.get("/threshold-stats")
def get_threshold_stats() -> list[ThresholdStat]:
    """Expected auto-rate, review load and error rate per confidence threshold (from the held-out test set)."""
    return THRESHOLD_STATS

"""FastAPI service for the steel-plate fault classifier - PLACEHOLDER until Stage 14 adds /predict + threshold stats.

Run: uvicorn app:app --reload --port 8000   (train first: python train.py)
"""
import json
from pathlib import Path

import joblib
from fastapi import FastAPI

ROOT = Path(__file__).resolve().parent
MODEL_DIR = ROOT / "model"

if not (MODEL_DIR / "model.joblib").exists():
    raise RuntimeError("model/model.joblib not found - run `python train.py` first.")
model = joblib.load(MODEL_DIR / "model.joblib")
metrics = json.loads((MODEL_DIR / "metrics.json").read_text())

app = FastAPI(title="Steel Defect Triage API", description="Fault-type classifier on UCI Steel Plates Faults data.")


@app.get("/health")
def health() -> dict:
    return {"status": "ok"}


@app.get("/model-info")
def model_info() -> dict:
    """Training metrics of the loaded model (metrics.json written by train.py)."""
    return metrics

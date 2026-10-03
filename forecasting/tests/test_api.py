"""API tests for app.py via FastAPI's in-process TestClient (needs model/model.joblib: run train.py first).

Stage 13 placeholder API: /health + /model-info. Stage 14 adds /predict and threshold-stats tests.
"""
from fastapi.testclient import TestClient

from app import app

client = TestClient(app)


def test_health():
    assert client.get("/health").json() == {"status": "ok"}


def test_model_info_beats_baseline_and_credits_data_source():
    info = client.get("/model-info").json()
    test = info["test"]
    assert test[info["model"]]["macro_f1"] > test["baseline_most_frequent"]["macro_f1"]
    assert "CC BY 4.0" in info["data"]

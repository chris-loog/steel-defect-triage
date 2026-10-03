"""API tests for app.py via FastAPI's in-process TestClient (needs model/model.joblib: run train.py first)."""
import pandas as pd
import pytest
from fastapi.testclient import TestClient

from app import HORIZON, app, load_units

client = TestClient(app)


@pytest.fixture(scope="module")
def forecast() -> dict:
    response = client.get("/forecast")
    assert response.status_code == 200
    return response.json()


def test_health():
    assert client.get("/health").json() == {"status": "ok"}


def test_forecast_has_24_consecutive_hours_after_last_actual(forecast):
    timestamps = pd.to_datetime([p["timestamp"] for p in forecast["forecast"]])
    expected = pd.date_range(
        pd.Timestamp(forecast["last_actual_timestamp"]) + pd.Timedelta(hours=1), periods=HORIZON, freq="h"
    )
    assert list(timestamps) == list(expected)


def test_predictions_are_non_negative(forecast):
    assert all(p["units_predicted"] >= 0 for p in forecast["forecast"])


def test_baseline_is_actual_output_24h_earlier(forecast):
    units = load_units()
    for p in forecast["forecast"]:
        yesterday = pd.Timestamp(p["timestamp"]) - pd.Timedelta(hours=24)
        assert p["units_same_hour_yesterday"] == units[yesterday]


def test_response_reports_mae_and_simulated_source(forecast):
    assert forecast["mae"]["model"] < forecast["mae"]["baseline_same_hour_yesterday"]
    assert "simulated" in forecast["data_source"].lower()

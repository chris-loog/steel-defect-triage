"""API tests for app.py via FastAPI's in-process TestClient (needs model/model.joblib: run train.py first)."""
import pytest
from fastapi.testclient import TestClient

from app import Defect, X_test, app, test_predictions
from features import FEATURES

client = TestClient(app)


def test_health():
    assert client.get("/health").json() == {"status": "ok"}


def test_model_info_beats_baseline_and_credits_data_source():
    info = client.get("/model-info").json()
    test = info["test"]
    assert test[info["model"]]["macro_f1"] > test["baseline_most_frequent"]["macro_f1"]
    assert "CC BY 4.0" in info["data"]


def test_defect_contract_matches_model_features():
    assert list(Defect.model_fields) == FEATURES


def test_predict_real_test_defect():
    defect = X_test.iloc[0].to_dict()
    [p] = client.post("/predict", json=[defect]).json()
    assert p["predicted_fault"] in p["probabilities"]
    assert sum(p["probabilities"].values()) == pytest.approx(1, abs=1e-3)
    assert p["confidence"] == max(p["probabilities"].values())


def test_predict_rejects_missing_feature():
    defect = X_test.iloc[0].to_dict()
    del defect["Pixels_Areas"]
    assert client.post("/predict", json=[defect]).status_code == 422


def test_batch_is_reproducible_sample_of_test_set():
    batch = client.get("/batch", params={"size": 20, "seed": 1}).json()
    assert len(batch) == 20
    assert {d["row"] for d in batch} <= set(test_predictions["row"])
    assert client.get("/batch", params={"size": 20, "seed": 1}).json() == batch


def test_batch_confidences_match_training_output():
    expected = test_predictions.set_index("row")["confidence"]
    for d in client.get("/batch", params={"size": 50, "seed": 2}).json():
        assert d["confidence"] == pytest.approx(expected[d["row"]], abs=1e-4)


@pytest.mark.parametrize("size", [0, len(test_predictions) + 1])
def test_batch_rejects_invalid_size(size):
    assert client.get("/batch", params={"size": size}).status_code == 422


def test_threshold_stats():
    stats = client.get("/threshold-stats").json()
    shares = [s["auto_share"] for s in stats]
    assert shares == sorted(shares, reverse=True)  # higher threshold -> never more auto-classified
    at_07 = next(s for s in stats if s["threshold"] == 0.7)
    assert at_07["auto_share"] == pytest.approx(0.68, abs=0.01)
    assert at_07["error_rate"] == pytest.approx(0.109, abs=0.005)

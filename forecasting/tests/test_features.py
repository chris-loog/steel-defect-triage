"""Unit tests for load_steel_data (data loading shared by train.py and app.py)."""
import pandas as pd
import pytest

from features import ALL_FEATURES, CLASSES, DATA_DIR, FEATURES, POSITION_FEATURES, load_steel_data


@pytest.fixture(scope="module")
def data() -> tuple[pd.DataFrame, pd.Series]:
    return load_steel_data()


def test_shape_and_no_missing_values(data):
    X, y = data
    assert X.shape == (1941, len(ALL_FEATURES)) and len(y) == 1941
    assert not X.isna().any().any()


def test_redundant_and_position_features_not_used():
    assert "TypeOfSteel_A400" not in ALL_FEATURES
    assert not set(POSITION_FEATURES) & set(FEATURES)
    assert set(FEATURES) <= set(ALL_FEATURES)


def test_label_matches_one_hot_column(data):
    _, y = data
    raw = pd.read_csv(DATA_DIR / "Faults.NNA", sep="\t", header=None)
    one_hot = raw.iloc[:, -7:].to_numpy()
    for i in [0, 500, 1940]:
        assert one_hot[i, CLASSES.index(y[i])] == 1


def test_class_counts_match_data_readme(data):
    _, y = data
    assert y.value_counts().to_dict() == {
        "Other_Faults": 673, "Bumps": 402, "K_Scatch": 391, "Z_Scratch": 190,
        "Pastry": 158, "Stains": 72, "Dirtiness": 55,
    }

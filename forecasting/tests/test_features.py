"""Unit tests for make_features (feature engineering shared by train.py and app.py)."""
import pandas as pd

from features import FEATURES, make_features


def hourly_series(hours: int = 200) -> pd.Series:
    # Value = position (0, 1, 2, ...), so the expected lag value is simply "position - lag".
    index = pd.date_range("2024-10-07 00:00", periods=hours, freq="h")  # 2024-10-07 is a Monday
    return pd.Series(range(hours), index=index, dtype=float)


def test_columns_match_feature_list():
    assert list(make_features(hourly_series()).columns) == FEATURES


def test_hour_and_dayofweek():
    df = make_features(hourly_series())
    assert df["hour"].iloc[0] == 0 and df["hour"].iloc[13] == 13
    assert df["dayofweek"].iloc[0] == 0   # Monday
    assert df["dayofweek"].iloc[24] == 1  # Tuesday


def test_lag_24_is_same_hour_yesterday():
    df = make_features(hourly_series())
    assert df["lag_24"].iloc[:24].isna().all()  # no history yet
    assert df["lag_24"].iloc[30] == 30 - 24


def test_lag_168_is_same_hour_last_week():
    df = make_features(hourly_series())
    assert df["lag_168"].iloc[:168].isna().all()
    assert df["lag_168"].iloc[190] == 190 - 168

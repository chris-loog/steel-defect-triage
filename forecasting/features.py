"""Feature engineering shared by train.py and app.py (simulated production data)."""
import pandas as pd

FEATURES = ["hour", "dayofweek", "lag_24", "lag_168"]


def make_features(units: pd.Series) -> pd.DataFrame:
    """Build model features from an hourly units_produced series with a DatetimeIndex.

    All features are known 24 h ahead, so one model can forecast each of the next 24 hours.
    """
    df = pd.DataFrame(index=units.index)
    df["hour"] = units.index.hour
    df["dayofweek"] = units.index.dayofweek  # 0 = Monday
    df["lag_24"] = units.shift(24)    # same hour yesterday
    df["lag_168"] = units.shift(168)  # same hour last week
    return df

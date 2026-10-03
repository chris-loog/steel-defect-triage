"""Train the 24h production forecast model on simulated data and compare against naive baselines."""
import json
from pathlib import Path

import joblib
import pandas as pd
from sklearn.ensemble import GradientBoostingRegressor, RandomForestRegressor
from sklearn.metrics import mean_absolute_error

from features import FEATURES, make_features

ROOT = Path(__file__).resolve().parent
DATA = ROOT.parent / "data" / "production_hourly.csv"
MODEL_DIR = ROOT / "model"
TARGET = "units_produced"

units = pd.read_csv(DATA, parse_dates=["timestamp"], index_col="timestamp")[TARGET]
df = make_features(units)
df[TARGET] = units
df = df.dropna()  # first week has no lag_168

# Time-based split: last 8 weeks = test (same as the exploration notebook). Never shuffle time series.
split = units.index.max() - pd.Timedelta(weeks=8)
train, test = df[df.index < split], df[df.index >= split]
X_train, y_train = train[FEATURES], train[TARGET]
X_test, y_test = test[FEATURES], test[TARGET]

mae = {
    "baseline_same_hour_yesterday": mean_absolute_error(y_test, test["lag_24"]),
    "baseline_same_hour_last_week": mean_absolute_error(y_test, test["lag_168"]),
}
models = {
    "random_forest": RandomForestRegressor(n_estimators=200, min_samples_leaf=5, random_state=42, n_jobs=-1),
    "gradient_boosting": GradientBoostingRegressor(random_state=42),
}
for name, model in models.items():
    model.fit(X_train, y_train)
    mae[name] = mean_absolute_error(y_test, model.predict(X_test))

print(f"Test MAE (units/hour), {test.index.min():%Y-%m-%d} .. {test.index.max():%Y-%m-%d}:")
for name, value in mae.items():
    print(f"  {name:<30} {value:6.1f}")

best = min(models, key=lambda name: mae[name])
print(f"Best model: {best}")

MODEL_DIR.mkdir(exist_ok=True)
joblib.dump(models[best], MODEL_DIR / "model.joblib")
metrics = {
    "data": "simulated production line",
    "model": best,
    "features": FEATURES,
    "train_range": [str(train.index.min()), str(train.index.max())],
    "test_range": [str(test.index.min()), str(test.index.max())],
    "mae": {name: round(value, 2) for name, value in mae.items()},
}
(MODEL_DIR / "metrics.json").write_text(json.dumps(metrics, indent=2) + "\n")

"""Train the steel-plate fault-type classifier and compare it against a most-frequent-class baseline.

Writes model/model.joblib (gitignored), model/metrics.json and model/test_predictions.csv (test-set confidence table
for the threshold what-if in the API).
"""
import json
from pathlib import Path

import joblib
import pandas as pd
from sklearn.base import clone
from sklearn.dummy import DummyClassifier
from sklearn.ensemble import GradientBoostingClassifier, RandomForestClassifier
from sklearn.metrics import accuracy_score, classification_report, f1_score
from sklearn.model_selection import StratifiedKFold, cross_val_score, train_test_split

from features import ALL_FEATURES, FEATURES, POSITION_FEATURES, TARGET, load_steel_data

MODEL_DIR = Path(__file__).resolve().parent / "model"
SEED = 42
# Decision (Stage 13): position features dropped although they add ~0.01-0.03 CV macro F1. The gain comes only from
# Y (position along the line), Y alone is no better than the baseline, and nearby defects do not share labels more
# often -> no physical explanation, likely specific to this one line. Re-checked on every run below.
POSITION_DECISION = "dropped: small gain (~0.01-0.03, CV-seed dependent), only from Y, no physical explanation"

X_all, y = load_steel_data()
# Stratified: keeps the class shares (Dirtiness only 2.8 %) equal in train and test. The file is sorted by class,
# so the split must shuffle (train_test_split does by default).
X_train_all, X_test_all, y_train, y_test = train_test_split(
    X_all, y, test_size=0.2, stratify=y, random_state=SEED
)
cv = StratifiedKFold(n_splits=5, shuffle=True, random_state=SEED)


def cv_macro_f1(model, features: list[str]) -> float:
    """Mean macro F1 over 5 CV folds on the TRAIN set only (test set stays untouched for the final report)."""
    return cross_val_score(clone(model), X_train_all[features], y_train, cv=cv, scoring="f1_macro").mean()


models = {
    "baseline_most_frequent": DummyClassifier(strategy="most_frequent"),
    "random_forest": RandomForestClassifier(n_estimators=300, random_state=SEED, n_jobs=-1),
    "gradient_boosting": GradientBoostingClassifier(random_state=SEED),
}

# 1) Model selection by cross-validation on train.
cv_scores = {name: cv_macro_f1(model, FEATURES) for name, model in models.items()}
best = max((name for name in models if name != "baseline_most_frequent"), key=cv_scores.get)

# 2) Leakage check: do absolute plate coordinates help the best model type, or could they leak?
no_position = [f for f in ALL_FEATURES if f not in POSITION_FEATURES]
leakage = {
    "with_position": cv_macro_f1(models[best], ALL_FEATURES),
    "without_position": cv_macro_f1(models[best], no_position),
    "with_y_position": cv_macro_f1(models[best], no_position + ["Y_Minimum", "Y_Maximum"]),
    "position_only": cv_macro_f1(models[best], POSITION_FEATURES),
}
gain = leakage["with_position"] - leakage["without_position"]

# 3) Fit every model on the whole train set, evaluate once on the test set.
X_train, X_test = X_train_all[FEATURES], X_test_all[FEATURES]
test_scores = {}
for name, model in models.items():
    pred = model.fit(X_train, y_train).predict(X_test)
    test_scores[name] = {
        "accuracy": accuracy_score(y_test, pred),
        "macro_f1": f1_score(y_test, pred, average="macro"),
    }

# 4) Test-set confidence table: confidence = highest class probability.
model = models[best]
proba = model.predict_proba(X_test)
predictions = pd.DataFrame({
    "row": X_test.index,  # line number in Faults.NNA (0-based)
    "true_fault": y_test.to_numpy(),
    "predicted_fault": model.classes_[proba.argmax(axis=1)],
    "confidence": proba.max(axis=1).round(4),
})
predictions["correct"] = predictions["true_fault"] == predictions["predicted_fault"]

# --- Report ---
print(f"Train {len(X_train)} / test {len(X_test)} defects, {len(FEATURES)} features\n")
print(f"{'model':<24} {'CV macro F1':>11} {'test acc':>9} {'test macro F1':>14}")
for name in models:
    s = test_scores[name]
    print(f"{name:<24} {cv_scores[name]:11.3f} {s['accuracy']:9.3f} {s['macro_f1']:14.3f}")
print(f"\nBest model (by CV): {best}")
print("\nLeakage check (CV macro F1, " + best + "):")
for name, value in leakage.items():
    print(f"  {name:<17} {value:.3f}")
print(f"  -> gain from position {gain:+.3f}; {POSITION_DECISION}")
print("\nPer-class (test):")
print(classification_report(y_test, predictions["predicted_fault"], digits=2))
print("Threshold what-if (test): auto-classified share and error rate among auto-classified")
for t in [0.5, 0.6, 0.7, 0.8, 0.9]:
    auto = predictions[predictions["confidence"] >= t]
    print(f"  >= {t:.1f}: auto {len(auto) / len(predictions):6.1%}   errors {1 - auto['correct'].mean():6.1%}")

# --- Save ---
MODEL_DIR.mkdir(exist_ok=True)
joblib.dump(model, MODEL_DIR / "model.joblib")
predictions.to_csv(MODEL_DIR / "test_predictions.csv", index=False)
report = classification_report(y_test, predictions["predicted_fault"], output_dict=True)
metrics = {
    "data": "UCI Steel Plates Faults (Buscema, Terzi & Tastle 2010, CC BY 4.0, doi:10.24432/C5J88N)",
    "target": TARGET,
    "classes": list(model.classes_),
    "model": best,
    "features": FEATURES,
    "dropped_features": {
        "TypeOfSteel_A400": "always 1 - TypeOfSteel_A300 (redundant)",
        **{f: "absolute plate position (leakage check)" for f in POSITION_FEATURES if f not in FEATURES},
    },
    "split": {"test_size": 0.2, "stratified": True, "seed": SEED, "n_train": len(X_train), "n_test": len(X_test)},
    "cv_macro_f1": {name: round(value, 3) for name, value in cv_scores.items()},
    "test": {name: {k: round(v, 3) for k, v in s.items()} for name, s in test_scores.items()},
    "leakage_check": {
        "model": best,
        "cv_macro_f1": {name: round(value, 3) for name, value in leakage.items()},
        "gain_from_position": round(gain, 3),
        "decision": POSITION_DECISION,
    },
    "per_class_test": {
        c: {k: round(v, 3) for k, v in report[c].items()} for c in model.classes_
    },
}
(MODEL_DIR / "metrics.json").write_text(json.dumps(metrics, indent=2) + "\n")

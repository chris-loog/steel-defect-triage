"""Load the UCI Steel Plates Faults data (shared by train.py and app.py).

Data: Buscema, Terzi & Tastle (2010), CC BY 4.0, https://doi.org/10.24432/C5J88N - see data/steel_plates/README.md.
"""
from pathlib import Path

import pandas as pd

DATA_DIR = Path(__file__).resolve().parent.parent / "data" / "steel_plates"
COLUMNS = (DATA_DIR / "Faults27x7_var").read_text().split()  # 27 features, then 7 one-hot fault classes
CLASSES = COLUMNS[-7:]
TARGET = "fault"

# Absolute defect coordinates on the plate. train.py checks whether they help or only leak (see metrics.json).
POSITION_FEATURES = ["X_Minimum", "X_Maximum", "Y_Minimum", "Y_Maximum"]
# TypeOfSteel_A400 is always 1 - TypeOfSteel_A300 -> redundant, dropped.
ALL_FEATURES = [c for c in COLUMNS[:-7] if c != "TypeOfSteel_A400"]
# Features the model uses: leakage check result (train.py) = position features dropped.
FEATURES = [c for c in ALL_FEATURES if c not in POSITION_FEATURES]


def load_steel_data() -> tuple[pd.DataFrame, pd.Series]:
    """Return (all candidate features, fault label) for the 1,941 defects. Row index = line number in Faults.NNA."""
    df = pd.read_csv(DATA_DIR / "Faults.NNA", sep="\t", header=None, names=COLUMNS)
    y = df[CLASSES].idxmax(axis=1).rename(TARGET)  # one-hot -> one label column
    return df[ALL_FEATURES], y

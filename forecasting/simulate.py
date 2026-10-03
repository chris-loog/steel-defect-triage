"""Production-line simulator ("digital twin") – generates SIMULATED hourly production output.

No real public hourly production data exists, so this script creates a realistic-looking history
for one production line. Run:  python simulate.py   → writes data/production_hourly.csv
"""

from pathlib import Path

import numpy as np
import pandas as pd

SEED = 42
END = pd.Timestamp("2026-09-30 23:00")
YEARS = 2
NOMINAL_RATE = 120  # units per hour at full speed
SHIFT_EFFICIENCY = {"early": 1.00, "late": 0.97, "night": 0.90}
OUTPUT_PATH = Path(__file__).resolve().parent.parent / "data" / "production_hourly.csv"


def build_time_index() -> pd.DataFrame:
    start = END - pd.DateOffset(years=YEARS) + pd.Timedelta(hours=1)
    return pd.DataFrame({"timestamp": pd.date_range(start, END, freq="h")})


def add_shifts(df: pd.DataFrame) -> pd.DataFrame:
    """Early 06-14, late 14-22, night 22-06. Mon-Fri all three shifts, Saturday early only, Sunday off."""
    hour = df["timestamp"].dt.hour
    df["shift"] = np.select([hour.between(6, 13), hour.between(14, 21)], ["early", "late"], "night")

    # A night shift belongs to the day it starts: 02:00 on Tuesday is Monday's night shift.
    shift_day = df["timestamp"] - pd.to_timedelta((hour < 6).astype(int), unit="D")
    weekday = shift_day.dt.dayofweek  # Monday = 0 ... Sunday = 6
    scheduled = np.where(df["shift"] == "early", weekday <= 5, weekday <= 4)

    df["shift"] = df["shift"].where(scheduled, "none")
    df["status"] = np.where(scheduled, "running", "off")
    df["capacity"] = df["shift"].map(SHIFT_EFFICIENCY).fillna(0.0) * NOMINAL_RATE
    return df


def main() -> None:
    df = build_time_index()
    df = add_shifts(df)
    OUTPUT_PATH.parent.mkdir(exist_ok=True)
    df.to_csv(OUTPUT_PATH, index=False)
    print(f"{len(df)} rows → {OUTPUT_PATH}")


if __name__ == "__main__":
    main()

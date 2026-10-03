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
HANDOVER_FACTOR = 0.85  # first hour of each shift
NOISE_STD = 0.05
STATUS_FACTOR = {"running": 1.0, "changeover": 0.4, "maintenance": 0.0, "breakdown": 0.0, "off": 0.0}
CHANGEOVERS_PER_WEEK = 2
BREAKDOWNS_PER_WEEK = 1.5
PRODUCTS = ["A", "B", "C"]
HOLIDAYS = ["01-01", "05-01", "10-03", "12-24", "12-25", "12-26", "12-31"]
OUTPUT_PATH =Path(__file__).resolve().parent.parent / "data" / "production_hourly.csv"


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


def add_holidays_and_maintenance(df: pd.DataFrame) -> pd.DataFrame:
    """Planned stops: public holidays (line off) and maintenance every 2nd Saturday early shift."""
    holiday = df["timestamp"].dt.strftime("%m-%d").isin(HOLIDAYS)
    df.loc[holiday, ["shift", "status"]] = ["none", "off"]

    ts = df["timestamp"]
    even_week = ts.dt.isocalendar().week % 2 == 0
    maintenance = (ts.dt.dayofweek == 5) & (df["shift"] == "early") & even_week & (df["status"] == "running")
    df.loc[maintenance, "status"] = "maintenance"
    return df


def add_changeovers(df: pd.DataFrame, rng: np.random.Generator) -> pd.DataFrame:
    """~2 product switches per week; the changeover hour runs at reduced speed."""
    running = df.index[df["status"] == "running"]
    p = CHANGEOVERS_PER_WEEK * len(df) / (168 * len(running))  # chance per running hour
    starts = running[rng.random(len(running)) < p]
    df.loc[starts, "status"] = "changeover"

    product = np.empty(len(df), dtype=object)
    current, last = "A", 0
    for idx in starts:  # product stays the same until the next changeover
        product[last:idx] = current
        current = rng.choice([prod for prod in PRODUCTS if prod != current])
        last = idx
    product[last:] = current
    df["product"] = product
    return df


def add_breakdowns(df: pd.DataFrame, rng: np.random.Generator) -> pd.DataFrame:
    """~1.5 unplanned stops per week, 1-4 hours each. Only hit hours where the line was producing."""
    producing = df.index[df["status"].isin(["running", "changeover"])]
    p = BREAKDOWNS_PER_WEEK * len(df) / (168 * len(producing))
    for start in producing[rng.random(len(producing)) < p]:
        hours = df.loc[start : start + rng.integers(1, 5) - 1]
        hit = hours.index[hours["status"].isin(["running", "changeover"])]
        df.loc[hit, "status"] = "breakdown"
    return df


def add_output(df: pd.DataFrame, rng: np.random.Generator) -> pd.DataFrame:
    """units = capacity x status factor x handover dip x noise; scrap is a small random share."""
    factor = df["status"].map(STATUS_FACTOR)
    handover = df["timestamp"].dt.hour.isin([6, 14, 22])
    factor = factor * np.where(handover, HANDOVER_FACTOR, 1.0)
    noise = rng.normal(1.0, NOISE_STD, len(df))
    units = (df["capacity"] * factor * noise).clip(lower=0).round().astype(int)

    # Scrap is higher in the changeover hour and the hour after it (line settling in).
    after_changeover = df["status"].eq("changeover") | df["status"].shift(1).eq("changeover")
    scrap_rate = rng.uniform(0.02, 0.04, len(df)) + np.where(after_changeover, 0.03, 0.0)
    df["units_produced"] = units
    df["scrap_units"] = rng.binomial(units, scrap_rate)
    df["good_units"] = units - df["scrap_units"]
    return df.drop(columns="capacity")


def main() -> None:
    rng = np.random.default_rng(SEED)
    df = build_time_index()
    df = add_shifts(df)
    df = add_holidays_and_maintenance(df)
    df = add_changeovers(df, rng)
    df = add_breakdowns(df, rng)
    df = add_output(df, rng)
    df = df[["timestamp", "shift", "product", "status", "units_produced", "scrap_units", "good_units"]]
    OUTPUT_PATH.parent.mkdir(exist_ok=True)
    df.to_csv(OUTPUT_PATH, index=False)
    print(f"{len(df)} rows → {OUTPUT_PATH}")


if __name__ == "__main__":
    main()

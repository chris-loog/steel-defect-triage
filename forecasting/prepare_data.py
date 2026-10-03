"""Download the UCI Steel Industry Energy Consumption dataset and turn it into a clean hourly CSV.

Source: https://archive.ics.uci.edu/dataset/851/steel+industry+energy+consumption (CC BY 4.0)
"""

import urllib.request
import zipfile
from pathlib import Path

import pandas as pd

ROOT = Path(__file__).resolve().parent.parent
RAW_DIR = ROOT / "data" / "raw"
RAW_ZIP = RAW_DIR / "steel_industry.zip"
URL = "https://archive.ics.uci.edu/static/public/851/steel+industry+energy+consumption.zip"


def download() -> None:
    if RAW_ZIP.exists():
        return
    RAW_DIR.mkdir(parents=True, exist_ok=True)
    urllib.request.urlretrieve(URL, RAW_ZIP)


def load_raw() -> pd.DataFrame:
    with zipfile.ZipFile(RAW_ZIP) as z:
        csv_name = next(n for n in z.namelist() if n.endswith(".csv"))
        with z.open(csv_name) as f:
            return pd.read_csv(f)


if __name__ == "__main__":
    download()
    raw = load_raw()
    print(raw.shape)
    print(raw.head(3).to_string())
    print(raw.tail(2).to_string())

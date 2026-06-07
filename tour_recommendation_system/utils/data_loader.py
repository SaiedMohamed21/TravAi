# =============================================================
# utils/data_loader.py
# Loads and lightly validates the tours dataset
# =============================================================

import pandas as pd
from utils.config import DATA_PATH, FEATURE_COLS, TARGET_COL


def load_dataset(path: str = DATA_PATH) -> pd.DataFrame:
    """
    Load the tours CSV, verify required columns exist, and return a
    clean DataFrame.  No rows are dropped — the dataset is assumed to
    be backend-ready.
    """
    df = pd.read_csv(path)

    # Verify every required column is present
    required = FEATURE_COLS + [TARGET_COL, "base_price_usd", "cluster_label", "city"]
    missing = [c for c in required if c not in df.columns]
    if missing:
        raise ValueError(f"Dataset is missing required columns: {missing}")

    # Convert available_datetime to datetime if present
    if "available_datetime" in df.columns:
        df["available_datetime"] = pd.to_datetime(
            df["available_datetime"], errors="coerce"
        )

    print(f"[DataLoader] Loaded {len(df):,} tours from '{path}'")
    print(f"[DataLoader] Cities    : {sorted(df['city'].unique())}")
    print(f"[DataLoader] Clusters  : {sorted(df['cluster_label'].unique())}")
    print(
        f"[DataLoader] Price range: ${df['base_price_usd'].min():.2f} "
        f"– ${df['base_price_usd'].max():.2f}"
    )
    return df

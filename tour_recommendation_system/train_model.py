# =============================================================
# train_model.py
# Train the RandomForest tour-score regression model and save
# it for later use by the recommendation engine.
#
# Usage:
#   python train_model.py
# =============================================================

import os
import numpy as np
import joblib
import pandas as pd
from sklearn.ensemble import RandomForestRegressor
from sklearn.model_selection import train_test_split
from sklearn.metrics import mean_absolute_error, mean_squared_error, r2_score

from utils.config import (
    DATA_PATH, MODEL_PATH, FEATURE_COLS, TARGET_COL,
    RANDOM_STATE, TEST_SIZE, N_ESTIMATORS, MAX_DEPTH
)
from utils.data_loader import load_dataset
from utils.quality_scorer import add_quality_score


def train_and_save() -> dict:
    """
    Full training pipeline.
    1. Load and prepare data.
    2. Train a RandomForestRegressor on FEATURE_COLS → TARGET_COL.
    3. Evaluate on the test split.
    4. Save the model to MODEL_PATH.

    Returns
    -------
    dict with keys: mae, rmse, r2, feature_importances
    """
    print("=" * 60)
    print("  Tour Recommendation System — Model Training")
    print("=" * 60)

    # ── 1. Load data ───────────────────────────────────────────
    df = load_dataset(DATA_PATH)
    df = add_quality_score(df)   # adds quality_score column for reference

    X = df[FEATURE_COLS].values
    y = df[TARGET_COL].values

    print(f"\n[Training] Features : {FEATURE_COLS}")
    print(f"[Training] Target   : {TARGET_COL}")
    print(f"[Training] Dataset  : {len(df)} rows")

    # ── 2. Train / test split ──────────────────────────────────
    X_train, X_test, y_train, y_test = train_test_split(
        X, y, test_size=TEST_SIZE, random_state=RANDOM_STATE
    )
    print(f"[Training] Train: {len(X_train)}, Test: {len(X_test)}")

    # ── 3. Train model ─────────────────────────────────────────
    print(f"\n[Training] Fitting RandomForestRegressor ...")
    model = RandomForestRegressor(
        n_estimators=N_ESTIMATORS,
        max_depth=MAX_DEPTH,
        random_state=RANDOM_STATE,
        n_jobs=-1,
    )
    model.fit(X_train, y_train)

    # ── 4. Evaluate ────────────────────────────────────────────
    y_pred = model.predict(X_test)
    mae  = mean_absolute_error(y_test, y_pred)
    rmse = np.sqrt(mean_squared_error(y_test, y_pred))
    r2   = r2_score(y_test, y_pred)

    print("\n[Evaluation] Results on hold-out test set:")
    print(f"  MAE  : {mae:.6f}")
    print(f"  RMSE : {rmse:.6f}")
    print(f"  R²   : {r2:.6f}")

    # ── 5. Feature importances ─────────────────────────────────
    importances = dict(zip(FEATURE_COLS, model.feature_importances_))
    print("\n[Feature Importances]")
    for feat, imp in sorted(importances.items(), key=lambda x: -x[1]):
        bar = "█" * int(imp * 40)
        print(f"  {feat:<25} {imp:.4f}  {bar}")

    # ── 6. Save model ──────────────────────────────────────────
    os.makedirs(os.path.dirname(MODEL_PATH), exist_ok=True)
    joblib.dump(model, MODEL_PATH)
    print(f"\n[Training] Model saved → {MODEL_PATH}")

    return {
        "mae": mae,
        "rmse": rmse,
        "r2": r2,
        "feature_importances": importances,
        "model": model,
    }


if __name__ == "__main__":
    train_and_save()

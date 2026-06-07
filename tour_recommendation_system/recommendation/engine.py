# =============================================================
# recommendation/engine.py
# Initial recommendation engine.
#
# Philosophy
# ----------
# The very first recommendation should be the BEST VALUE option:
# the tour that offers the highest quality experience relative to
# its price.  We do not jump to luxury immediately — we behave
# like a smart, budget-conscious traveller who wants the most
# rewarding experience per dollar spent.
#
# value_score = VALUE_SCORE_QUALITY_WEIGHT × quality_score
#             + VALUE_SCORE_PRICE_PENALTY  × price_score
#
# price_score is already normalised so that higher = cheaper
# relative to peers.
# =============================================================

import pandas as pd
import joblib

from utils.config import (
    MODEL_PATH, FEATURE_COLS,
    VALUE_SCORE_QUALITY_WEIGHT, VALUE_SCORE_PRICE_PENALTY,
)
from utils.quality_scorer import add_quality_score


def load_model():
    """Load the trained RandomForest model from disk."""
    return joblib.load(MODEL_PATH)


def prepare_dataframe(df: pd.DataFrame, model=None) -> pd.DataFrame:
    """
    Add quality_score and (optionally) ml_predicted_score columns.
    Returns a new DataFrame with these columns added.
    """
    df = add_quality_score(df)

    if model is not None:
        X = df[FEATURE_COLS].values
        df["ml_predicted_score"] = model.predict(X)
    else:
        df["ml_predicted_score"] = df["tour_score"]  # fallback

    # Value score: high quality, affordable price
    df["value_score"] = (
        VALUE_SCORE_QUALITY_WEIGHT * df["quality_score"]
        + VALUE_SCORE_PRICE_PENALTY * df["price_score"]
    )

    return df


def get_initial_recommendation(
    df: pd.DataFrame,
    city: str = None,
    cluster: str = None,
    max_budget: float = None,
    model=None,
) -> pd.Series:
    """
    Return the single best value-oriented tour matching the constraints.

    Parameters
    ----------
    df         : full prepared DataFrame (must have value_score column)
    city       : filter by city name (optional)
    cluster    : filter by cluster label, e.g. 'Economy' (optional)
    max_budget : maximum price in USD (optional)
    model      : trained ML model (for scoring, optional)

    Returns
    -------
    pd.Series — the recommended tour row
    """
    candidates = df.copy()

    # Apply filters
    if city:
        candidates = candidates[candidates["city"] == city]
    if cluster:
        candidates = candidates[candidates["cluster_label"] == cluster]
    if max_budget is not None:
        candidates = candidates[candidates["base_price_usd"] <= max_budget]

    if candidates.empty:
        raise ValueError(
            f"No tours match the given constraints "
            f"(city={city}, cluster={cluster}, max_budget={max_budget})."
        )

    # Sort by value_score descending → pick top
    best = candidates.sort_values("value_score", ascending=False).iloc[0]
    return best

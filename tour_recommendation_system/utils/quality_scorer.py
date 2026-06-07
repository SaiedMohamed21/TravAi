# =============================================================
# utils/quality_scorer.py
# Computes a price-independent quality score for every tour.
#
# Philosophy
# ----------
# Price is intentionally excluded.  We want to measure HOW GOOD
# a tour experience is, not how cheap it is.  A cheap tour can
# still be excellent; an expensive one can be mediocre.
#
# The quality_score is a weighted sum of experience-focused
# sub-scores.  Weights are defined in utils/config.py and
# explained there.
# =============================================================

import pandas as pd
from utils.config import QUALITY_WEIGHTS


def compute_quality_scores(df: pd.DataFrame) -> pd.Series:
    """
    Compute a single quality_score (0–1) for every row in df.

    The score is a weighted average of the columns listed in
    QUALITY_WEIGHTS.  All source columns are already normalised
    to [0, 1] in the backend-ready dataset.

    Parameters
    ----------
    df : pd.DataFrame
        The full tours DataFrame.

    Returns
    -------
    pd.Series
        quality_score for each row, index aligned with df.
    """
    score = pd.Series(0.0, index=df.index)

    for col, weight in QUALITY_WEIGHTS.items():
        if col not in df.columns:
            print(f"[QualityScorer] WARNING: column '{col}' not found – skipping.")
            continue
        score += df[col] * weight

    # Clip to [0, 1] as a safety measure
    score = score.clip(0.0, 1.0)
    return score


def add_quality_score(df: pd.DataFrame) -> pd.DataFrame:
    """
    Convenience wrapper: adds a 'quality_score' column to df in place
    and returns the modified DataFrame.
    """
    df = df.copy()
    df["quality_score"] = compute_quality_scores(df)
    return df

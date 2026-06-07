# =============================================================
# regeneration/regenerator.py
# Progressive regeneration engine — the heart of the system.
#
# Philosophy (mirrors the Flight recommendation system)
# ────────────────────────────────────────────────────────
# On each "regenerate" call the user signals:
#   "I'm willing to spend a bit more if the experience is
#    meaningfully better."
#
# We therefore:
# 1. Search ONLY among tours MORE expensive than the current one
#    (no downgrade, no lateral move).
# 2. Compute delta_quality = candidate.quality − current.quality
# 3. Apply a price-jump penalty to discourage wild price leaps.
# 4. Rank candidates by a net_upgrade_score.
# 5. Accept the winner only if net_upgrade_score ≥ dynamic_threshold.
# 6. If nothing qualifies → return None (caller shows "no upgrade").
#
# Dynamic threshold rises slightly with each regeneration to
# ensure each successive upgrade is meaningful, not cosmetic.
# =============================================================

import pandas as pd
from typing import Optional

from utils.config import (
    REGEN_BASE_THRESHOLD,
    REGEN_PRICE_JUMP_PENALTY,
    REGEN_MAX_PRICE_MULT,
    DIVERSITY_CLUSTER_PENALTY,
    DIVERSITY_CITY_PENALTY,
)


def compute_dynamic_threshold(regen_count: int) -> float:
    """
    The minimum net_upgrade_score required to accept a new recommendation.
    Grows slightly with each regeneration so improvements must be real.

    regen_count = 0 → first regeneration → threshold = base
    regen_count = 1 → second            → threshold = base × 1.10
    ...
    """
    return REGEN_BASE_THRESHOLD * (1.0 + 0.10 * regen_count)


def compute_price_jump_penalty(
    current_price: float,
    candidate_price: float,
    price_range: float = 330.53,   # dataset max − min ≈ 345.62 − 15.09
) -> float:
    """
    Penalise large price jumps using a dataset-range-normalised delta.

    Formula:
        penalty = REGEN_PRICE_JUMP_PENALTY × (Δprice / price_range)

    This ensures the penalty is always in [0, REGEN_PRICE_JUMP_PENALTY],
    regardless of how cheap the current tour is.  A $300 price jump on a
    $330 range → max penalty 0.20.  A $50 jump → penalty ≈ 0.030.

    Why not ratio-based?
    A ratio-based formula crushes candidates when the starting price is
    very low (e.g. $18 → $90 gets ×4 ratio penalty), even though $90 is
    perfectly reasonable.  The range-based formula is proportional to the
    absolute price step taken, not to how cheap the origin was.
    """
    price_delta = max(0.0, candidate_price - current_price)
    penalty = REGEN_PRICE_JUMP_PENALTY * (price_delta / max(price_range, 1.0))
    return penalty


def regenerate(
    df: pd.DataFrame,
    current: pd.Series,
    session_history: list,          # list of tour_id already shown
    city: str = None,
    cluster: str = None,
    city_budget: float = None,      # hard budget cap for this city
    regen_count: int = 0,           # how many times user has regenerated
) -> Optional[pd.Series]:
    """
    Find the best progressive upgrade over the current recommendation.

    Parameters
    ----------
    df             : full prepared DataFrame with quality_score column
    current        : the currently displayed tour (pd.Series)
    session_history: list of tour_id values already shown this session
    city           : filter candidates to this city (optional)
    cluster        : filter candidates to this cluster (optional)
    city_budget    : hard price ceiling from Local Budget Divider (optional)
    regen_count    : number of previous regenerations (for threshold scaling)

    Returns
    -------
    pd.Series of the upgrade, or None if no meaningful upgrade exists
    """
    current_price   = current["base_price_usd"]
    current_quality = current["quality_score"]
    current_cluster = current.get("cluster_label", "")
    current_city    = current.get("city", "")

    # Compute the price range for this candidate pool (used in penalty)
    pool_price_range = df["base_price_usd"].max() - df["base_price_usd"].min()
    if pool_price_range < 1.0:
        pool_price_range = 1.0

    # ── 1. Build candidate pool ────────────────────────────────
    candidates = df.copy()

    # Must be more expensive than current (we are looking for an upgrade)
    candidates = candidates[candidates["base_price_usd"] > current_price]

    # Hard cap: candidate must not exceed REGEN_MAX_PRICE_MULT × the current price
    candidates = candidates[
        candidates["base_price_usd"] <= current_price * REGEN_MAX_PRICE_MULT
    ]

    # Respect Local Budget Divider city ceiling
    if city_budget is not None:
        candidates = candidates[candidates["base_price_usd"] <= city_budget]

    # Respect city / cluster filters (if explicitly set)
    if city:
        candidates = candidates[candidates["city"] == city]
    if cluster:
        candidates = candidates[candidates["cluster_label"] == cluster]

    # Exclude tours already shown in this session
    if session_history:
        candidates = candidates[~candidates["tour_id"].isin(session_history)]

    if candidates.empty:
        return None

    # ── 2. Score each candidate ────────────────────────────────
    threshold = compute_dynamic_threshold(regen_count)
    results   = []

    for _, row in candidates.iterrows():
        delta_quality     = row["quality_score"] - current_quality
        price_jump_penalty = compute_price_jump_penalty(
            current_price, row["base_price_usd"], pool_price_range
        )

        # Diversity adjustments
        diversity_penalty = 0.0
        if row["cluster_label"] == current_cluster:
            diversity_penalty += DIVERSITY_CLUSTER_PENALTY
        if row["city"] == current_city:
            diversity_penalty += DIVERSITY_CITY_PENALTY

        # Net upgrade score
        net_upgrade = delta_quality - price_jump_penalty - diversity_penalty

        results.append({
            "tour_id":          row["tour_id"],
            "delta_quality":    delta_quality,
            "price_jump_pen":   price_jump_penalty,
            "diversity_pen":    diversity_penalty,
            "net_upgrade":      net_upgrade,
            "_idx":             row.name,
        })

    results_df = pd.DataFrame(results)

    # ── 3. Filter by threshold ─────────────────────────────────
    qualified = results_df[results_df["net_upgrade"] >= threshold]

    if qualified.empty:
        return None

    # ── 4. Pick the best upgrade ───────────────────────────────
    best_idx = qualified.sort_values("net_upgrade", ascending=False).iloc[0]["_idx"]
    return df.loc[int(best_idx)]

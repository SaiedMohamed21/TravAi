# =============================================================
# utils/config.py
# Central configuration for the Tour Recommendation System
# =============================================================

import os

# ── Paths ─────────────────────────────────────────────────────
BASE_DIR   = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DATA_PATH  = os.path.join(BASE_DIR, "data", "tours_dataset_backend_ready.csv")
MODEL_PATH = os.path.join(BASE_DIR, "models", "tour_rf_model.joblib")
CHART_DIR  = os.path.join(BASE_DIR, "charts")

# ── ML Model ──────────────────────────────────────────────────
RANDOM_STATE  = 42
TEST_SIZE     = 0.20          # 80/20 train-test split
N_ESTIMATORS  = 200
MAX_DEPTH     = 12

# ── Feature columns used as model input ───────────────────────
FEATURE_COLS = [
    "price_score",
    "rating_score",
    "reviews_score",
    "experience_score",
    "duration_score",
    "transport_score",
    "meal_score",
    "senior_guide_score",
    "rating_bonus",
]

TARGET_COL = "tour_score"

# ── Quality-score weights (price excluded) ────────────────────
# Weights sum to 1.0.
# rating_score  : 25% – most reliable quality signal
# reviews_score : 20% – crowd-validated credibility
# experience_s  : 20% – depth of the tourist experience
# duration_s    : 10% – longer tours offer more value
# transport_s   : 10% – convenience matters
# meal_s        : 8%  – comfort during the tour
# senior_guide  : 7%  – expertise indicator
QUALITY_WEIGHTS = {
    "rating_score":       0.25,
    "reviews_score":      0.20,
    "experience_score":   0.20,
    "duration_score":     0.10,
    "transport_score":    0.10,
    "meal_score":         0.08,
    "senior_guide_score": 0.07,
}

# ── Recommendation behaviour ───────────────────────────────────
VALUE_SCORE_PRICE_PENALTY = 0.3   # weight of price_score in initial value mix
VALUE_SCORE_QUALITY_WEIGHT = 0.7  # weight of quality_score in initial value mix

# ── Regeneration thresholds ────────────────────────────────────
REGEN_BASE_THRESHOLD     = 0.015  # minimum quality delta to accept an upgrade
REGEN_PRICE_JUMP_PENALTY = 0.20   # penalty coefficient per 100 % price increase
REGEN_MAX_PRICE_MULT     = 8.0    # hard cap: candidate must be ≤ 8× current price

# ── Diversity penalty ──────────────────────────────────────────
DIVERSITY_CLUSTER_PENALTY   = 0.10  # subtracted if same cluster seen before
DIVERSITY_CITY_PENALTY      = 0.05  # subtracted if same city seen twice in session

# ── Budget divider ─────────────────────────────────────────────
# Default city budget allocations as fractions of total trip budget.
# These can be overridden at runtime.
DEFAULT_CITY_BUDGET_FRACTIONS = {
    "Cairo":           0.25,
    "Luxor":           0.20,
    "Aswan":           0.20,
    "Hurghada":        0.20,
    "Sharm El Sheikh": 0.15,
}

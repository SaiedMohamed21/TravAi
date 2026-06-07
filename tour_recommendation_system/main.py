# =============================================================
# main.py
# Master orchestration script for the Tour Recommendation System.
#
# Runs the full pipeline end-to-end:
#   1. Load and inspect the dataset
#   2. Train (or reload) the ML model
#   3. Show city-level best-value recommendations
#   4. Run a full progressive regeneration demo (Economy → Luxury)
#   5. Produce all academic charts
#
# Usage:
#   python main.py
#   python main.py --no-train
#   python main.py --budget 1200
# =============================================================

import argparse
import os

from utils.config import MODEL_PATH, DEFAULT_CITY_BUDGET_FRACTIONS
from utils.data_loader import load_dataset
from recommendation.engine import load_model, prepare_dataframe, get_initial_recommendation
from regeneration.regenerator import regenerate
from regeneration.session import RecommendationSession
from validation.evaluator import print_recommendation_table, print_regeneration_comparison
from charts.visualizer import generate_all_charts


def run_full_pipeline(
    skip_train: bool = False,
    budget: float = 1000.0,
) -> None:

    print("\n" + "█" * 60)
    print("  TOUR RECOMMENDATION SYSTEM — Full Pipeline")
    print("█" * 60)

    # ── Step 1: Train or load model ────────────────────────────
    if skip_train and os.path.exists(MODEL_PATH):
        print("\n[Pipeline] Loading saved model ...")
        model = load_model()
        importances = {}
    else:
        from train_model import train_and_save
        result      = train_and_save()
        model       = result["model"]
        importances = result["feature_importances"]

    # ── Step 2: Prepare data ───────────────────────────────────
    df = load_dataset()
    df = prepare_dataframe(df, model)
    print(f"\n[Pipeline] Data enriched with quality_score and value_score.")

    # ── SECTION A: Best city-level value picks ─────────────────
    print("\n" + "=" * 70)
    print("  SECTION A — Best Value-For-Money Pick Per City")
    print("  (within allocated city budget)")
    print("=" * 70)
    print(f"\n  Total trip budget: ${budget:.2f}")

    cities = sorted(df["city"].unique())
    all_initials = {}

    for c in cities:
        city_frac   = DEFAULT_CITY_BUDGET_FRACTIONS.get(c, 0.20)
        city_budget = budget * city_frac
        try:
            best = get_initial_recommendation(df, city=c, max_budget=city_budget, model=model)
            all_initials[c] = best
            print(
                f"\n  ▸ {c:<20} budget=${city_budget:.0f}  →  "
                f"{str(best.get('tour_title',''))[:35]:<35} "
                f"${best['base_price_usd']:.2f}  Q={best.get('quality_score',0):.3f}"
            )
        except ValueError as e:
            print(f"  ▸ {c:<20} ✗ {e}")

    # ── SECTION B: Progressive Regeneration Demo ───────────────
    # Start from the most affordable cluster in Cairo, then
    # let regeneration walk up to better experiences.
    print("\n\n" + "=" * 70)
    print("  SECTION B — Progressive Regeneration Demo")
    print("  (Cairo: Economy → Premium → Luxury journey)")
    print("=" * 70)

    demo_budget = budget * 0.40   # 40% of total budget for this demo
    demo_session = RecommendationSession(city="Cairo", city_budget=demo_budget)

    try:
        # Force Economy start so we can demonstrate the full upgrade journey
        initial = get_initial_recommendation(
            df, city="Cairo", cluster="Economy", max_budget=demo_budget, model=model
        )
    except ValueError:
        # Fallback: no cluster restriction
        initial = get_initial_recommendation(
            df, city="Cairo", max_budget=demo_budget, model=model
        )

    demo_session.record(initial)
    print_recommendation_table(initial, title="✦ Initial Recommendation (Economy)")

    for step in range(1, 5):
        upgrade = regenerate(
            df=df,
            current=demo_session.current,
            session_history=list(demo_session.shown_ids),
            city="Cairo",
            cluster=None,             # allow cross-cluster upgrades
            city_budget=demo_budget,
            regen_count=step - 1,
        )
        if upgrade is None:
            print(
                f"\n  ✗ Regeneration #{step}: No better tour found within budget."
                "\n    Keeping current recommendation."
            )
            break
        demo_session.record(upgrade)
        demo_session.increment_regen()
        print_recommendation_table(upgrade, title=f"✦ Upgrade #{step}")

    demo_session.print_history()
    print_regeneration_comparison(demo_session.history)

    # ── Step 5: Generate all charts ────────────────────────────
    generate_all_charts(df, importances or {}, history=demo_session.history)
    print("\n[Pipeline] ✓ All steps complete. Charts saved to /charts/")


def main() -> None:
    parser = argparse.ArgumentParser(
        description="Tour Recommendation System — Master Pipeline"
    )
    parser.add_argument("--no-train", action="store_true",
                        help="Skip training and load saved model")
    parser.add_argument("--budget", default=1000.0, type=float,
                        help="Total trip budget in USD (default: 1000)")
    args = parser.parse_args()

    run_full_pipeline(
        skip_train=args.no_train,
        budget=args.budget,
    )


if __name__ == "__main__":
    main()

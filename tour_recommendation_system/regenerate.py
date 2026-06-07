# =============================================================
# regenerate.py
# Demonstrates the full progressive regeneration pipeline.
#
# Usage:
#   python regenerate.py
#   python regenerate.py --city Cairo --budget 200 --regen 4
# =============================================================

import argparse
import sys

from utils.data_loader import load_dataset
from utils.quality_scorer import add_quality_score
from recommendation.engine import load_model, prepare_dataframe, get_initial_recommendation
from regeneration.regenerator import regenerate
from regeneration.session import RecommendationSession
from validation.evaluator import print_recommendation_table, print_regeneration_comparison
from charts.visualizer import plot_regeneration_journey


def run_regeneration(
    city: str = None,
    cluster: str = None,
    budget: float = None,
    n_regen: int = 3,
) -> None:
    print("=" * 60)
    print("  Tour Recommendation System — Regeneration Demo")
    print("=" * 60)

    # ── Load and prepare data ──────────────────────────────────
    df    = load_dataset()
    model = load_model()
    df    = prepare_dataframe(df, model)

    # ── Start session ──────────────────────────────────────────
    session = RecommendationSession(
        city=city,
        cluster=cluster,
        city_budget=budget,
    )

    # ── Initial recommendation ─────────────────────────────────
    try:
        initial = get_initial_recommendation(
            df,
            city=city,
            cluster=cluster,
            max_budget=budget,
            model=model,
        )
    except ValueError as e:
        print(f"\n[ERROR] {e}")
        sys.exit(1)

    session.record(initial)
    print_recommendation_table(initial, title="✦ Initial Recommendation")

    # ── Progressive regeneration loop ──────────────────────────
    for step in range(1, n_regen + 1):
        print(f"\n{'─' * 60}")
        print(f"  Regeneration #{step}  (threshold = {0.02 * (1 + 0.10 * (step-1)):.4f})")
        print("─" * 60)

        upgrade = regenerate(
            df=df,
            current=session.current,
            session_history=list(session.shown_ids),
            city=city,
            cluster=None,          # allow cross-cluster upgrades (Economy → Premium → Luxury)
            city_budget=budget,
            regen_count=step - 1,
        )

        if upgrade is None:
            print(
                "\n  ✗ No better tour package found within your budget.\n"
                "    Keeping current recommendation."
            )
            break

        session.record(upgrade)
        session.increment_regen()
        print_recommendation_table(upgrade, title=f"✦ Upgrade #{step}")

    # ── Final summary ──────────────────────────────────────────
    session.print_history()
    print_regeneration_comparison(session.history)

    # ── Regeneration chart ─────────────────────────────────────
    if len(session.history) >= 2:
        plot_regeneration_journey(session.history)


def main() -> None:
    parser = argparse.ArgumentParser(
        description="Tour Recommendation System — Progressive Regeneration"
    )
    parser.add_argument("--city",    default=None,
                        help="Filter by city name, e.g. 'Cairo'")
    parser.add_argument("--cluster", default=None,
                        help="Filter by cluster: Economy | Premium | Luxury")
    parser.add_argument("--budget",  default=None, type=float,
                        help="Maximum tour price in USD")
    parser.add_argument("--regen",   default=3, type=int,
                        help="Number of regeneration steps (default: 3)")
    args = parser.parse_args()

    run_regeneration(
        city=args.city,
        cluster=args.cluster,
        budget=args.budget,
        n_regen=args.regen,
    )


if __name__ == "__main__":
    main()

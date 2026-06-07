# =============================================================
# validation/evaluator.py
# Produces validation tables and regeneration comparison reports.
# =============================================================

import pandas as pd
from tabulate import tabulate
from typing import List


def format_tour_row(tour: pd.Series, label: str = "") -> dict:
    """Extract display-friendly fields from a tour Series."""
    return {
        "Label":        label,
        "Tour":         str(tour.get("tour_title", ""))[:40],
        "City":         tour.get("city", ""),
        "Cluster":      tour.get("cluster_label", ""),
        "Price (USD)":  f"${tour['base_price_usd']:.2f}",
        "Rating":       f"{tour.get('rating', 0):.1f}",
        "Duration (h)": f"{tour.get('duration_hours', 0):.1f}",
        "Quality":      f"{tour.get('quality_score', 0):.4f}",
        "Value Score":  f"{tour.get('value_score', 0):.4f}",
        "Tour Score":   f"{tour.get('tour_score', 0):.4f}",
    }


def print_recommendation_table(tour: pd.Series, title: str = "Recommendation") -> None:
    """Pretty-print a single tour recommendation."""
    row = format_tour_row(tour, title)
    print("\n" + "─" * 70)
    print(f"  {title}")
    print("─" * 70)
    for k, v in row.items():
        if k == "Label":
            continue
        print(f"  {k:<20}: {v}")
    if "tour_description" in tour and pd.notna(tour["tour_description"]):
        desc = str(tour["tour_description"])
        print(f"  {'Description':<20}: {desc[:80]}{'...' if len(desc) > 80 else ''}")
    print("─" * 70)


def print_regeneration_comparison(
    history: List[pd.Series],
) -> None:
    """
    Print a comparison table of all tours shown during regeneration.
    Shows: Initial → Upgrade 1 → Upgrade 2 → ...
    Also shows delta_quality and price change between steps.
    """
    print("\n" + "=" * 80)
    print("  Regeneration Comparison Table")
    print("=" * 80)

    rows = []
    for i, tour in enumerate(history):
        label = "Initial" if i == 0 else f"Upgrade {i}"
        if i == 0:
            delta_q = "—"
            delta_p = "—"
        else:
            dq = tour.get("quality_score", 0) - history[i-1].get("quality_score", 0)
            dp = tour["base_price_usd"] - history[i-1]["base_price_usd"]
            delta_q = f"+{dq:.4f}" if dq >= 0 else f"{dq:.4f}"
            delta_p = f"+${dp:.2f}" if dp >= 0 else f"-${abs(dp):.2f}"

        rows.append({
            "Step":         label,
            "Tour":         str(tour.get("tour_title", ""))[:35],
            "Cluster":      tour.get("cluster_label", ""),
            "Price":        f"${tour['base_price_usd']:.2f}",
            "ΔPrice":       delta_p,
            "Quality":      f"{tour.get('quality_score', 0):.4f}",
            "ΔQuality":     delta_q,
            "Tour Score":   f"{tour.get('tour_score', 0):.4f}",
        })

    print(tabulate(rows, headers="keys", tablefmt="rounded_outline"))
    print()

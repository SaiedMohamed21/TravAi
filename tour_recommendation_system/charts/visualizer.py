# =============================================================
# charts/visualizer.py
# Academic-style visualisation for the Tour Recommendation System.
# Produces IEEE-paper quality charts saved to /charts/.
# =============================================================

import os
import numpy as np
import pandas as pd
import matplotlib
matplotlib.use("Agg")          # non-interactive backend — safe for scripts
import matplotlib.pyplot as plt
import matplotlib.gridspec as gridspec
from matplotlib.ticker import MaxNLocator

from utils.config import CHART_DIR

# ── Global style ───────────────────────────────────────────────
plt.rcParams.update({
    "font.family":         "serif",
    "font.size":           10,
    "axes.titlesize":      11,
    "axes.labelsize":      10,
    "xtick.labelsize":     9,
    "ytick.labelsize":     9,
    "legend.fontsize":     9,
    "axes.spines.top":     False,
    "axes.spines.right":   False,
    "axes.grid":           True,
    "grid.alpha":          0.3,
    "grid.linestyle":      "--",
    "figure.dpi":          150,
    "savefig.bbox":        "tight",
    "savefig.pad_inches":  0.15,
})

CLUSTER_COLORS = {
    "Economy": "#2E86AB",
    "Premium": "#F4A261",
    "Luxury":  "#E76F51",
}


def _save(fig, filename: str) -> str:
    os.makedirs(CHART_DIR, exist_ok=True)
    path = os.path.join(CHART_DIR, filename)
    fig.savefig(path)
    plt.close(fig)
    print(f"[Chart] Saved → {path}")
    return path


# ─────────────────────────────────────────────────────────────
# 1. Feature Importance Bar Chart
# ─────────────────────────────────────────────────────────────
def plot_feature_importance(importances: dict) -> str:
    """
    Horizontal bar chart of RandomForest feature importances.
    """
    features = list(importances.keys())
    values   = list(importances.values())

    sorted_pairs = sorted(zip(values, features))
    values, features = zip(*sorted_pairs)

    fig, ax = plt.subplots(figsize=(7, 4))
    colors = plt.cm.Blues(np.linspace(0.35, 0.85, len(features)))
    bars = ax.barh(features, values, color=colors, edgecolor="white", linewidth=0.6)

    # Annotate bars
    for bar, val in zip(bars, values):
        ax.text(
            val + 0.002, bar.get_y() + bar.get_height() / 2,
            f"{val:.4f}", va="center", ha="left", fontsize=8
        )

    ax.set_xlabel("Relative Importance")
    ax.set_title("Fig. 1 — RandomForest Feature Importances\n(Tour Score Prediction Model)")
    ax.set_xlim(0, max(values) * 1.20)
    fig.tight_layout()
    return _save(fig, "01_feature_importance.png")


# ─────────────────────────────────────────────────────────────
# 2. Quality Score vs Price Scatter
# ─────────────────────────────────────────────────────────────
def plot_quality_vs_price(df: pd.DataFrame) -> str:
    """
    Scatter plot of quality_score vs base_price_usd coloured by cluster.
    """
    fig, ax = plt.subplots(figsize=(7, 5))

    for cluster, grp in df.groupby("cluster_label"):
        ax.scatter(
            grp["base_price_usd"],
            grp["quality_score"],
            label=cluster,
            alpha=0.55,
            s=18,
            color=CLUSTER_COLORS.get(cluster, "#888"),
            edgecolors="none",
        )

    ax.set_xlabel("Base Price (USD)")
    ax.set_ylabel("Quality Score (price-independent)")
    ax.set_title("Fig. 2 — Quality Score vs. Price by Market Segment")
    ax.legend(title="Cluster", framealpha=0.7)
    fig.tight_layout()
    return _save(fig, "02_quality_vs_price.png")


# ─────────────────────────────────────────────────────────────
# 3. Cluster Distribution
# ─────────────────────────────────────────────────────────────
def plot_cluster_distribution(df: pd.DataFrame) -> str:
    """
    Bar chart showing number of tours per cluster.
    """
    counts = df["cluster_label"].value_counts().reindex(
        ["Economy", "Premium", "Luxury"], fill_value=0
    )
    fig, ax = plt.subplots(figsize=(5, 4))
    colors  = [CLUSTER_COLORS[c] for c in counts.index]
    ax.bar(counts.index, counts.values, color=colors, edgecolor="white", linewidth=0.8)

    for i, (label, val) in enumerate(zip(counts.index, counts.values)):
        ax.text(i, val + 5, str(val), ha="center", va="bottom", fontsize=9)

    ax.set_ylabel("Number of Tours")
    ax.set_title("Fig. 3 — Tour Distribution by Market Cluster")
    ax.yaxis.set_major_locator(MaxNLocator(integer=True))
    fig.tight_layout()
    return _save(fig, "03_cluster_distribution.png")


# ─────────────────────────────────────────────────────────────
# 4. City-wise Average Quality vs Average Price
# ─────────────────────────────────────────────────────────────
def plot_city_overview(df: pd.DataFrame) -> str:
    """
    Grouped bar chart: average quality_score and normalised price per city.
    """
    city_stats = df.groupby("city").agg(
        avg_quality=("quality_score", "mean"),
        avg_price=("base_price_usd", "mean"),
    ).reset_index()

    # Normalise price to [0,1] for visual comparison
    price_norm = (city_stats["avg_price"] - city_stats["avg_price"].min()) / (
        city_stats["avg_price"].max() - city_stats["avg_price"].min() + 1e-9
    )
    city_stats["price_norm"] = price_norm

    x   = np.arange(len(city_stats))
    w   = 0.35
    fig, ax = plt.subplots(figsize=(7, 4))

    ax.bar(x - w/2, city_stats["avg_quality"], w, label="Avg Quality Score",
           color="#2E86AB", edgecolor="white")
    ax.bar(x + w/2, city_stats["price_norm"],  w, label="Avg Price (normalised)",
           color="#F4A261", edgecolor="white")

    ax.set_xticks(x)
    ax.set_xticklabels(city_stats["city"], rotation=15, ha="right")
    ax.set_ylabel("Score")
    ax.set_title("Fig. 4 — City-wise Average Quality Score vs. Normalised Price")
    ax.legend(framealpha=0.7)
    fig.tight_layout()
    return _save(fig, "04_city_overview.png")


# ─────────────────────────────────────────────────────────────
# 5. Regeneration Journey Chart
# ─────────────────────────────────────────────────────────────
def plot_regeneration_journey(history: list) -> str:
    """
    Line chart showing how quality and price evolve across regeneration steps.

    history : list of pd.Series (one per recommendation step)
    """
    if len(history) < 2:
        print("[Chart] Regeneration journey needs at least 2 steps — skipped.")
        return ""

    steps    = [f"{'Init' if i == 0 else f'Regen {i}'}" for i in range(len(history))]
    qualities = [h.get("quality_score", 0) for h in history]
    prices    = [h["base_price_usd"] for h in history]

    fig, ax1 = plt.subplots(figsize=(7, 4))
    ax2 = ax1.twinx()

    color_q = "#2E86AB"
    color_p = "#E76F51"

    ax1.plot(steps, qualities, marker="o", color=color_q, linewidth=2,
             markersize=7, label="Quality Score")
    ax1.set_ylabel("Quality Score", color=color_q)
    ax1.tick_params(axis="y", labelcolor=color_q)

    ax2.plot(steps, prices, marker="s", color=color_p, linewidth=2,
             linestyle="--", markersize=7, label="Price (USD)")
    ax2.set_ylabel("Price (USD)", color=color_p)
    ax2.tick_params(axis="y", labelcolor=color_p)

    ax1.set_title("Fig. 5 — Progressive Regeneration: Quality vs. Price Evolution")
    ax1.set_xlabel("Recommendation Step")

    lines1, labels1 = ax1.get_legend_handles_labels()
    lines2, labels2 = ax2.get_legend_handles_labels()
    ax1.legend(lines1 + lines2, labels1 + labels2, loc="lower right", framealpha=0.7)

    fig.tight_layout()
    return _save(fig, "05_regeneration_journey.png")


# ─────────────────────────────────────────────────────────────
# 6. Tour Score Distribution
# ─────────────────────────────────────────────────────────────
def plot_score_distribution(df: pd.DataFrame) -> str:
    """
    Histogram of tour_score coloured by cluster using stacked bars.
    """
    bins = np.linspace(df["tour_score"].min(), df["tour_score"].max(), 30)
    fig, ax = plt.subplots(figsize=(7, 4))

    for cluster in ["Economy", "Premium", "Luxury"]:
        grp = df[df["cluster_label"] == cluster]["tour_score"]
        ax.hist(grp, bins=bins, alpha=0.70,
                label=cluster, color=CLUSTER_COLORS[cluster], edgecolor="white",
                linewidth=0.4)

    ax.set_xlabel("Tour Score")
    ax.set_ylabel("Frequency")
    ax.set_title("Fig. 6 — Tour Score Distribution by Market Cluster")
    ax.legend(title="Cluster", framealpha=0.7)
    fig.tight_layout()
    return _save(fig, "06_score_distribution.png")


# ─────────────────────────────────────────────────────────────
# Generate all charts at once
# ─────────────────────────────────────────────────────────────
def generate_all_charts(
    df: pd.DataFrame,
    importances: dict,
    history: list = None,
) -> None:
    """
    Convenience function: generate every standard chart in one call.
    """
    print("\n[Charts] Generating academic visualisations ...")
    if importances:
        plot_feature_importance(importances)
    plot_quality_vs_price(df)
    plot_cluster_distribution(df)
    plot_city_overview(df)
    plot_score_distribution(df)

    if history and len(history) >= 2:
        plot_regeneration_journey(history)
    print(f"[Charts] All charts saved to → {CHART_DIR}/")

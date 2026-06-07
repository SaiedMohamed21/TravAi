# =============================================================
# api/services/recommendation_service.py
# Bridges the FastAPI layer with the existing recommendation
# and regeneration engines. NEVER modifies engine logic.
# =============================================================

import pandas as pd
from typing import Optional, Dict, Any

from recommendation.engine import load_model, prepare_dataframe, get_initial_recommendation
from regeneration.regenerator import regenerate
from api.schemas.schemas import TourCard


def _build_reason(tour: pd.Series, context: str = "initial") -> str:
    """Generate a human-readable recommendation reason from tour data."""
    parts = []

    quality = tour.get("quality_score", 0)
    if quality >= 0.85:
        parts.append("Exceptional quality score")
    elif quality >= 0.70:
        parts.append("High quality experience")
    else:
        parts.append("Good value experience")

    cluster = tour.get("cluster_label", "")
    if cluster:
        parts.append(f"{cluster} tier")

    rating = tour.get("rating", 0)
    reviews = tour.get("number_of_reviews", 0)
    if rating >= 4.8:
        parts.append(f"outstanding {rating}/5 rating ({reviews:,} reviews)")
    elif rating >= 4.5:
        parts.append(f"excellent {rating}/5 rating")

    if tour.get("transport_included"):
        parts.append("transport included")
    if tour.get("meals_included"):
        parts.append("meals included")

    duration = tour.get("duration_hours", 0)
    if duration >= 8:
        parts.append(f"full-day {duration}h experience")
    elif duration >= 4:
        parts.append(f"{duration}h immersive tour")

    return " · ".join(parts) if parts else "Best match for your criteria"


def _series_to_card(tour: pd.Series, reason_context: str = "initial") -> TourCard:
    """Convert a pd.Series tour row to a TourCard response model."""
    return TourCard(
        tour_id=int(tour["tour_id"]),
        tour_title=str(tour["tour_title"]),
        city=str(tour["city"]),
        cluster_label=str(tour["cluster_label"]),
        base_price_usd=float(tour["base_price_usd"]),
        rating=float(tour["rating"]),
        number_of_reviews=int(tour["number_of_reviews"]),
        duration_hours=float(tour["duration_hours"]),
        transport_included=bool(tour["transport_included"]),
        meals_included=bool(tour["meals_included"]),
        quality_score=float(tour.get("quality_score", 0)),
        value_score=float(tour.get("value_score", 0)),
        recommendation_reason=_build_reason(tour, reason_context),
        languages_spoken=str(tour.get("languages_spoken", "")),
        accessibility=str(tour.get("accessibility", "")),
        guide_name=str(tour.get("guide_name", "")),
        available_datetime=str(tour.get("available_datetime", "")),
        tour_description=str(tour.get("tour_description", "")),
    )


class RecommendationService:
    """Stateless service wrapping the existing ML pipeline."""

    def __init__(self, df: pd.DataFrame, model):
        self.df = df
        self.model = model

    def recommend(
        self,
        budget: float,
        city: Optional[str] = None,
        cluster: Optional[str] = None,
        preferences: Optional[Dict[str, Any]] = None,
    ) -> TourCard:
        best = get_initial_recommendation(
            self.df,
            city=city,
            cluster=cluster,
            max_budget=budget,
            model=self.model,
        )
        return _series_to_card(best, "initial")

    def regenerate(
        self,
        current_tour_id: int,
        session_history: list,
        city: Optional[str] = None,
        cluster: Optional[str] = None,
        city_budget: Optional[float] = None,
        regen_count: int = 0,
    ) -> Optional[Dict[str, Any]]:
        # Retrieve current tour from df
        current_rows = self.df[self.df["tour_id"] == current_tour_id]
        if current_rows.empty:
            raise ValueError(f"Tour ID {current_tour_id} not found.")
        current = current_rows.iloc[0]

        upgrade = regenerate(
            df=self.df,
            current=current,
            session_history=session_history,
            city=city,
            cluster=cluster,
            city_budget=city_budget,
            regen_count=regen_count,
        )

        if upgrade is None:
            return None

        quality_improvement = float(
            upgrade.get("quality_score", 0) - current.get("quality_score", 0)
        )
        price_difference = float(
            upgrade["base_price_usd"] - current["base_price_usd"]
        )

        # Determine upgrade level
        if quality_improvement >= 0.10:
            upgrade_level = "Major Upgrade"
        elif quality_improvement >= 0.05:
            upgrade_level = "Significant Upgrade"
        else:
            upgrade_level = "Incremental Upgrade"

        return {
            "tour": _series_to_card(upgrade, "regeneration"),
            "quality_improvement": round(quality_improvement, 4),
            "price_difference": round(price_difference, 2),
            "upgrade_level": upgrade_level,
        }

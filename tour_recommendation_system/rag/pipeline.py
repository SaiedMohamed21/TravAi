# =============================================================
# rag/pipeline.py
# Retrieval-Augmented Generation pipeline
#
# Retrieves real tours from the dataset to ground chatbot
# responses — no hallucinations, only actual data.
# =============================================================

import pandas as pd
import numpy as np
from typing import List, Optional, Dict, Any
import re


class TourRAGPipeline:
    """
    Lightweight vector-free RAG using keyword + score-based retrieval.

    For each user query we:
    1. Detect intent (city, cluster, budget, activities).
    2. Filter the dataset accordingly.
    3. Rank by quality_score + value_score.
    4. Return top-k tours as structured context for the LLM prompt.
    """

    CITY_ALIASES = {
        "cairo": "Cairo",
        "القاهرة": "Cairo",
        "luxor": "Luxor",
        "الأقصر": "Luxor",
        "aswan": "Aswan",
        "أسوان": "Aswan",
        "hurghada": "Hurghada",
        "الغردقة": "Hurghada",
        "sharm": "Sharm El Sheikh",
        "sharm el sheikh": "Sharm El Sheikh",
        "شرم الشيخ": "Sharm El Sheikh",
    }

    CLUSTER_ALIASES = {
        "economy": "Economy",
        "budget": "Economy",
        "cheap": "Economy",
        "رخيص": "Economy",
        "اقتصادي": "Economy",
        "mid": "Mid-Range",
        "midrange": "Mid-Range",
        "mid-range": "Mid-Range",
        "متوسط": "Mid-Range",
        "premium": "Premium",
        "ممتاز": "Premium",
        "luxury": "Luxury",
        "فاخر": "Luxury",
        "vip": "Luxury",
    }

    def __init__(self, df: pd.DataFrame):
        self.df = df

    def detect_city(self, text: str) -> Optional[str]:
        text_lower = text.lower()
        for alias, canonical in self.CITY_ALIASES.items():
            if alias in text_lower:
                return canonical
        return None

    def detect_cluster(self, text: str) -> Optional[str]:
        text_lower = text.lower()
        for alias, canonical in self.CLUSTER_ALIASES.items():
            if alias in text_lower:
                return canonical
        return None

    def detect_budget(self, text: str) -> Optional[float]:
        # Match patterns like "$200", "200 USD", "200 dollars"
        patterns = [
            r'\$\s*(\d+(?:\.\d+)?)',
            r'(\d+(?:\.\d+)?)\s*(?:usd|dollars?)',
            r'budget\s+(?:of\s+)?(\d+(?:\.\d+)?)',
            r'(\d+(?:\.\d+)?)\s*(?:دولار|جنيه)',
        ]
        for pattern in patterns:
            match = re.search(pattern, text.lower())
            if match:
                return float(match.group(1))
        return None

    def detect_keywords(self, text: str) -> List[str]:
        keywords = []
        text_lower = text.lower()
        tour_keywords = [
            "diving", "snorkeling", "history", "historical", "ancient",
            "pyramid", "temple", "nile", "cruise", "desert", "safari",
            "beach", "reef", "coral", "cultural", "food", "cooking",
            "adventure", "hiking", "family", "romantic", "sunset",
            "museum", "market", "bazaar", "felucca", "hot air balloon",
            # Arabic
            "غوص", "تاريخ", "أهرام", "معبد", "نيل", "صحراء", "شعاب مرجانية"
        ]
        for kw in tour_keywords:
            if kw in text_lower:
                keywords.append(kw)
        return keywords

    def retrieve(
        self,
        query: str,
        city_context: Optional[str] = None,
        top_k: int = 5,
    ) -> List[Dict[str, Any]]:
        """
        Retrieve the most relevant tours for a given query.
        Returns list of tour dicts for prompt injection.
        """
        candidates = self.df.copy()

        # Apply explicit city context (from UI filter)
        if city_context and city_context in self.df["city"].unique():
            candidates = candidates[candidates["city"] == city_context]

        # Detect constraints from query text
        detected_city = self.detect_city(query)
        detected_cluster = self.detect_cluster(query)
        detected_budget = self.detect_budget(query)

        if detected_city and not city_context:
            candidates = candidates[candidates["city"] == detected_city]
        if detected_cluster:
            candidates = candidates[candidates["cluster_label"] == detected_cluster]
        if detected_budget:
            candidates = candidates[candidates["base_price_usd"] <= detected_budget]

        # Keyword relevance scoring
        keywords = self.detect_keywords(query)
        if keywords and "tour_description" in candidates.columns:
            def relevance_score(row):
                desc = str(row.get("tour_description", "")).lower()
                title = str(row.get("tour_title", "")).lower()
                return sum(1 for kw in keywords if kw in desc or kw in title)
            candidates["_relevance"] = candidates.apply(relevance_score, axis=1)
            # Boost relevant, don't filter out non-matching
            candidates["_rank_score"] = (
                candidates["_relevance"] * 0.3
                + candidates.get("quality_score", pd.Series(0, index=candidates.index)) * 0.5
                + candidates.get("value_score", pd.Series(0, index=candidates.index)) * 0.2
            )
        else:
            score_col = "quality_score" if "quality_score" in candidates.columns else "tour_score"
            candidates["_rank_score"] = candidates[score_col]

        # Deduplicate by tour_title (same tour different dates)
        candidates = candidates.drop_duplicates(subset=["tour_title"])
        top = candidates.sort_values("_rank_score", ascending=False).head(top_k)

        return [self._tour_to_dict(row) for _, row in top.iterrows()]

    def _tour_to_dict(self, row: pd.Series) -> Dict[str, Any]:
        return {
            "tour_id": int(row["tour_id"]),
            "tour_title": str(row["tour_title"]),
            "city": str(row["city"]),
            "cluster_label": str(row["cluster_label"]),
            "base_price_usd": float(row["base_price_usd"]),
            "rating": float(row["rating"]),
            "number_of_reviews": int(row["number_of_reviews"]),
            "duration_hours": float(row["duration_hours"]),
            "transport_included": bool(row["transport_included"]),
            "meals_included": bool(row["meals_included"]),
            "quality_score": float(row.get("quality_score", 0)),
            "value_score": float(row.get("value_score", 0)),
            "languages_spoken": str(row.get("languages_spoken", "")),
            "accessibility": str(row.get("accessibility", "")),
            "guide_name": str(row.get("guide_name", "")),
            "tour_description": str(row.get("tour_description", "")),
        }

    def build_context_block(self, tours: List[Dict[str, Any]]) -> str:
        """Format retrieved tours into a structured context string for prompting."""
        if not tours:
            return "No specific tours retrieved for this query."

        lines = ["=== RETRIEVED TOURS FROM DATABASE ==="]
        for i, t in enumerate(tours, 1):
            lines.append(f"""
Tour {i}: {t['tour_title']}
  City: {t['city']} | Cluster: {t['cluster_label']}
  Price: ${t['base_price_usd']:.2f} | Rating: {t['rating']}/5 ({t['number_of_reviews']} reviews)
  Duration: {t['duration_hours']}h | Transport: {'✓' if t['transport_included'] else '✗'} | Meals: {'✓' if t['meals_included'] else '✗'}
  Quality Score: {t['quality_score']:.3f} | Languages: {t['languages_spoken']}
  Description: {t['tour_description'][:120]}...""")

        lines.append("=== END OF RETRIEVED TOURS ===")
        return "\n".join(lines)

    def get_dataset_summary(self) -> Dict[str, Any]:
        return {
            "total_tours": len(self.df.drop_duplicates(subset=["tour_title"])),
            "cities": sorted(self.df["city"].unique().tolist()),
            "clusters": sorted(self.df["cluster_label"].unique().tolist()),
            "price_range": {
                "min": float(self.df["base_price_usd"].min()),
                "max": float(self.df["base_price_usd"].max()),
            },
        }

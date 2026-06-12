import pandas as pd
import numpy as np
from typing import List

from exceptions import MissingColumnsError


class HotelRepository:
    """
    Repository layer for hotel data.
    Currently loads from CSV; can be swapped with a database later.
    """

    def __init__(self, csv_path: str):
        self.df = self._load_and_clean(csv_path)

    def get_dataframe(self) -> pd.DataFrame:
        return self.df.copy()

    def _load_and_clean(self, csv_path: str) -> pd.DataFrame:
        df = pd.read_csv(csv_path)

        if "Unnamed: 0" in df.columns:
            df = df.drop(columns=["Unnamed: 0"])

        for col in ["city_area", "hotel_name"]:
            if col in df.columns:
                df[col] = df[col].astype(str).str.strip()

        numeric_cols = [
            "price_usd_final",
            "price_usd",
            "premium_final_score",
            "business_final_score",
            "economic_final_score",
            "quality_score",
            "single_FB_USD",
            "double_FB_USD",
            "single_FB_PriceUSD",
            "double_FB_PriceUSD",
            "single_FB_dollar",
            "double_FB_dollar",
            "single_fb_price",
            "double_fb_price",
            "star_rating",
            "num_reviews",
            "avg_review_score",
        ]

        for col in numeric_cols:
            if col in df.columns:
                df[col] = pd.to_numeric(df[col], errors="coerce")

        # Validate required columns
        required_cols = ["city_area", "hotel_name", "governorate", "cluster_segment"]
        missing = [c for c in required_cols if c not in df.columns]
        if missing:
            raise MissingColumnsError(missing)

        # Validate required score columns
        required_score_cols = [
            "premium_final_score",
            "business_final_score",
            "economic_final_score",
            "quality_score",
        ]
        missing_scores = [c for c in required_score_cols if c not in df.columns]
        if missing_scores:
            raise MissingColumnsError(missing_scores)

        # Validate at least one single FB price column
        single_price_cols = [
            "single_FB_USD",
            "single_FB_PriceUSD",
            "single_FB_dollar",
            "single_fb_price",
        ]
        if not any(col in df.columns for col in single_price_cols):
            raise MissingColumnsError(
                ["At least one single FB price column is required: " + ", ".join(single_price_cols)]
            )

        # Validate at least one double FB price column
        double_price_cols = [
            "double_FB_USD",
            "double_FB_PriceUSD",
            "double_FB_dollar",
            "double_fb_price",
        ]
        if not any(col in df.columns for col in double_price_cols):
            raise MissingColumnsError(
                ["At least one double FB price column is required: " + ", ".join(double_price_cols)]
            )

        df = df.dropna(subset=["city_area", "hotel_name"]).copy()
        return df

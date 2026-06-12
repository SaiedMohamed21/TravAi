import pandas as pd
import numpy as np
import itertools
import re
import unicodedata
from typing import List, Dict, Any, Tuple

from exceptions import (
    BudgetTooLowError,
    NoHotelsFoundError,
    InvalidRecommendationInputError,
)

# =========================================================
# CONFIG / CONSTANTS
# =========================================================

DEFAULT_THRESHOLDS_TO_TRY = [0.45, 0.40, 0.35, 0.30, 0.25]

SUPPORTED_CLUSTERS = {"premium", "business", "economic", "quality"}

CITY_ALIAS_MAP = {
    "cairo": [
        "cairo",
        "new cairo",
        "greater cairo",
        "zamalek",
        "maadi",
        "nasr city",
        "heliopolis",
        "giza",
    ],
    "luxor": ["luxor", "west bank", "east bank"],
    "sharm el sheikh": [
        "sharm el sheikh",
        "sharm el-sheikh",
        "sharm",
        "sharm elsheikh",
    ],
    "alexandria": ["alexandria", "alex"],
    "hurghada": ["hurghada"],
    "aswan": ["aswan"],
    "giza": ["giza", "pyramids", "6th october", "6 october", "october"],
}


# =========================================================
# BASIC HELPERS
# =========================================================

def safe_float(x, default=np.nan):
    try:
        if pd.isna(x):
            return default
        return float(x)
    except Exception:
        return default


def json_safe(value):
    if value is None:
        return None

    try:
        if pd.isna(value):
            return None
    except Exception:
        pass

    if isinstance(value, np.integer):
        return int(value)

    if isinstance(value, np.floating):
        return float(value)

    return value


def to_int_or_none(value):
    value = json_safe(value)
    if value is None:
        return None
    return int(value)


def to_float_or_none(value):
    value = json_safe(value)
    if value is None:
        return None
    return float(value)


def normalize_text(value: Any) -> str:
    if value is None:
        return ""

    text = str(value).strip().lower()
    text = unicodedata.normalize("NFKD", text)
    text = "".join(ch for ch in text if not unicodedata.combining(ch))
    text = re.sub(r"[^a-z0-9\s\-]", " ", text)
    text = re.sub(r"\s+", " ", text).strip()

    return text


def canonicalize_city(city: str) -> str:
    city_norm = normalize_text(city)

    for canonical, aliases in CITY_ALIAS_MAP.items():
        if city_norm == canonical or city_norm in aliases:
            return canonical

    return city_norm


def city_match_mask(series: pd.Series, requested_city: str) -> pd.Series:
    requested_key = canonicalize_city(requested_city)
    aliases = CITY_ALIAS_MAP.get(requested_key, [requested_key])

    s = series.fillna("").astype(str).map(normalize_text)
    mask = pd.Series(False, index=series.index)

    for alias in aliases:
        alias = normalize_text(alias)

        if not alias:
            continue

        mask = mask | s.str.contains(alias, regex=False, na=False)

    return mask


def get_single_fb_price(row: pd.Series) -> float:
    possible_cols = [
        "single_FB_USD",
        "single_FB_PriceUSD",
        "single_FB_dollar",
        "single_fb_price",
    ]

    for col in possible_cols:
        if col in row.index and pd.notna(row[col]) and float(row[col]) > 0:
            return float(row[col])

    return np.nan


def get_double_fb_price(row: pd.Series) -> float:
    possible_cols = [
        "double_FB_USD",
        "double_FB_PriceUSD",
        "double_FB_dollar",
        "double_fb_price",
    ]

    for col in possible_cols:
        if col in row.index and pd.notna(row[col]) and float(row[col]) > 0:
            return float(row[col])

    return np.nan


def get_normalized_type_value(h: Dict[str, Any]):
    """
    Output key should be normalized_type.

    Priority:
    1. normalized_type
    2. type_norm
    3. property_type
    4. accommodation_type
    """

    for col in ["normalized_type", "type_norm", "property_type", "accommodation_type"]:
        value = h.get(col)

        if value is not None:
            safe_value = json_safe(value)
            if safe_value is not None:
                return safe_value

    return None


def parse_trip_plan(trip_plan: List[Dict[str, Any]]) -> Dict[str, Dict[str, Any]]:
    parsed = {}

    if not trip_plan or not isinstance(trip_plan, list):
        raise InvalidRecommendationInputError("trip_plan must be a non-empty list.")

    for item in trip_plan:
        if not isinstance(item, dict):
            raise InvalidRecommendationInputError("Each trip_plan item must be a dict.")

        required_keys = ["city", "check_in", "check_out"]

        for key in required_keys:
            if key not in item:
                raise InvalidRecommendationInputError(
                    "Each trip_plan item must contain city, check_in, and check_out."
                )

        requested_city = str(item["city"]).strip()
        city_key = canonicalize_city(requested_city)

        check_in = pd.to_datetime(item["check_in"])
        check_out = pd.to_datetime(item["check_out"])

        nights = (check_out - check_in).days

        if nights <= 0:
            raise InvalidRecommendationInputError(
                f"Invalid dates for city '{requested_city}'. check_out must be after check_in."
            )

        parsed[city_key] = {
            "requested_city": requested_city,
            "check_in": check_in.strftime("%Y-%m-%d"),
            "check_out": check_out.strftime("%Y-%m-%d"),
            "nights": nights,
        }

    return parsed


def validate_room_input(
    num_people: int,
    single_rooms: int,
    double_rooms: int,
) -> Dict[str, Any]:

    if num_people <= 0:
        raise InvalidRecommendationInputError("num_people must be > 0.")

    if single_rooms < 0 or double_rooms < 0:
        raise InvalidRecommendationInputError("single_rooms and double_rooms must be >= 0.")

    if single_rooms == 0 and double_rooms == 0:
        raise InvalidRecommendationInputError("At least one room must be selected.")

    capacity = single_rooms + (2 * double_rooms)

    return {
        "capacity": capacity,
        "is_valid": capacity >= num_people,
        "extra_capacity": capacity - num_people,
    }


def calculate_trip_hotel_price(
    row: pd.Series,
    single_rooms: int,
    double_rooms: int,
    nights: int,
) -> float:

    single_price = get_single_fb_price(row)
    double_price = get_double_fb_price(row)

    if pd.isna(single_price) or pd.isna(double_price):
        return np.nan

    return (single_rooms * single_price + double_rooms * double_price) * nights


def hotel_signature(package_hotels: List[Dict[str, Any]]) -> Tuple[str, ...]:
    return tuple(
        f"{h['matched_city']}::{h['hotel_name']}"
        for h in package_hotels
    )


# =========================================================
# SCORING HELPERS
# =========================================================

def compute_budget_fit(utilization: float) -> float:
    if pd.isna(utilization):
        return -1.0

    if utilization > 1.0:
        return -1.0

    if utilization >= 0.90:
        return 0.85

    if utilization >= 0.75:
        return 1.00

    if utilization >= 0.60:
        return 0.55 + (utilization - 0.60) * (0.45 / 0.15)

    if utilization >= 0.40:
        return 0.10 + (utilization - 0.40) * (0.45 / 0.20)

    return -0.40 - ((0.40 - utilization) / 0.40) * 0.60


def city_alignment_from_penalty(city_penalty: float) -> float:
    if pd.isna(city_penalty):
        return 0.0

    return float(np.clip(1.0 - (city_penalty / 1.25), 0.0, 1.0))


def compute_city_outlier_penalty(
    city_actual_ratios: List[float],
) -> Tuple[float, List[Dict[str, float]]]:

    details = []
    total_penalty = 0.0

    for ratio in city_actual_ratios:
        ratio = safe_float(ratio, default=np.nan)

        if pd.isna(ratio) or ratio <= 0:
            excess = 1.0
            shortage = 1.0
            city_pen = 1.0
            ratio = np.nan
        else:
            excess = max(ratio - 1.0, 0.0)
            shortage = max(1.0 - ratio, 0.0)

            city_pen = 0.0

            if ratio > 1.80:
                city_pen += 0.90 + (ratio - 1.80) * 0.25
            elif ratio > 1.45:
                city_pen += 0.45 + (ratio - 1.45) * 1.20
            elif ratio > 1.20:
                city_pen += (ratio - 1.20) * 1.40

            if ratio < 0.35:
                city_pen += 0.55 + (0.35 - ratio) * 0.60
            elif ratio < 0.55:
                city_pen += 0.18 + (0.55 - ratio) * 0.90

        details.append({
            "target_ratio": float(ratio) if not pd.isna(ratio) else np.nan,
            "excess_ratio": float(excess),
            "shortage_ratio": float(shortage),
            "city_penalty": float(city_pen),
        })

        total_penalty += city_pen

    avg_penalty = total_penalty / max(len(city_actual_ratios), 1)

    return float(avg_penalty), details


# =========================================================
# DATA PREP
# =========================================================

def get_base_score_col(cluster: str) -> str:
    cluster = str(cluster).strip().lower()

    cluster_map = {
        "premium": "premium_final_score",
        "business": "business_final_score",
        "economic": "economic_final_score",
        "quality": "quality_score",
    }

    if cluster not in cluster_map:
        raise InvalidRecommendationInputError(
            f"cluster must be one of: {sorted(SUPPORTED_CLUSTERS)}"
        )

    return cluster_map[cluster]


def get_required_cluster_segment(base_score_col: str):
    """
    Hard filter:
    premium_final_score  -> cluster_segment must be Premium
    business_final_score -> cluster_segment must be Business
    economic_final_score -> cluster_segment must be Economic
    quality_score        -> no hard segment filter
    """

    score_to_segment = {
        "premium_final_score": "premium",
        "business_final_score": "business",
        "economic_final_score": "economic",
    }

    return score_to_segment.get(base_score_col)


def prepare_filtered_df(
    base_df: pd.DataFrame,
    selected_cities: List[str],
    base_score_col: str,
    initial_quality_threshold: float,
    thresholds_to_try: List[float] = None,
) -> Tuple[pd.DataFrame, float]:

    if thresholds_to_try is None:
        thresholds_to_try = DEFAULT_THRESHOLDS_TO_TRY

    thresholds = [initial_quality_threshold] + [
        t for t in thresholds_to_try
        if t != initial_quality_threshold
    ]

    required_segment = get_required_cluster_segment(base_score_col)

    if required_segment is not None and "cluster_segment" not in base_df.columns:
        raise InvalidRecommendationInputError(
            "cluster_segment column is required when using premium, business, or economic cluster."
        )

    last_missing_cities = []

    for thr in thresholds:
        city_frames = []

        for requested_city in selected_cities:
            city_mask = city_match_mask(base_df["city_area"], requested_city)
            temp = base_df[city_mask].copy()

            if temp.empty:
                continue

            temp["matched_city"] = canonicalize_city(requested_city)
            temp["requested_city"] = requested_city

            city_frames.append(temp)

        if not city_frames:
            raise NoHotelsFoundError(selected_cities)

        df = pd.concat(city_frames, ignore_index=True)

        dedup_cols = [
            c for c in ["hotel_name", "city_area", "matched_city"]
            if c in df.columns
        ]

        if dedup_cols:
            df = df.drop_duplicates(subset=dedup_cols, keep="first").copy()

        if base_score_col not in df.columns:
            raise InvalidRecommendationInputError(
                f"Missing required score column: {base_score_col}"
            )

        df[base_score_col] = pd.to_numeric(df[base_score_col], errors="coerce")
        df = df.dropna(subset=[base_score_col]).copy()

        # 1) Score threshold filter
        df = df[df[base_score_col] >= thr].copy()

        # 2) Hard cluster segment filter
        if required_segment is not None:
            df = df[
                df["cluster_segment"]
                .astype(str)
                .str.strip()
                .str.lower()
                .eq(required_segment)
            ].copy()

        df["single_fb_price"] = df.apply(get_single_fb_price, axis=1)
        df["double_fb_price"] = df.apply(get_double_fb_price, axis=1)

        df = df.dropna(subset=["single_fb_price", "double_fb_price"]).copy()

        if df.empty:
            last_missing_cities = selected_cities
            continue

        available = sorted(df["matched_city"].dropna().unique().tolist())

        missing = [
            canonicalize_city(c)
            for c in selected_cities
            if canonicalize_city(c) not in available
        ]

        if not missing:
            return df, thr

        last_missing_cities = missing

    raise NoHotelsFoundError(
        last_missing_cities,
        f"These cities disappeared after filtering even after relaxation: {last_missing_cities}"
    )


# =========================================================
# API RESPONSE FORMATTER
# =========================================================

def format_hotel_api_response(
    chosen_hotels: List[Dict[str, Any]],
    parsed_trip: Dict[str, Dict[str, Any]],
    cluster: str,
    generated_budget: float,
) -> Dict[str, Any]:

    hotels_output = []

    cluster_fallback = str(cluster).strip().capitalize()

    for h in chosen_hotels:
        city_key = h["matched_city"]
        city_trip = parsed_trip[city_key]

        cluster_segment = json_safe(h.get("cluster_segment"))

        if cluster_segment is None:
            cluster_segment = cluster_fallback

        hotels_output.append({
            "governorate": json_safe(h.get("governorate")),
            "city_area": json_safe(h.get("city_area")),
            "hotel_name": json_safe(h.get("hotel_name")),
            "star_rating": to_int_or_none(h.get("star_rating")),
            "num_reviews": to_int_or_none(h.get("num_reviews")),
            "avg_review_score": to_float_or_none(h.get("avg_review_score")),
            "amenities": json_safe(h.get("amenities")),
            "normalized_type": get_normalized_type_value(h),
            "cluster_segment": cluster_segment,
            "check_in": city_trip["check_in"],
            "check_out": city_trip["check_out"],
        })

    return {
        "hotels": hotels_output,
        "budget": round(float(generated_budget), 2),
    }


# =========================================================
# MAIN ENGINE
# =========================================================

def recommend_one_package(
    base_df: pd.DataFrame,
    trip_plan: List[Dict[str, Any]],
    cluster: str,
    total_budget: float,
    num_people: int,
    single_rooms: int,
    double_rooms: int,
    top_k_per_city: int = 8,
    quality_threshold: float = 0.45,
    regenerate_index: int = 0,
    diversity_max_same_hotels: int = None,
) -> Dict[str, Any]:

    if total_budget <= 0:
        raise InvalidRecommendationInputError("total_budget must be > 0.")

    if top_k_per_city <= 0:
        raise InvalidRecommendationInputError("top_k_per_city must be > 0.")

    if regenerate_index < 0:
        raise InvalidRecommendationInputError("regenerate_index must be >= 0.")

    room_validation = validate_room_input(
        num_people=num_people,
        single_rooms=single_rooms,
        double_rooms=double_rooms,
    )

    if not room_validation["is_valid"]:
        raise InvalidRecommendationInputError(
            f"Room capacity is not enough. "
            f"capacity={room_validation['capacity']}, num_people={num_people}"
        )

    parsed_trip = parse_trip_plan(trip_plan)

    selected_city_keys = list(parsed_trip.keys())

    selected_city_display = [
        parsed_trip[k]["requested_city"]
        for k in selected_city_keys
    ]

    nights_per_city = {
        k: parsed_trip[k]["nights"]
        for k in selected_city_keys
    }

    num_cities = len(selected_city_keys)

    base_score_col = get_base_score_col(cluster)

    if diversity_max_same_hotels is None:
        diversity_max_same_hotels = max(0, num_cities - 2)

    # 2) Filter hotels by city, score threshold, and hard cluster segment
    filtered_df, used_threshold = prepare_filtered_df(
        base_df=base_df,
        selected_cities=selected_city_display,
        base_score_col=base_score_col,
        initial_quality_threshold=quality_threshold,
    )

    # 3) Calculate real trip hotel price
    filtered_df["trip_hotel_price"] = filtered_df.apply(
        lambda row: calculate_trip_hotel_price(
            row=row,
            single_rooms=single_rooms,
            double_rooms=double_rooms,
            nights=nights_per_city[row["matched_city"]],
        ),
        axis=1,
    )

    filtered_df = filtered_df.dropna(subset=["trip_hotel_price"]).copy()

    if filtered_df.empty:
        raise NoHotelsFoundError(message="No hotels remain after calculating trip prices.")

    # 4) City budget allocation
    city_summary = (
        filtered_df.groupby("matched_city")
        .agg(
            hotels_count=("hotel_name", "count"),
            median_trip_price=("trip_hotel_price", "median"),
            avg_trip_price=("trip_hotel_price", "mean"),
            min_trip_price=("trip_hotel_price", "min"),
            max_trip_price=("trip_hotel_price", "max"),
        )
        .reset_index()
        .set_index("matched_city")
    )

    missing_after = [
        c for c in selected_city_keys
        if c not in city_summary.index
    ]

    if missing_after:
        raise NoHotelsFoundError(
            missing_after,
            f"These cities disappeared after trip price calculation: {missing_after}"
        )

    selected_city_summary = city_summary.loc[selected_city_keys].copy()

    selected_city_summary["requested_city"] = [
        parsed_trip[c]["requested_city"]
        for c in selected_city_summary.index
    ]

    selected_city_summary["nights"] = [
        parsed_trip[c]["nights"]
        for c in selected_city_summary.index
    ]

    selected_city_summary["adjusted_price"] = (
        selected_city_summary["median_trip_price"] ** 0.70
    )

    selected_city_summary["city_weight"] = (
        selected_city_summary["adjusted_price"]
        / selected_city_summary["adjusted_price"].sum()
    )

    selected_city_summary["city_budget_target"] = (
        selected_city_summary["city_weight"] * total_budget
    )

    city_budget_targets = selected_city_summary["city_budget_target"].to_dict()
    city_medians = selected_city_summary["median_trip_price"].to_dict()

    # 5) Hotel scoring
    filtered_df["city_median_trip_price"] = (
        filtered_df["matched_city"].map(city_medians)
    )

    filtered_df["relative_price"] = (
        filtered_df["trip_hotel_price"]
        / filtered_df["city_median_trip_price"]
    ).clip(lower=0.35, upper=3.00)

    if "quality_score" in filtered_df.columns:
        filtered_df["quality_component"] = (
            filtered_df["quality_score"].fillna(filtered_df[base_score_col])
        )
    else:
        filtered_df["quality_component"] = filtered_df[base_score_col]

    filtered_df["cluster_fit_component"] = filtered_df[base_score_col]

    filtered_df["value_component"] = (
        filtered_df["quality_component"]
        / (filtered_df["relative_price"] ** 0.65)
    )

    filtered_df["intrinsic_hotel_score"] = (
        0.72 * filtered_df["quality_component"]
        + 0.28 * filtered_df["value_component"]
    )

    filtered_df["weighted_intrinsic_hotel_score"] = (
        filtered_df["intrinsic_hotel_score"]
        * filtered_df["matched_city"].map(nights_per_city)
    )

    # 6) Select top hotels per city
    hotels_per_city = {}

    for city_key in selected_city_keys:
        temp = filtered_df[filtered_df["matched_city"] == city_key].copy()

        temp = temp.sort_values(
            by=[
                "cluster_fit_component",
                "intrinsic_hotel_score",
                "trip_hotel_price",
            ],
            ascending=[False, False, True],
        ).head(top_k_per_city)

        hotels_per_city[city_key] = temp.to_dict(orient="records")

    empty_city_lists = [
        parsed_trip[city]["requested_city"]
        for city, hotels in hotels_per_city.items()
        if len(hotels) == 0
    ]

    if empty_city_lists:
        raise NoHotelsFoundError(
            empty_city_lists,
            f"No candidate hotels found for these cities: {empty_city_lists}"
        )

    # 7) Generate package combinations
    all_combos = list(
        itertools.product(
            *[
                hotels_per_city[city]
                for city in selected_city_keys
            ]
        )
    )

    candidates = []
    all_candidates_debug = []

    for combo in all_combos:
        combo = list(combo)

        total_price = sum(h["trip_hotel_price"] for h in combo)

        intrinsic_score = sum(
            h["intrinsic_hotel_score"]
            for h in combo
        )

        total_weighted_score = sum(
            h["weighted_intrinsic_hotel_score"]
            for h in combo
        )

        cluster_focus_score = sum(
            h["cluster_fit_component"]
            for h in combo
        )

        city_penalty = 0.0
        city_ratio_details = []

        for h in combo:
            city = h["matched_city"]
            target = city_budget_targets[city]
            actual = h["trip_hotel_price"]

            city_penalty += abs(actual - target) / target
            city_ratio_details.append(actual / target)

        city_penalty /= len(combo)

        outlier_city_penalty, outlier_detail_rows = compute_city_outlier_penalty(
            city_ratio_details
        )

        budget_utilization = total_price / total_budget
        budget_fit_score = compute_budget_fit(budget_utilization)
        city_alignment_score = city_alignment_from_penalty(city_penalty)

        main_score = (
            0.39 * intrinsic_score
            + 0.29 * cluster_focus_score
            + 0.18 * budget_fit_score
            + 0.10 * city_alignment_score
            - 0.16 * outlier_city_penalty
        )

        candidate = {
            "hotels": combo,
            "signature": hotel_signature(combo),
            "total_price": float(total_price),
            "intrinsic_score": float(intrinsic_score),
            "total_weighted_score": float(total_weighted_score),
            "cluster_focus_score": float(cluster_focus_score),
            "city_penalty": float(city_penalty),
            "city_alignment_score": float(city_alignment_score),
            "outlier_city_penalty": float(outlier_city_penalty),
            "budget_utilization": float(budget_utilization),
            "budget_fit_score": float(budget_fit_score),
            "main_score": float(main_score),
        }

        all_candidates_debug.append(candidate)

        if total_price <= total_budget:
            candidates.append(candidate)

    # 8) Low budget case
    if len(candidates) == 0:
        cheapest_pkg = min(
            all_candidates_debug,
            key=lambda x: x["total_price"]
        )

        min_required_budget = float(cheapest_pkg["total_price"])

        raise BudgetTooLowError(
            current_budget=total_budget,
            cheapest_available_price=min_required_budget,
        )

    # 9) Base ranking
    candidates = sorted(
        candidates,
        key=lambda x: (
            x["main_score"],
            x["cluster_focus_score"],
            x["intrinsic_score"],
            -x["city_penalty"],
        ),
        reverse=True,
    )

    # 10) Regenerate logic with diversity
    chosen_pool = []
    seen_signatures = set()
    remaining = candidates.copy()

    used_hotels_by_city = {
        city: set()
        for city in selected_city_keys
    }

    while remaining:
        if not chosen_pool:
            best = remaining.pop(0)
            chosen_pool.append(best)
            seen_signatures.add(best["signature"])

            for h in best["hotels"]:
                used_hotels_by_city[h["matched_city"]].add(h["hotel_name"])

            continue

        best_idx = None
        best_adjusted_score = -np.inf

        for idx, cand in enumerate(remaining):
            if cand["signature"] in seen_signatures:
                continue

            same_total = 0
            same_vs_any_prev = 0

            for h in cand["hotels"]:
                if h["hotel_name"] in used_hotels_by_city[h["matched_city"]]:
                    same_total += 1

            for prev in chosen_pool:
                overlap = sum(
                    h1["hotel_name"] == h2["hotel_name"]
                    and h1["matched_city"] == h2["matched_city"]
                    for h1, h2 in zip(cand["hotels"], prev["hotels"])
                )

                same_vs_any_prev = max(same_vs_any_prev, overlap)

            if same_vs_any_prev > diversity_max_same_hotels:
                continue

            novelty_ratio = (num_cities - same_total) / max(num_cities, 1)

            regenerate_novelty_weight = min(
                0.18 + (0.05 * regenerate_index),
                0.35,
            )

            repeated_hotel_penalty = min(
                0.10 + (0.04 * regenerate_index),
                0.25,
            )

            adjusted_score = (
                cand["main_score"]
                + (regenerate_novelty_weight * novelty_ratio)
                - (repeated_hotel_penalty * same_total)
            )

            if adjusted_score > best_adjusted_score:
                best_adjusted_score = adjusted_score
                best_idx = idx

        if best_idx is None:
            break

        best = remaining.pop(best_idx)
        chosen_pool.append(best)
        seen_signatures.add(best["signature"])

        for h in best["hotels"]:
            used_hotels_by_city[h["matched_city"]].add(h["hotel_name"])

    if len(chosen_pool) == 0:
        chosen_pool = candidates[:1]
    elif len(chosen_pool) <= regenerate_index:
        chosen_pool = candidates

    chosen = chosen_pool[
        min(regenerate_index, len(chosen_pool) - 1)
    ]

    # 11) Compact final API response
    return format_hotel_api_response(
        chosen_hotels=chosen["hotels"],
        parsed_trip=parsed_trip,
        cluster=cluster,
        generated_budget=chosen["total_price"],
    )

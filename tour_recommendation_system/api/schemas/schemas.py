# =============================================================
# api/schemas/schemas.py
# Pydantic models for all API request/response contracts
# =============================================================

from pydantic import BaseModel, Field
from typing import Optional, List, Any, Dict


# ── Request schemas ────────────────────────────────────────────

class RecommendRequest(BaseModel):
    budget: float = Field(..., gt=0, description="Total available budget in USD")
    city: Optional[str] = Field(None, description="Filter by Egyptian city")
    cluster: Optional[str] = Field(None, description="Cluster label: Economy, Mid-Range, Premium, Luxury")
    preferences: Optional[Dict[str, Any]] = Field(default_factory=dict)


class RegenerateRequest(BaseModel):
    current_tour_id: int = Field(..., description="tour_id of the currently displayed tour")
    session_history: List[int] = Field(default_factory=list, description="List of already-seen tour_ids")
    city: Optional[str] = None
    cluster: Optional[str] = None
    city_budget: Optional[float] = None
    regen_count: int = Field(0, ge=0, description="Number of prior regenerations")


class ChatRequest(BaseModel):
    message: str = Field(..., min_length=1, description="User message")
    conversation_history: List[Dict[str, str]] = Field(
        default_factory=list,
        description="Prior messages: [{role: 'user'|'assistant', content: '...'}]"
    )
    city_context: Optional[str] = Field(None, description="Active city filter for RAG context")


# ── Response schemas ───────────────────────────────────────────

class TourCard(BaseModel):
    tour_id: int
    tour_title: str
    city: str
    cluster_label: str
    base_price_usd: float
    rating: float
    number_of_reviews: int
    duration_hours: float
    transport_included: bool
    meals_included: bool
    quality_score: float
    value_score: float
    recommendation_reason: str
    languages_spoken: Optional[str] = None
    accessibility: Optional[str] = None
    guide_name: Optional[str] = None
    available_datetime: Optional[str] = None
    tour_description: Optional[str] = None


class RecommendResponse(BaseModel):
    success: bool
    tour: Optional[TourCard] = None
    error: Optional[str] = None


class RegenerateResponse(BaseModel):
    success: bool
    tour: Optional[TourCard] = None
    quality_improvement: Optional[float] = None
    price_difference: Optional[float] = None
    upgrade_level: Optional[str] = None
    error: Optional[str] = None


class ChatMessage(BaseModel):
    role: str  # "assistant"
    content: str
    retrieved_tours: Optional[List[TourCard]] = None
    language_detected: Optional[str] = None


class ChatResponse(BaseModel):
    success: bool
    message: Optional[ChatMessage] = None
    error: Optional[str] = None


class HealthResponse(BaseModel):
    status: str
    model_loaded: bool
    dataset_size: int
    cities: List[str]
    clusters: List[str]
    ai_provider: str

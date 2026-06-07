# =============================================================
# api/routes/routes.py
# All FastAPI route handlers
# =============================================================

from fastapi import APIRouter, HTTPException, Request
from api.schemas.schemas import (
    RecommendRequest, RecommendResponse,
    RegenerateRequest, RegenerateResponse,
    ChatRequest, ChatResponse, ChatMessage,
    HealthResponse,
)
from chatbot.ai_client import get_ai_response, _detect_language

router = APIRouter()

# Keywords that indicate a greeting (no tours needed)
GREETING_KEYWORDS = [
    "hello", "hi", "hey", "howdy", "greetings",
    "مرحبا", "مرحباً", "هلو", "هاي", "أهلا", "اهلا", "أهلاً", "اهلاً",
    "السلام", "سلام", "ازيك", "ازيك؟", "عامل", "كيف حالك", "صباح", "مساء",
    "هالو", "ايه", "ايه اخبارك", "ايه الاخبار",
]

def _is_greeting(message: str) -> bool:
    msg = message.strip().lower()
    for kw in GREETING_KEYWORDS:
        if kw in msg:
            return True
    # Also treat very short messages (≤3 words) with no question as greeting
    words = msg.split()
    if len(words) <= 3 and "?" not in msg and "؟" not in msg:
        return True
    return False


# ── Health ─────────────────────────────────────────────────────

@router.get("/health", response_model=HealthResponse, tags=["System"])
async def health(request: Request):
    state = request.app.state
    import os
    return HealthResponse(
        status="ok",
        model_loaded=state.model is not None,
        dataset_size=len(state.df),
        cities=sorted(state.df["city"].unique().tolist()),
        clusters=sorted(state.df["cluster_label"].unique().tolist()),
        ai_provider=os.getenv("AI_PROVIDER", "gemini"),
    )


# ── Recommend ──────────────────────────────────────────────────

@router.post("/recommend", response_model=RecommendResponse, tags=["Recommendations"])
async def recommend(body: RecommendRequest, request: Request):
    svc = request.app.state.recommendation_service
    try:
        tour = svc.recommend(
            budget=body.budget,
            city=body.city,
            cluster=body.cluster,
            preferences=body.preferences,
        )
        return RecommendResponse(success=True, tour=tour)
    except ValueError as e:
        return RecommendResponse(success=False, error=str(e))
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


# ── Regenerate ────────────────────────────────────────────────

@router.post("/regenerate", response_model=RegenerateResponse, tags=["Recommendations"])
async def regenerate_endpoint(body: RegenerateRequest, request: Request):
    svc = request.app.state.recommendation_service
    try:
        result = svc.regenerate(
            current_tour_id=body.current_tour_id,
            session_history=body.session_history,
            city=body.city,
            cluster=body.cluster,
            city_budget=body.city_budget,
            regen_count=body.regen_count,
        )
        if result is None:
            return RegenerateResponse(
                success=False,
                error="No meaningful upgrade found within budget constraints."
            )
        return RegenerateResponse(
            success=True,
            tour=result["tour"],
            quality_improvement=result["quality_improvement"],
            price_difference=result["price_difference"],
            upgrade_level=result["upgrade_level"],
        )
    except ValueError as e:
        return RegenerateResponse(success=False, error=str(e))
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


# ── Chat ──────────────────────────────────────────────────────

@router.post("/chat", response_model=ChatResponse, tags=["Chatbot"])
async def chat(body: ChatRequest, request: Request):
    rag = request.app.state.rag_pipeline

    try:
        # Check if message is a greeting — skip RAG if so
        greeting = _is_greeting(body.message)

        if greeting:
            retrieved = []
            context_block = ""
        else:
            retrieved = rag.retrieve(
                query=body.message,
                city_context=body.city_context,
                top_k=3,
            )
            context_block = rag.build_context_block(retrieved)

        # Build message list including current message
        all_messages = list(body.conversation_history) + [
            {"role": "user", "content": body.message}
        ]

        # Call AI
        ai_text = await get_ai_response(
            messages=all_messages,
            context_block=context_block,
            user_message=body.message,
        )

        lang = _detect_language(body.message)

        # Convert retrieved tours to TourCard schema
        from api.schemas.schemas import TourCard
        tour_cards = []
        for t in retrieved:
            try:
                tour_cards.append(TourCard(
                    tour_id=t["tour_id"],
                    tour_title=t["tour_title"],
                    city=t["city"],
                    cluster_label=t["cluster_label"],
                    base_price_usd=t["base_price_usd"],
                    rating=t["rating"],
                    number_of_reviews=t["number_of_reviews"],
                    duration_hours=t["duration_hours"],
                    transport_included=t["transport_included"],
                    meals_included=t["meals_included"],
                    quality_score=t["quality_score"],
                    value_score=t["value_score"],
                    recommendation_reason=None,
                    languages_spoken=t.get("languages_spoken"),
                    accessibility=t.get("accessibility"),
                    guide_name=t.get("guide_name"),
                    tour_description=t.get("tour_description"),
                ))
            except Exception:
                pass

        return ChatResponse(
            success=True,
            message=ChatMessage(
                role="assistant",
                content=ai_text,
                retrieved_tours=tour_cards if tour_cards else None,
                language_detected=lang,
            ),
        )

    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

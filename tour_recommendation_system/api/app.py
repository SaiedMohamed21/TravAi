# =============================================================
# api/app.py
# Production-ready FastAPI application entry point
#
# Start with:
#   uvicorn api.app:app --reload
# =============================================================

import os
import sys
import logging
from contextlib import asynccontextmanager

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from dotenv import load_dotenv

# Load .env before anything else
load_dotenv()

# Ensure project root is on PYTHONPATH
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from utils.data_loader import load_dataset
from recommendation.engine import load_model, prepare_dataframe
from rag.pipeline import TourRAGPipeline
from api.services.recommendation_service import RecommendationService
from api.routes.routes import router

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)


# ── Lifespan: startup + shutdown ───────────────────────────────

@asynccontextmanager
async def lifespan(app: FastAPI):
    logger.info("🚀 Starting Egypt Tourism AI Platform...")

    # Load dataset
    df = load_dataset()

    # Load ML model
    model = load_model()
    logger.info("✓ ML model loaded")

    # Enrich dataframe with scores
    df = prepare_dataframe(df, model)
    logger.info(f"✓ Dataset enriched: {len(df):,} tours")

    # Initialize RAG pipeline
    rag = TourRAGPipeline(df)
    logger.info("✓ RAG pipeline ready")

    # Initialize recommendation service
    svc = RecommendationService(df, model)
    logger.info("✓ Recommendation service ready")

    # Attach to app state
    app.state.df = df
    app.state.model = model
    app.state.rag_pipeline = rag
    app.state.recommendation_service = svc

    provider = os.getenv("AI_PROVIDER", "gemini")
    logger.info(f"✓ AI provider: {provider}")
    logger.info("🌍 Egypt Tourism AI Platform is ready!")

    yield

    logger.info("Shutting down...")


# ── App factory ────────────────────────────────────────────────

def create_app() -> FastAPI:
    app = FastAPI(
        title="Egypt Tourism AI Platform",
        description=(
            "A production-ready AI-powered tourism assistant for Egypt. "
            "Multilingual chatbot with RAG, smart recommendations, and progressive regeneration."
        ),
        version="2.0.0",
        docs_url="/docs",
        redoc_url="/redoc",
        lifespan=lifespan,
    )

    # CORS
    origins = ["*"]

    app.add_middleware(
        CORSMiddleware,
        allow_origins=origins,
        allow_credentials=True,
        allow_methods=["*"],
        allow_headers=["*"],
    )

    # Routes
    app.include_router(router, prefix="/api")

    return app


app = create_app()


if __name__ == "__main__":
    import uvicorn
    uvicorn.run(
        "api.app:app",
        host=os.getenv("HOST", "0.0.0.0"),
        port=int(os.getenv("PORT", 8000)),
        reload=os.getenv("RELOAD", "true").lower() == "true",
    )

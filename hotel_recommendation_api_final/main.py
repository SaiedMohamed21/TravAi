from fastapi import FastAPI, HTTPException, Request
from fastapi.responses import JSONResponse
import logging

from config import HOTEL_CSV_PATH
from data_loader import HotelRepository
from schemas import HotelRecommendRequest, HotelRecommendResponse
from recommender import recommend_one_package
from exceptions import (
    HotelRecommendationError,
    BudgetTooLowError,
    NoHotelsFoundError,
    MissingColumnsError,
    InvalidRecommendationInputError,
)

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

app = FastAPI(
    title="Hotel Recommendation API",
    description="Production-ready hotel recommendation engine",
    version="1.0.0",
)

# Global repository instance (loaded on startup)
hotel_repo: HotelRepository | None = None


@app.on_event("startup")
def startup_event():
    global hotel_repo
    try:
        hotel_repo = HotelRepository(HOTEL_CSV_PATH)
        logger.info(f"Loaded hotel data from {HOTEL_CSV_PATH}")
    except MissingColumnsError as e:
        logger.error(f"Failed to load hotel data: {e}")
        raise
    except Exception as e:
        logger.error(f"Failed to load hotel data: {e}")
        raise


@app.exception_handler(HotelRecommendationError)
async def hotel_exception_handler(request: Request, exc: HotelRecommendationError):
    if isinstance(exc, BudgetTooLowError):
        return JSONResponse(status_code=422, content=exc.to_dict())
    if isinstance(exc, NoHotelsFoundError):
        return JSONResponse(status_code=422, content=exc.to_dict())
    if isinstance(exc, MissingColumnsError):
        return JSONResponse(status_code=500, content=exc.to_dict())
    if isinstance(exc, InvalidRecommendationInputError):
        return JSONResponse(status_code=422, content=exc.to_dict())
    return JSONResponse(status_code=500, content={"detail": str(exc)})


@app.exception_handler(Exception)
async def generic_exception_handler(request: Request, exc: Exception):
    logger.exception("Unhandled exception")
    return JSONResponse(
        status_code=500,
        content={"detail": "An internal server error occurred."},
    )


@app.get("/health")
def health_check():
    return {"status": "ok"}


@app.post("/api/hotels/recommend", response_model=HotelRecommendResponse)
def recommend_hotels(request: HotelRecommendRequest):
    if hotel_repo is None:
        raise HTTPException(status_code=503, detail="Hotel data not loaded.")

    try:
        result = recommend_one_package(
            base_df=hotel_repo.get_dataframe(),
            trip_plan=[item.model_dump() for item in request.trip_plan],
            cluster=request.cluster,
            total_budget=request.total_budget,
            num_people=request.num_people,
            single_rooms=request.single_rooms,
            double_rooms=request.double_rooms,
            top_k_per_city=request.top_k_per_city,
            quality_threshold=request.quality_threshold,
            regenerate_index=request.regenerate_index,
        )
        return result
    except HotelRecommendationError:
        raise
    except Exception as e:
        logger.exception("Recommendation failed")
        raise HTTPException(status_code=500, detail="An internal error occurred.")

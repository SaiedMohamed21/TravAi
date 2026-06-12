from pydantic import BaseModel, Field, field_validator, model_validator
from typing import List, Literal
from datetime import datetime


class TripPlanItem(BaseModel):
    city: str = Field(..., description="City name")
    check_in: str = Field(..., description="Check-in date (YYYY-MM-DD)")
    check_out: str = Field(..., description="Check-out date (YYYY-MM-DD)")

    @model_validator(mode="after")
    def validate_dates(self):
        check_in_dt = datetime.strptime(self.check_in, "%Y-%m-%d")
        check_out_dt = datetime.strptime(self.check_out, "%Y-%m-%d")
        if check_out_dt <= check_in_dt:
            raise ValueError("check_out must be after check_in")
        return self


class HotelRecommendRequest(BaseModel):
    trip_plan: List[TripPlanItem] = Field(..., min_length=1)
    cluster: Literal["premium", "business", "economic", "quality"]
    total_budget: float = Field(..., gt=0)
    num_people: int = Field(..., gt=0)
    single_rooms: int = Field(..., ge=0)
    double_rooms: int = Field(..., ge=0)
    top_k_per_city: int = Field(default=8, ge=1)
    quality_threshold: float = Field(default=0.45)
    regenerate_index: int = Field(default=0, ge=0)

    @field_validator("trip_plan")
    @classmethod
    def validate_trip_plan_non_empty(cls, v):
        if not v:
            raise ValueError("trip_plan must be a non-empty list.")
        return v

    @model_validator(mode="after")
    def validate_rooms(self):
        if self.single_rooms == 0 and self.double_rooms == 0:
            raise ValueError("At least one room must be selected.")
        capacity = self.single_rooms + 2 * self.double_rooms
        if capacity < self.num_people:
            raise ValueError(
                f"Room capacity is not enough. "
                f"capacity={capacity}, num_people={self.num_people}"
            )
        return self


class HotelResponse(BaseModel):
    governorate: str | None
    city_area: str | None
    hotel_name: str | None
    star_rating: int | None
    num_reviews: int | None
    avg_review_score: float | None
    amenities: str | None
    normalized_type: str | None
    cluster_segment: str | None
    check_in: str
    check_out: str


class HotelRecommendResponse(BaseModel):
    hotels: List[HotelResponse]
    budget: float

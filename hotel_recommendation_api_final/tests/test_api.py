import pytest
from fastapi.testclient import TestClient
from unittest.mock import MagicMock, patch
import pandas as pd

from main import app

client = TestClient(app)


# Patch startup to avoid loading real CSV
@pytest.fixture(autouse=True)
def mock_startup():
    with patch("main.HotelRepository") as MockRepo:
        mock_repo = MagicMock()
        mock_repo.get_dataframe.return_value = pd.DataFrame({
            "city_area": ["Cairo", "Cairo", "Luxor"],
            "hotel_name": ["Hotel A", "Hotel B", "Hotel C"],
            "governorate": ["Cairo", "Cairo", "Luxor"],
            "star_rating": [5, 4, 5],
            "num_reviews": [1000, 500, 2000],
            "avg_review_score": [8.5, 7.5, 9.0],
            "amenities": ["wifi;pool", "wifi", "pool;spa"],
            "normalized_type": ["hotel", "hotel", "resort"],
            "cluster_segment": ["Premium", "Premium", "Premium"],
            "single_FB_USD": [100, 80, 120],
            "double_FB_USD": [150, 120, 180],
            "premium_final_score": [0.8, 0.7, 0.9],
            "business_final_score": [0.6, 0.5, 0.7],
            "economic_final_score": [0.4, 0.3, 0.5],
            "quality_score": [0.75, 0.65, 0.85],
        })
        with patch("main.hotel_repo", mock_repo):
            yield mock_repo


def test_health():
    response = client.get("/health")
    assert response.status_code == 200
    assert response.json() == {"status": "ok"}


def test_invalid_cluster():
    payload = {
        "trip_plan": [{"city": "Cairo", "check_in": "2026-07-01", "check_out": "2026-07-03"}],
        "cluster": "luxury",
        "total_budget": 4000,
        "num_people": 2,
        "single_rooms": 2,
        "double_rooms": 0,
    }
    response = client.post("/api/hotels/recommend", json=payload)
    assert response.status_code == 422


def test_invalid_room_capacity():
    payload = {
        "trip_plan": [{"city": "Cairo", "check_in": "2026-07-01", "check_out": "2026-07-03"}],
        "cluster": "premium",
        "total_budget": 4000,
        "num_people": 5,
        "single_rooms": 1,
        "double_rooms": 1,
    }
    response = client.post("/api/hotels/recommend", json=payload)
    assert response.status_code == 422


def test_no_rooms():
    payload = {
        "trip_plan": [{"city": "Cairo", "check_in": "2026-07-01", "check_out": "2026-07-03"}],
        "cluster": "premium",
        "total_budget": 4000,
        "num_people": 2,
        "single_rooms": 0,
        "double_rooms": 0,
    }
    response = client.post("/api/hotels/recommend", json=payload)
    assert response.status_code == 422


def test_budget_too_low():
    with patch("main.hotel_repo") as mock_repo:
        mock_repo.get_dataframe.return_value = pd.DataFrame({
            "city_area": ["Cairo"],
            "hotel_name": ["Expensive Hotel"],
            "governorate": ["Cairo"],
            "star_rating": [5],
            "num_reviews": [1000],
            "avg_review_score": [9.0],
            "amenities": ["wifi"],
            "normalized_type": ["hotel"],
            "cluster_segment": ["Premium"],
            "single_FB_USD": [5000],
            "double_FB_USD": [8000],
            "premium_final_score": [0.9],
            "business_final_score": [0.7],
            "economic_final_score": [0.5],
            "quality_score": [0.85],
        })
        payload = {
            "trip_plan": [{"city": "Cairo", "check_in": "2026-07-01", "check_out": "2026-07-03"}],
            "cluster": "premium",
            "total_budget": 100,
            "num_people": 2,
            "single_rooms": 2,
            "double_rooms": 0,
        }
        response = client.post("/api/hotels/recommend", json=payload)
        assert response.status_code == 422
        data = response.json()
        assert "current_budget" in data
        assert "cheapest_available_price" in data
        assert "budget_gap" in data


def test_successful_request():
    with patch("main.hotel_repo") as mock_repo:
        mock_repo.get_dataframe.return_value = pd.DataFrame({
            "city_area": ["Cairo", "Cairo", "Luxor", "Luxor"],
            "hotel_name": ["Hotel A", "Hotel B", "Hotel C", "Hotel D"],
            "governorate": ["Cairo", "Cairo", "Luxor", "Luxor"],
            "star_rating": [5, 4, 5, 4],
            "num_reviews": [1000, 500, 2000, 800],
            "avg_review_score": [8.5, 7.5, 9.0, 8.0],
            "amenities": ["wifi;pool", "wifi", "pool;spa", "wifi;gym"],
            "normalized_type": ["hotel", "hotel", "resort", "hotel"],
            "cluster_segment": ["Premium", "Premium", "Premium", "Premium"],
            "single_FB_USD": [100, 80, 120, 90],
            "double_FB_USD": [150, 120, 180, 140],
            "premium_final_score": [0.8, 0.7, 0.9, 0.75],
            "business_final_score": [0.6, 0.5, 0.7, 0.55],
            "economic_final_score": [0.4, 0.3, 0.5, 0.35],
            "quality_score": [0.75, 0.65, 0.85, 0.7],
        })
        payload = {
            "trip_plan": [
                {"city": "Cairo", "check_in": "2026-07-01", "check_out": "2026-07-03"},
                {"city": "Luxor", "check_in": "2026-07-03", "check_out": "2026-07-05"}
            ],
            "cluster": "premium",
            "total_budget": 10000,
            "num_people": 2,
            "single_rooms": 2,
            "double_rooms": 0,
            "top_k_per_city": 8,
            "quality_threshold": 0.45,
            "regenerate_index": 0,
        }
        response = client.post("/api/hotels/recommend", json=payload)
        assert response.status_code == 200
        data = response.json()
        assert "hotels" in data
        assert "budget" in data
        assert isinstance(data["budget"], float)
        for hotel in data["hotels"]:
            assert "normalized_type" in hotel
            assert "property_type" not in hotel

# Hotel Recommendation API

A production-ready FastAPI REST API for hotel package recommendations.

## Features

- Recommends one hotel per city based on trip plan
- Supports cluster filtering: `premium`, `business`, `economic`, `quality`
- Budget-aware package generation with scoring and diversity
- City alias matching (Cairo, Luxor, Sharm El Sheikh, Alexandria, Hurghada, Aswan, Giza)
- Regenerate index support for alternative packages
- Clean JSON error responses

## Project Structure

```
hotel_recommendation_api/
├── main.py
├── schemas.py
├── recommender.py
├── data_loader.py
├── config.py
├── exceptions.py
├── pytest.ini
├── requirements.txt
├── Dockerfile
├── README.md
└── tests/
    └── test_api.py
```

## Installation

```bash
pip install -r requirements.txt
```

## Running Locally

```bash
uvicorn main:app --host 0.0.0.0 --port 8000
```

The API will be available at `http://localhost:8000`.

## Environment Variables

| Variable        | Description                          | Default                   |
|-----------------|--------------------------------------|---------------------------|
| `HOTEL_CSV_PATH` | Path to the hotels CSV file         | `hotels_with_address.csv` |

Set it before running:

```bash
export HOTEL_CSV_PATH=/path/to/your/hotels.csv
uvicorn main:app --host 0.0.0.0 --port 8000
```

## Expected CSV Columns

Minimum required columns:

- `governorate`
- `city_area`
- `hotel_name`
- `star_rating`
- `num_reviews`
- `avg_review_score`
- `amenities`
- `normalized_type` (or `type_norm`, `property_type`, `accommodation_type`)
- `cluster_segment`
- `single_FB_USD` (or `single_FB_PriceUSD`, `single_FB_dollar`, `single_fb_price`)
- `double_FB_USD` (or `double_FB_PriceUSD`, `double_FB_dollar`, `double_fb_price`)
- `premium_final_score`
- `business_final_score`
- `economic_final_score`
- `quality_score`

## API Endpoints

### GET /health

Health check endpoint.

**Response:**
```json
{
  "status": "ok"
}
```

### POST /api/hotels/recommend

Request a hotel recommendation package.

**Request Body:**
```json
{
  "trip_plan": [
    {
      "city": "Cairo",
      "check_in": "2026-07-01",
      "check_out": "2026-07-03"
    },
    {
      "city": "Luxor",
      "check_in": "2026-07-03",
      "check_out": "2026-07-05"
    },
    {
      "city": "Sharm El Sheikh",
      "check_in": "2026-07-05",
      "check_out": "2026-07-08"
    }
  ],
  "cluster": "premium",
  "total_budget": 4000,
  "num_people": 5,
  "single_rooms": 1,
  "double_rooms": 2,
  "top_k_per_city": 8,
  "quality_threshold": 0.45,
  "regenerate_index": 0
}
```

**Successful Response:**
```json
{
  "hotels": [
    {
      "governorate": "Cairo",
      "city_area": "Nasr City, Cairo",
      "hotel_name": "Intercontinental Cairo Citystars by IHG",
      "star_rating": 5,
      "num_reviews": 2294,
      "avg_review_score": 8.9,
      "amenities": "ac; bar; gym; parking; pool; restaurant; spa; wifi",
      "normalized_type": "hotel",
      "cluster_segment": "Premium",
      "check_in": "2026-07-01",
      "check_out": "2026-07-03"
    }
  ],
  "budget": 3313.66
}
```

## Example curl Request

```bash
curl -X POST http://localhost:8000/api/hotels/recommend \
  -H "Content-Type: application/json" \
  -d '{
    "trip_plan": [
      {"city": "Cairo", "check_in": "2026-07-01", "check_out": "2026-07-03"},
      {"city": "Luxor", "check_in": "2026-07-03", "check_out": "2026-07-05"}
    ],
    "cluster": "premium",
    "total_budget": 4000,
    "num_people": 5,
    "single_rooms": 1,
    "double_rooms": 2
  }'
```

## Example Python Request

```python
import requests

payload = {
    "trip_plan": [
        {"city": "Cairo", "check_in": "2026-07-01", "check_out": "2026-07-03"},
        {"city": "Luxor", "check_in": "2026-07-03", "check_out": "2026-07-05"}
    ],
    "cluster": "premium",
    "total_budget": 4000,
    "num_people": 5,
    "single_rooms": 1,
    "double_rooms": 2
}

response = requests.post("http://localhost:8000/api/hotels/recommend", json=payload)
print(response.json())
```

## Running Tests

Tests are configured to run without manual `PYTHONPATH` setup:

```bash
pytest tests/test_api.py -v
```

If you encounter import issues in older environments, you can also run:

```bash
PYTHONPATH=. pytest tests/test_api.py -v
```

## Docker

Build and run with Docker:

```bash
docker build -t hotel-recommendation-api .
docker run -p 8000:8000 -e HOTEL_CSV_PATH=/app/hotels.csv hotel-recommendation-api
```

## License

Internal use only.

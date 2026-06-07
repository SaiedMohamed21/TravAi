# 🏛️ Egypt Tourism AI Platform

A production-ready AI-powered tourism assistant platform built on top of an ML recommendation engine. Features a multilingual chatbot (Layla AI), RAG pipeline grounded in real tour data, smart recommendations with progressive regeneration, and a modern React UI.

---

## 🚀 Quick Start

### 1. Backend Setup

```bash
# Clone / extract the project
cd tour_recommendation_system

# Create a virtual environment (recommended)
python -m venv .venv
source .venv/bin/activate        # Windows: .venv\Scripts\activate

# Install Python dependencies
pip install -r requirements.txt

# Configure environment variables
cp .env.example .env
# → Edit .env and add your GEMINI_API_KEY (see AI Setup below)

# Start the FastAPI backend
uvicorn api.app:app --reload
# → API available at: http://localhost:8000
# → Swagger docs at:  http://localhost:8000/docs
```

### 2. Frontend Setup

```bash
cd frontend
npm install
npm run dev
# → UI available at: http://localhost:5173
```

---

## 🤖 AI Setup (Required for full chatbot)

The chatbot works in fallback mode without an API key, but for full AI-powered multilingual responses:

### Option A — Google Gemini (Recommended, free tier available)

1. Get a free key at https://aistudio.google.com/app/apikey
2. In `.env`:
```
GEMINI_API_KEY=your_key_here
AI_PROVIDER=gemini
```

### Option B — OpenRouter (Supports many models)

1. Get a key at https://openrouter.ai/keys
2. In `.env`:
```
OPENROUTER_API_KEY=your_key_here
AI_PROVIDER=openrouter
```

---

## 📡 API Reference

Base URL: `http://localhost:8000/api`

### GET `/health`
System status, model info, available cities/clusters.

### POST `/recommend`
Get the best tour matching your criteria.
```json
{
  "budget": 300,
  "city": "Cairo",
  "cluster": "Mid-Range"
}
```

### POST `/regenerate`
Find a progressive upgrade over the current recommendation.
```json
{
  "current_tour_id": 42,
  "session_history": [42],
  "city": "Cairo",
  "city_budget": 300,
  "regen_count": 0
}
```

### POST `/chat`
Multilingual tourism chatbot with RAG.
```json
{
  "message": "What are the best diving tours in Sharm El Sheikh?",
  "conversation_history": [],
  "city_context": "Sharm El Sheikh"
}
```

---

## 🗂️ Project Structure

```
tour_recommendation_system/
│
├── api/                        # NEW — FastAPI backend
│   ├── app.py                  # Application entry point
│   ├── routes/routes.py        # All endpoint handlers
│   ├── schemas/schemas.py      # Pydantic request/response models
│   └── services/               # Business logic bridges
│
├── chatbot/                    # NEW — AI client
│   └── ai_client.py            # Gemini / OpenRouter integration
│
├── rag/                        # NEW — RAG pipeline
│   └── pipeline.py             # Tour retrieval & prompt construction
│
├── frontend/                   # NEW — React + Tailwind UI
│   ├── src/
│   │   ├── App.jsx             # Main app layout
│   │   ├── api.js              # API client
│   │   └── components/
│   │       ├── ChatWindow.jsx   # Multilingual chatbot UI
│   │       ├── RecommendPanel.jsx  # Smart recommendation panel
│   │       ├── TourCard.jsx     # Beautiful tour display cards
│   │       └── TypingIndicator.jsx
│   └── package.json
│
├── recommendation/             # PRESERVED — ML recommendation engine
├── regeneration/               # PRESERVED — Progressive regeneration
├── utils/                      # PRESERVED — Config, data loader, scorer
├── models/                     # PRESERVED — Trained RandomForest model
├── data/                       # PRESERVED — Tours dataset
├── charts/                     # PRESERVED — Analytics charts
├── validation/                 # PRESERVED — Evaluation utilities
│
├── main.py                     # PRESERVED — Original CLI pipeline
├── .env.example                # NEW — Environment variable template
└── requirements.txt            # UPDATED — Added FastAPI/httpx/dotenv
```

---

## 🌍 Features

- **Multilingual Chatbot**: Auto-detects and responds in Arabic (RTL) or English
- **RAG Pipeline**: Retrieves real tours from the dataset — no hallucinations
- **Smart Recommendations**: ML-powered quality + value scoring
- **Progressive Regeneration**: Budget-aware upgrade journey (Economy → Luxury)
- **Tour Cards**: Rich visual cards with quality scores, ratings, badges
- **Mobile Responsive**: Full RTL + LTR support, tab navigation on mobile
- **Dark UI**: Premium aesthetic with sand/gold color palette

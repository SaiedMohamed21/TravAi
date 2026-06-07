#!/usr/bin/env bash
# ──────────────────────────────────────────────────────────────
# run.sh  —  Tour Recommendation System launcher (Linux / macOS)
# ──────────────────────────────────────────────────────────────

set -e   # exit on first error

echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "  Tour Recommendation System"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

# 1. Create and activate a virtual environment (optional but clean)
if [ ! -d "venv" ]; then
    echo "[Setup] Creating virtual environment ..."
    python3 -m venv venv
fi

echo "[Setup] Activating virtual environment ..."
source venv/bin/activate

# 2. Install dependencies
echo "[Setup] Installing requirements ..."
pip install -q -r requirements.txt

# 3. Run the full pipeline
echo "[Run] Starting main pipeline ..."
python main.py "$@"

echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "  Done! Charts saved to ./charts/"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

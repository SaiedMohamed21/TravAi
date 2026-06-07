@echo off
:: ──────────────────────────────────────────────────────────────
:: run.bat  —  Tour Recommendation System launcher (Windows)
:: ──────────────────────────────────────────────────────────────

echo.
echo ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
echo   Tour Recommendation System
echo ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

:: 1. Create virtual environment if it doesn't exist
IF NOT EXIST "venv\" (
    echo [Setup] Creating virtual environment ...
    python -m venv venv
)

:: 2. Activate virtual environment
echo [Setup] Activating virtual environment ...
call venv\Scripts\activate.bat

:: 3. Install dependencies
echo [Setup] Installing requirements ...
pip install -q -r requirements.txt

:: 4. Run the full pipeline
echo [Run] Starting main pipeline ...
python main.py %*

echo.
echo ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
echo   Done! Charts saved to .\charts\
echo ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
pause

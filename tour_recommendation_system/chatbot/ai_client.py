# =============================================================
# chatbot/ai_client.py
# Unified AI client supporting Gemini and OpenRouter.
# Insert your API key in .env — never hardcode here.
# =============================================================

import os
import asyncio
import httpx
from typing import List, Dict


GEMINI_API_KEY = os.getenv("GEMINI_API_KEY", "")
OPENROUTER_API_KEY = os.getenv("OPENROUTER_API_KEY", "")
AI_PROVIDER = os.getenv("AI_PROVIDER", "gemini").lower()


TOURISM_SYSTEM_PROMPT = """You are Horus, an expert AI tourism assistant specializing exclusively in Egyptian tourism.
You help travelers discover amazing tours, plan itineraries, and make the best decisions for their trips to Egypt.

STRICT RULES:
1. You ONLY answer questions related to tourism, travel, Egyptian destinations, tours, activities, and travel planning.
2. If a user asks about anything unrelated to tourism (politics, coding, math, etc.), politely refuse and redirect to tourism topics.
3. You MUST respond in the SAME LANGUAGE the user uses. If they write in Arabic, respond in Arabic. If English, respond in English.
4. For Arabic responses, use proper RTL-compatible formatting.
5. NEVER invent or hallucinate tours, prices, or places not in the retrieved database context.
6. ONLY recommend tours that appear in the RETRIEVED TOURS section provided to you.
7. When recommending tours, always mention: name, price, rating, duration, and what makes it special.
8. Be warm, professional, and enthusiastic about Egyptian tourism.
9. If the user sends a greeting (hello, hi, مرحبا, أهلا, ازيك, etc.), respond warmly and ask what kind of Egyptian experience they are looking for. Do NOT list tours unless they ask.

EGYPT DESTINATIONS YOU COVER:
- Cairo (Pyramids, Egyptian Museum, Islamic Cairo, Khan el-Khalili)
- Luxor (Valley of the Kings, Karnak Temple, Luxor Temple)
- Aswan (Abu Simbel, Philae Temple, Nubian Village)
- Hurghada (Red Sea diving, snorkeling, beaches)
- Sharm El Sheikh (Ras Mohammed, coral reefs, diving)

TOUR CLUSTERS:
- Economy: Budget-friendly, great value ($15-$80)
- Mid-Range: Balanced quality and price ($80-$180)
- Premium: High-quality experiences ($180-$280)
- Luxury / VIP: Elite, all-inclusive ($280+)

Always greet users warmly and help them find the perfect Egyptian tour experience."""


def _detect_language(text: str) -> str:
    arabic_chars = sum(1 for c in text if '\u0600' <= c <= '\u06FF')
    return "ar" if arabic_chars > len(text) * 0.2 else "en"


async def call_gemini(
    messages: List[Dict[str, str]],
    context_block: str,
    user_message: str,
) -> str:
    if not GEMINI_API_KEY or GEMINI_API_KEY == "YOUR_GEMINI_API_KEY_HERE":
        return _fallback_response(user_message, context_block)

    system_with_context = TOURISM_SYSTEM_PROMPT
    if context_block:
        system_with_context += f"\n\n{context_block}"

    contents = []
    for msg in messages[:-1]:
        role = "user" if msg["role"] == "user" else "model"
        contents.append({"role": role, "parts": [{"text": msg["content"]}]})

    contents.append({"role": "user", "parts": [{"text": user_message}]})

    url = f"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={GEMINI_API_KEY}"

    payload = {
        "system_instruction": {"parts": [{"text": system_with_context}]},
        "contents": contents,
        "generationConfig": {
            "temperature": 0.7,
            "maxOutputTokens": 1024,
        },
    }

    async with httpx.AsyncClient(timeout=30.0) as client:
        for attempt in range(3):
            resp = await client.post(url, json=payload)
            if resp.status_code == 429:
                if attempt < 2:
                    await asyncio.sleep(10)
                    continue
                resp.raise_for_status()
            resp.raise_for_status()
            data = resp.json()
            return data["candidates"][0]["content"]["parts"][0]["text"]


async def call_openrouter(
    messages: List[Dict[str, str]],
    context_block: str,
    user_message: str,
) -> str:
    if not OPENROUTER_API_KEY or OPENROUTER_API_KEY == "YOUR_OPENROUTER_API_KEY_HERE":
        return _fallback_response(user_message, context_block)

    system_with_context = TOURISM_SYSTEM_PROMPT
    if context_block:
        system_with_context += f"\n\n{context_block}"

    or_messages = [{"role": "system", "content": system_with_context}]
    for msg in messages:
        or_messages.append({"role": msg["role"], "content": msg["content"]})

    url = "https://openrouter.ai/api/v1/chat/completions"
    headers = {
        "Authorization": f"Bearer {OPENROUTER_API_KEY}",
        "HTTP-Referer": "https://tourism-ai.app",
        "X-Title": "Egypt Tourism AI Assistant",
        "Content-Type": "application/json",
    }
    payload = {
        "model": "openrouter/owl-alpha",
        "messages": or_messages,
        "max_tokens": 1024,
        "temperature": 0.7,
    }

    async with httpx.AsyncClient(timeout=30.0) as client:
        resp = await client.post(url, headers=headers, json=payload)
        resp.raise_for_status()
        data = resp.json()
        return data["choices"][0]["message"]["content"]


def _fallback_response(user_message: str, context_block: str) -> str:
    lang = _detect_language(user_message)

    if context_block and "Tour 1:" in context_block:
        if lang == "ar":
            return (
                "🌟 **مرحباً بك في مساعد السياحة المصرية!**\n\n"
                "بناءً على بحثك، وجدت لك بعض الجولات الرائعة من قاعدة بياناتنا:\n\n"
                + _extract_simple_tour_list_ar(context_block)
            )
        else:
            return (
                "🌟 **Welcome to Egypt Tourism AI!**\n\n"
                "Based on your query, here are some great tours from our database:\n\n"
                + _extract_simple_tour_list(context_block)
            )
    else:
        if lang == "ar":
            return (
                "مرحباً! أنا **حورس**، مساعدك السياحي لاستكشاف مصر 🇪🇬\n\n"
                "يمكنني مساعدتك في:\n"
                "• اقتراح جولات سياحية في القاهرة، الأقصر، أسوان، الغردقة، وشرم الشيخ\n"
                "• مقارنة الأسعار والباقات\n"
                "• اختيار تجربة تناسب ميزانيتك\n\n"
                "ما الذي تبحث عنه في رحلتك؟ 🏛️"
            )
        else:
            return (
                "Hello! I'm **Horus**, your Egypt Tourism AI Assistant 🇪🇬\n\n"
                "I can help you with:\n"
                "• Tour recommendations across Cairo, Luxor, Aswan, Hurghada & Sharm El Sheikh\n"
                "• Comparing packages and prices\n"
                "• Finding the perfect experience within your budget\n\n"
                "What kind of Egyptian adventure are you looking for? 🏛️"
            )


def _extract_simple_tour_list(context_block: str) -> str:
    lines = []
    for line in context_block.split("\n"):
        if line.strip().startswith("Tour "):
            lines.append(f"**{line.strip()}**")
        elif any(k in line for k in ["Price:", "Rating:", "Duration:"]):
            lines.append(f"  {line.strip()}")
    return "\n".join(lines[:20])


def _extract_simple_tour_list_ar(context_block: str) -> str:
    lines = []
    for line in context_block.split("\n"):
        if line.strip().startswith("Tour "):
            lines.append(f"**{line.strip()}**")
        elif "Price:" in line:
            lines.append(f"  السعر: {line.split('Price:')[-1].strip()}")
        elif "Rating:" in line:
            lines.append(f"  التقييم: {line.split('Rating:')[-1].strip()}")
    return "\n".join(lines[:20])


async def get_ai_response(
    messages: List[Dict[str, str]],
    context_block: str,
    user_message: str,
) -> str:
    try:
        if AI_PROVIDER == "openrouter":
            return await call_openrouter(messages, context_block, user_message)
        else:
            return await call_gemini(messages, context_block, user_message)
    except httpx.HTTPStatusError as e:
        if e.response.status_code in (401, 403):
            return (
                "⚠️ API key is invalid or not set. "
                "Please add your GEMINI_API_KEY or OPENROUTER_API_KEY to .env\n\n"
                + _fallback_response(user_message, context_block)
            )
        if e.response.status_code == 429:
            return _fallback_response(user_message, context_block)
        raise
    except Exception:
        return _fallback_response(user_message, context_block)

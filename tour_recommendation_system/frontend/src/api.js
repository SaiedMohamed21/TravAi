// src/api.js — Typed API client for the Egypt Tourism AI backend

const BASE = 'http://localhost:8000/api'
async function post(path, body) {
  const res = await fetch(`${BASE}${path}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
  if (!res.ok) {
    const err = await res.json().catch(() => ({ detail: res.statusText }))
    throw new Error(err.detail || 'Request failed')
  }
  return res.json()
}

async function get(path) {
  const res = await fetch(`${BASE}${path}`)
  if (!res.ok) throw new Error(res.statusText)
  return res.json()
}

export const api = {
  health: () => get('/health'),

  recommend: ({ budget, city, cluster, preferences }) =>
    post('/recommend', { budget, city, cluster, preferences }),

  regenerate: ({ current_tour_id, session_history, city, cluster, city_budget, regen_count }) =>
    post('/regenerate', { current_tour_id, session_history, city, cluster, city_budget, regen_count }),

  chat: ({ message, conversation_history, city_context }) =>
    post('/chat', { message, conversation_history, city_context }),
}

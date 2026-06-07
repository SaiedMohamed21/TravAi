// src/components/ChatWindow.jsx
import React, { useState, useRef, useEffect, useCallback } from 'react'
import ReactMarkdown from 'react-markdown'
import { api } from '../api'
import TypingIndicator from './TypingIndicator'
import TourCard from './TourCard'

const QUICK_PROMPTS_EN = [
  "Best tours in Cairo under $100",
  "Compare Luxury vs Economy packages",
  "Recommend a diving tour in Sharm El Sheikh",
  "What's included in a Nile cruise?",
]
const QUICK_PROMPTS_AR = [
  "أفضل جولات في الأقصر",
  "ما هي جولات الغوص في الغردقة؟",
  "قارن بين الباقات الفاخرة والاقتصادية",
  "جولات عائلية في أسوان",
]

function ChatBubble({ msg }) {
  const isUser = msg.role === 'user'
  const isRTL = msg.lang === 'ar'

  return (
    <div
      className={`flex gap-3 mb-4 ${isUser ? 'flex-row-reverse' : 'flex-row'}`}
      dir={isRTL ? 'rtl' : 'ltr'}
    >
      {/* Avatar */}
      {!isUser && (
        <div className="w-8 h-8 rounded-full bg-gradient-to-br from-sand-600 to-sand-400 flex items-center justify-center shrink-0 shadow-lg mt-1">
          <span className="text-xs font-bold text-stone-900">AI</span>
        </div>
      )}

      <div className={`flex flex-col gap-2 max-w-[85%] ${isUser ? 'items-end' : 'items-start'}`}>
        {/* Text bubble */}
        <div
          className={`rounded-2xl px-4 py-3 text-sm leading-relaxed ${
            isUser
              ? 'bg-sand-500/20 border border-sand-400/25 text-sand-100 rounded-br-sm'
              : 'glass-gold border border-sand-400/15 text-stone-200 rounded-bl-sm'
          }`}
        >
          {isUser ? (
            <p>{msg.content}</p>
          ) : (
            <div className="prose-chat">
              <ReactMarkdown>{msg.content}</ReactMarkdown>
            </div>
          )}
        </div>

        {/* Tour cards from RAG */}
        {msg.tours && msg.tours.length > 0 && (
          <div className="w-full space-y-2">
            <p className="text-xs text-stone-500 px-1">
              ✦ Retrieved from database ({msg.tours.length} tours)
            </p>
            <div className="grid gap-2">
              {msg.tours.slice(0, 3).map(t => (
                <TourCard key={t.tour_id} tour={t} compact />
              ))}
            </div>
          </div>
        )}

        <span className="text-xs text-stone-600 px-1">
          {new Date(msg.ts).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
        </span>
      </div>
    </div>
  )
}

export default function ChatWindow() {
  const [messages, setMessages] = useState([
    {
      id: 0,
      role: 'assistant',
      content: "🌟 **Welcome to Egypt Tourism AI!** I'm **Horus**, your personal travel assistant.\n\nI can help you discover amazing tours across Cairo, Luxor, Aswan, Hurghada, and Sharm El Sheikh. All my recommendations come directly from our verified tour database.\n\nWhat Egyptian adventure can I help you plan today? 🏛️",
      ts: Date.now(),
      lang: 'en',
    }
  ])
  const [input, setInput] = useState('')
  const [isTyping, setIsTyping] = useState(false)
  const [lang, setLang] = useState('en')
  const [cityContext, setCityContext] = useState('')
  const bottomRef = useRef(null)
  const inputRef = useRef(null)

  const scrollToBottom = useCallback(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [])

  useEffect(() => { scrollToBottom() }, [messages, isTyping])

  const detectLang = text => {
    const arabicChars = (text.match(/[\u0600-\u06FF]/g) || []).length
    return arabicChars > text.length * 0.2 ? 'ar' : 'en'
  }

  const sendMessage = async (text) => {
    if (!text?.trim()) return
    const userLang = detectLang(text)
    setLang(userLang)

    const userMsg = { id: Date.now(), role: 'user', content: text, ts: Date.now(), lang: userLang }
    setMessages(prev => [...prev, userMsg])
    setInput('')
    setIsTyping(true)

    try {
      const history = messages
        .filter(m => m.role !== 'system')
        .slice(-10)
        .map(m => ({ role: m.role, content: m.content }))

      const res = await api.chat({
        message: text,
        conversation_history: history,
        city_context: cityContext || null,
      })

      if (res.success && res.message) {
        setMessages(prev => [...prev, {
          id: Date.now() + 1,
          role: 'assistant',
          content: res.message.content,
          tours: res.message.retrieved_tours || [],
          ts: Date.now(),
          lang: res.message.language_detected || 'en',
        }])
      }
    } catch (e) {
      setMessages(prev => [...prev, {
        id: Date.now() + 2,
        role: 'assistant',
        content: `⚠️ Could not connect to the server. Make sure the backend is running:\n\n\`uvicorn api.app:app --reload\``,
        ts: Date.now(),
        lang: 'en',
      }])
    } finally {
      setIsTyping(false)
    }
  }

  const handleKey = e => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault()
      sendMessage(input)
    }
  }

  const quickPrompts = lang === 'ar' ? QUICK_PROMPTS_AR : QUICK_PROMPTS_EN

  const CITIES = ['', 'Cairo', 'Luxor', 'Aswan', 'Hurghada', 'Sharm El Sheikh']

  return (
    <div className="h-full flex flex-col" dir={lang === 'ar' ? 'rtl' : 'ltr'}>
      {/* Chat header */}
      <div className="flex items-center justify-between px-4 py-3 border-b border-white/8 shrink-0">
        <div className="flex items-center gap-3">
          <div className="relative">
            <div className="w-9 h-9 rounded-full bg-gradient-to-br from-sand-500 to-sand-700 flex items-center justify-center shadow-lg">
              <span className="text-sm font-bold text-stone-900">H</span>
            </div>
            <div className="absolute -bottom-0.5 -right-0.5 w-3 h-3 bg-emerald-400 rounded-full border-2 border-stone-950" />
          </div>
          <div>
            <p className="text-sm font-semibold text-white">Horus AI</p>
            <p className="text-xs text-stone-500">Egypt Tourism Expert • Online</p>
          </div>
        </div>

        {/* City context filter */}
        <select
          value={cityContext}
          onChange={e => setCityContext(e.target.value)}
          className="bg-stone-800/60 border border-white/10 text-stone-400 text-xs rounded-lg px-2 py-1 focus:outline-none"
          title="Filter tour context by city"
        >
          {CITIES.map(c => <option key={c} value={c}>{c || 'All cities'}</option>)}
        </select>
      </div>

      {/* Messages */}
      <div className="flex-1 overflow-y-auto scrollbar-thin px-4 py-4 space-y-1">
        {messages.map(msg => <ChatBubble key={msg.id} msg={msg} />)}
        {isTyping && <TypingIndicator />}
        <div ref={bottomRef} />
      </div>

      {/* Quick prompts */}
      {messages.length <= 2 && !isTyping && (
        <div className="px-4 pb-2 flex flex-wrap gap-1.5">
          {quickPrompts.map((p, i) => (
            <button
              key={i}
              onClick={() => sendMessage(p)}
              className="text-xs bg-stone-800/80 hover:bg-stone-700/80 border border-white/10 hover:border-sand-400/30 text-stone-400 hover:text-sand-400 px-3 py-1.5 rounded-full transition-all duration-200"
            >
              {p}
            </button>
          ))}
        </div>
      )}

      {/* Input */}
      <div className="px-4 pb-4 pt-2 shrink-0 border-t border-white/8">
        <div className="flex gap-2 items-end">
          <textarea
            ref={inputRef}
            value={input}
            onChange={e => setInput(e.target.value)}
            onKeyDown={handleKey}
            placeholder={lang === 'ar' ? 'اسأل عن أي جولة في مصر...' : 'Ask about any tour in Egypt...'}
            rows={1}
            className="flex-1 bg-stone-800/60 border border-white/10 focus:border-sand-400/40 text-stone-200 placeholder:text-stone-600 text-sm rounded-xl px-4 py-3 resize-none focus:outline-none transition-colors scrollbar-thin"
            style={{ maxHeight: '120px', overflowY: 'auto' }}
            dir={lang === 'ar' ? 'rtl' : 'ltr'}
          />
          <button
            onClick={() => sendMessage(input)}
            disabled={!input.trim() || isTyping}
            className="w-10 h-10 bg-sand-500 hover:bg-sand-400 disabled:opacity-40 disabled:cursor-not-allowed rounded-xl flex items-center justify-center transition-all duration-200 shrink-0"
          >
            <svg className="w-4 h-4 text-stone-900" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2.5} d="M12 19l9 2-9-18-9 18 9-2zm0 0v-8"/>
            </svg>
          </button>
        </div>
        <p className="text-xs text-stone-700 mt-1.5 text-center">
          Powered by RAG • Responses grounded in real tour data
        </p>
      </div>
    </div>
  )
}

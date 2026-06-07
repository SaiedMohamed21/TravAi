// src/App.jsx — Egypt Tourism AI Platform
import React, { useState, useEffect } from 'react'
import ChatWindow from './components/ChatWindow'
import RecommendPanel from './components/RecommendPanel'
import { api } from './api'

function StatusBar({ health }) {
  if (!health) return null
  return (
    <div className="flex items-center gap-3 text-xs">
      <span className="flex items-center gap-1.5">
        <span className={`w-1.5 h-1.5 rounded-full ${health.model_loaded ? 'bg-emerald-400' : 'bg-red-400'}`} />
        <span className="text-stone-500">ML Model</span>
      </span>
      <span className="text-stone-700">·</span>
      <span className="text-stone-500">{health.dataset_size?.toLocaleString()} tours</span>
      <span className="text-stone-700">·</span>
      <span className="text-stone-500 capitalize">{health.ai_provider}</span>
    </div>
  )
}

function Logo() {
  return (
    <div className="flex items-center gap-3">
      <div className="relative">
        <div className="w-8 h-8 flex items-center justify-center">
          <svg viewBox="0 0 32 32" fill="none" className="w-8 h-8">
            <polygon points="16,2 30,28 2,28" fill="none" stroke="url(#gold)" strokeWidth="1.5"/>
            <polygon points="16,8 26,24 6,24" fill="url(#goldFill)" fillOpacity="0.15"/>
            <line x1="16" y1="2" x2="16" y2="28" stroke="url(#gold)" strokeWidth="0.75" strokeDasharray="2,2"/>
            <defs>
              <linearGradient id="gold" x1="0" y1="0" x2="1" y2="1">
                <stop offset="0%" stopColor="#d4a853"/>
                <stop offset="100%" stopColor="#c4922e"/>
              </linearGradient>
              <linearGradient id="goldFill" x1="0" y1="0" x2="0" y2="1">
                <stop offset="0%" stopColor="#d4a853"/>
                <stop offset="100%" stopColor="#c4922e"/>
              </linearGradient>
            </defs>
          </svg>
        </div>
      </div>
      <div>
        <h1 className="font-display font-bold text-white text-base leading-none tracking-wide">
          Egypt Tourism <span className="text-sand-400">AI</span>
        </h1>
        <p className="text-stone-500 text-[10px] tracking-widest uppercase mt-0.5">Your Intelligent Travel Companion</p>
      </div>
    </div>
  )
}

const TABS = [
  { id: 'chat', label: 'AI Chat', icon: (
    <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z"/>
    </svg>
  )},
  { id: 'recommend', label: 'Recommend', icon: (
    <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z"/>
    </svg>
  )},
]

export default function App() {
  const [activeTab, setActiveTab] = useState('chat')
  const [health, setHealth] = useState(null)

  useEffect(() => {
    api.health().then(setHealth).catch(() => setHealth(null))
  }, [])

  return (
    <div className="min-h-screen flex flex-col">
      {/* Top decorative strip */}
      <div className="h-px bg-gradient-to-r from-transparent via-sand-500/50 to-transparent" />

      {/* Header */}
      <header className="glass border-b border-white/8 sticky top-0 z-50">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 h-14 flex items-center justify-between">
          <Logo />
          <div className="flex items-center gap-4">
            <StatusBar health={health} />
            {!health && (
              <span className="text-xs text-red-400 flex items-center gap-1.5">
                <span className="w-1.5 h-1.5 rounded-full bg-red-400 animate-pulse" />
                Backend offline
              </span>
            )}
          </div>
        </div>
      </header>

      {/* Main layout */}
      <main className="flex-1 max-w-7xl mx-auto w-full px-4 sm:px-6 py-4">
        {/* Desktop: side-by-side */}
        <div className="hidden lg:grid lg:grid-cols-[1fr_380px] gap-4 h-[calc(100vh-6rem)]">
          {/* Chat (left) */}
          <div className="glass rounded-2xl overflow-hidden flex flex-col">
            <ChatWindow />
          </div>

          {/* Recommend (right) */}
          <div className="glass rounded-2xl overflow-hidden flex flex-col p-4">
            <RecommendPanel />
          </div>
        </div>

        {/* Mobile: tab navigation */}
        <div className="lg:hidden flex flex-col h-[calc(100vh-5.5rem)]">
          {/* Tab bar */}
          <div className="flex glass rounded-2xl p-1 mb-3 shrink-0">
            {TABS.map(tab => (
              <button
                key={tab.id}
                onClick={() => setActiveTab(tab.id)}
                className={`flex-1 flex items-center justify-center gap-2 py-2.5 rounded-xl text-sm font-medium transition-all duration-200 ${
                  activeTab === tab.id
                    ? 'bg-sand-500/20 text-sand-400 border border-sand-400/20'
                    : 'text-stone-500 hover:text-stone-300'
                }`}
              >
                {tab.icon}
                {tab.label}
              </button>
            ))}
          </div>

          {/* Tab content */}
          <div className="flex-1 glass rounded-2xl overflow-hidden">
            {activeTab === 'chat' ? (
              <ChatWindow />
            ) : (
              <div className="h-full overflow-y-auto p-4">
                <RecommendPanel />
              </div>
            )}
          </div>
        </div>
      </main>

      {/* Footer strip */}
      <div className="h-px bg-gradient-to-r from-transparent via-sand-500/30 to-transparent" />

      {/* Background decorations */}
      <div className="fixed inset-0 pointer-events-none overflow-hidden -z-10">
        <div className="absolute top-0 left-1/4 w-96 h-96 bg-sand-500/3 rounded-full blur-3xl" />
        <div className="absolute bottom-0 right-1/4 w-80 h-80 bg-nile-500/3 rounded-full blur-3xl" />
        <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-[600px] h-[400px] bg-pharaoh-500/2 rounded-full blur-3xl" />
      </div>
    </div>
  )
}

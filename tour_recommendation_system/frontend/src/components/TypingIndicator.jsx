// src/components/TypingIndicator.jsx
import React from 'react'

export default function TypingIndicator() {
  return (
    <div className="flex items-end gap-3 mb-4">
      {/* Avatar */}
      <div className="w-8 h-8 rounded-full bg-gradient-to-br from-sand-600 to-sand-400 flex items-center justify-center shrink-0 shadow-lg">
        <span className="text-xs font-bold text-stone-900">AI</span>
      </div>

      {/* Bubble */}
      <div className="glass-gold rounded-2xl rounded-bl-sm px-4 py-3 border border-sand-400/20">
        <div className="flex items-center gap-1.5">
          <div className="typing-dot" style={{ animationDelay: '0ms' }} />
          <div className="typing-dot" style={{ animationDelay: '150ms' }} />
          <div className="typing-dot" style={{ animationDelay: '300ms' }} />
        </div>
      </div>
    </div>
  )
}

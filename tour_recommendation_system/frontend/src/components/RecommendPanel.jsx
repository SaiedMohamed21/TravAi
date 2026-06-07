// src/components/RecommendPanel.jsx
import React, { useState } from 'react'
import { api } from '../api'
import TourCard from './TourCard'

const CITIES = ['', 'Cairo', 'Luxor', 'Aswan', 'Hurghada', 'Sharm El Sheikh']
const CLUSTERS = ['', 'Economy', 'Mid-Range', 'Premium', 'Luxury']

function Skeleton() {
  return (
    <div className="rounded-2xl border border-white/10 p-5 space-y-3">
      {[1, 2, 3, 4].map(i => (
        <div key={i} className={`h-4 shimmer-bg rounded-lg ${i === 1 ? 'w-3/4' : i === 4 ? 'w-1/2' : 'w-full'}`} />
      ))}
    </div>
  )
}

export default function RecommendPanel() {
  const [budget, setBudget] = useState(500)
  const [city, setCity] = useState('')
  const [cluster, setCluster] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState(null)
  const [tour, setTour] = useState(null)
  const [regenInfo, setRegenInfo] = useState(null)
  const [sessionHistory, setSessionHistory] = useState([])
  const [regenCount, setRegenCount] = useState(0)
  const [regenLoading, setRegenLoading] = useState(false)
  const [noMoreUpgrades, setNoMoreUpgrades] = useState(false)

  const handleRecommend = async () => {
    setLoading(true)
    setError(null)
    setTour(null)
    setRegenInfo(null)
    setSessionHistory([])
    setRegenCount(0)
    setNoMoreUpgrades(false)

    try {
      const res = await api.recommend({ budget, city: city || null, cluster: cluster || null })
      if (res.success) {
        setTour(res.tour)
        setSessionHistory([res.tour.tour_id])
      } else {
        setError(res.error || 'No tours found matching your criteria.')
      }
    } catch (e) {
      setError(e.message)
    } finally {
      setLoading(false)
    }
  }

  const handleRegenerate = async () => {
    if (!tour) return
    setRegenLoading(true)
    setNoMoreUpgrades(false)

    try {
      const res = await api.regenerate({
        current_tour_id: tour.tour_id,
        session_history: sessionHistory,
        city: city || null,
        cluster: cluster || null,
        city_budget: budget,
        regen_count: regenCount,
      })

      if (res.success && res.tour) {
        setTour(res.tour)
        setRegenInfo({
          quality_improvement: res.quality_improvement,
          price_difference: res.price_difference,
          upgrade_level: res.upgrade_level,
        })
        setSessionHistory(prev => [...prev, res.tour.tour_id])
        setRegenCount(c => c + 1)
      } else {
        setNoMoreUpgrades(true)
      }
    } catch (e) {
      setError(e.message)
    } finally {
      setRegenLoading(false)
    }
  }

  return (
    <div className="h-full flex flex-col gap-4">
      {/* Filter form */}
      <div className="glass rounded-2xl p-4 space-y-3">
        <h2 className="font-display text-lg font-semibold text-white flex items-center gap-2">
          <span className="text-sand-400">✦</span> Find Your Tour
        </h2>

        {/* Budget */}
        <div>
          <div className="flex justify-between items-center mb-1">
            <label className="text-xs text-stone-400 font-medium">Budget</label>
            <span className="text-sand-400 font-bold text-sm">${budget}</span>
          </div>
          <input
            type="range" min="50" max="1500" step="25"
            value={budget}
            onChange={e => setBudget(Number(e.target.value))}
            className="w-full accent-sand-500 cursor-pointer"
          />
          <div className="flex justify-between text-xs text-stone-600 mt-0.5">
            <span>$50</span><span>$1,500</span>
          </div>
        </div>

        {/* City */}
        <div>
          <label className="text-xs text-stone-400 font-medium block mb-1">City</label>
          <select
            value={city}
            onChange={e => setCity(e.target.value)}
            className="w-full bg-stone-800/60 border border-white/10 text-stone-200 text-sm rounded-xl px-3 py-2 focus:outline-none focus:border-sand-400/50"
          >
            {CITIES.map(c => <option key={c} value={c}>{c || 'All cities'}</option>)}
          </select>
        </div>

        {/* Cluster */}
        <div>
          <label className="text-xs text-stone-400 font-medium block mb-1">Experience Level</label>
          <select
            value={cluster}
            onChange={e => setCluster(e.target.value)}
            className="w-full bg-stone-800/60 border border-white/10 text-stone-200 text-sm rounded-xl px-3 py-2 focus:outline-none focus:border-sand-400/50"
          >
            {CLUSTERS.map(c => <option key={c} value={c}>{c || 'Any level'}</option>)}
          </select>
        </div>

        <button
          onClick={handleRecommend}
          disabled={loading}
          className="w-full btn-primary disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {loading ? (
            <span className="flex items-center justify-center gap-2">
              <svg className="animate-spin w-4 h-4" fill="none" viewBox="0 0 24 24">
                <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"/>
                <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z"/>
              </svg>
              Finding best tour...
            </span>
          ) : 'Get Recommendation'}
        </button>
      </div>

      {/* Error */}
      {error && (
        <div className="bg-red-500/10 border border-red-400/20 rounded-xl p-3 text-red-300 text-sm">
          ⚠️ {error}
        </div>
      )}

      {/* Loading skeleton */}
      {loading && <Skeleton />}

      {/* Tour card */}
      {!loading && tour && (
        <div className="flex-1 overflow-y-auto scrollbar-thin space-y-3">
          {regenCount > 0 && (
            <div className="text-xs text-stone-500 flex items-center gap-2">
              <div className="flex-1 h-px bg-stone-800" />
              Upgrade #{regenCount}
              <div className="flex-1 h-px bg-stone-800" />
            </div>
          )}

          <TourCard
            tour={tour}
            onRegenerate={handleRegenerate}
            isUpgrade={regenCount > 0}
            upgradeInfo={regenCount > 0 ? regenInfo : null}
          />

          {regenLoading && (
            <div className="flex items-center justify-center gap-2 py-4 text-stone-500 text-sm">
              <svg className="animate-spin w-4 h-4" fill="none" viewBox="0 0 24 24">
                <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"/>
                <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z"/>
              </svg>
              Finding upgrade...
            </div>
          )}

          {noMoreUpgrades && (
            <div className="bg-stone-800/60 border border-white/8 rounded-xl p-3 text-center">
              <p className="text-stone-400 text-sm">✓ You've reached the best available option in your budget range.</p>
              <button
                onClick={handleRecommend}
                className="mt-2 text-sand-400 text-xs hover:underline"
              >
                Start fresh search
              </button>
            </div>
          )}

          {/* Session summary */}
          {sessionHistory.length > 1 && (
            <div className="glass rounded-xl p-3">
              <p className="text-xs text-stone-500 mb-1">Session journey</p>
              <div className="flex items-center gap-1.5 flex-wrap">
                {sessionHistory.map((id, i) => (
                  <React.Fragment key={id}>
                    <span className={`text-xs px-2 py-0.5 rounded-full ${i === sessionHistory.length - 1 ? 'bg-sand-500/20 text-sand-400' : 'bg-stone-800 text-stone-500'}`}>
                      Tour {i + 1}
                    </span>
                    {i < sessionHistory.length - 1 && <span className="text-stone-600 text-xs">→</span>}
                  </React.Fragment>
                ))}
              </div>
            </div>
          )}
        </div>
      )}

      {!loading && !tour && !error && (
        <div className="flex-1 flex flex-col items-center justify-center text-center gap-4 opacity-50">
          <div className="text-5xl animate-float">🗺️</div>
          <div>
            <p className="text-stone-400 text-sm font-medium">Set your budget & preferences</p>
            <p className="text-stone-600 text-xs mt-1">We'll find the perfect Egyptian tour for you</p>
          </div>
        </div>
      )}
    </div>
  )
}

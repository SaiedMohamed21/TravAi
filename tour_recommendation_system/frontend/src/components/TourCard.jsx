// src/components/TourCard.jsx
import React from 'react'

const CLUSTER_COLORS = {
  Economy: 'text-emerald-400 bg-emerald-400/10 border-emerald-400/20',
  'Mid-Range': 'text-nile-400 bg-nile-400/10 border-nile-400/20',
  Premium: 'text-pharaoh-400 bg-pharaoh-400/10 border-pharaoh-400/20',
  Luxury: 'text-sand-400 bg-sand-400/10 border-sand-400/20',
}

const CITY_EMOJIS = {
  Cairo: '🏛️',
  Luxor: '🗿',
  Aswan: '⛵',
  Hurghada: '🤿',
  'Sharm El Sheikh': '🐠',
}

function StarRating({ rating }) {
  return (
    <span className="flex items-center gap-0.5">
      {[1,2,3,4,5].map(i => (
        <svg key={i} className={`w-3 h-3 ${i <= Math.round(rating) ? 'text-sand-400' : 'text-stone-700'}`} fill="currentColor" viewBox="0 0 20 20">
          <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z"/>
        </svg>
      ))}
      <span className="text-xs text-stone-400 ml-1">{rating}</span>
    </span>
  )
}

function Badge({ children, variant = 'default' }) {
  const cls = {
    default: 'bg-white/8 text-stone-300 border-white/10',
    gold: 'bg-sand-500/15 text-sand-300 border-sand-400/25',
    green: 'bg-emerald-500/15 text-emerald-300 border-emerald-400/25',
  }[variant]
  return (
    <span className={`inline-flex items-center px-2 py-0.5 rounded-lg text-xs border ${cls}`}>
      {children}
    </span>
  )
}

export default function TourCard({ tour, onRegenerate, isUpgrade, upgradeInfo, compact = false }) {
  if (!tour) return null

  const clusterColor = CLUSTER_COLORS[tour.cluster_label] || 'text-stone-400 bg-stone-400/10 border-stone-400/20'
  const cityEmoji = CITY_EMOJIS[tour.city] || '🌍'
  const qualityPct = Math.round((tour.quality_score || 0) * 100)

  return (
    <div className={`relative overflow-hidden rounded-2xl border border-white/10 bg-gradient-to-br from-stone-900/80 to-stone-950/80 backdrop-blur-sm transition-all duration-300 hover:border-sand-400/30 hover:shadow-lg hover:shadow-sand-900/20 ${compact ? 'p-4' : 'p-5'}`}>
      {/* Upgrade badge */}
      {isUpgrade && upgradeInfo && (
        <div className="absolute top-3 right-3">
          <span className="glass-gold text-sand-300 text-xs font-semibold px-2.5 py-1 rounded-full border border-sand-400/30">
            ↑ {upgradeInfo.upgrade_level}
          </span>
        </div>
      )}

      {/* Decorative gradient */}
      <div className="absolute inset-0 bg-gradient-to-br from-sand-500/3 to-transparent pointer-events-none" />

      {/* Header */}
      <div className="relative">
        <div className="flex items-start justify-between gap-3 mb-3">
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2 mb-1.5">
              <span className="text-xl">{cityEmoji}</span>
              <span className={`text-xs font-medium px-2 py-0.5 rounded-full border ${clusterColor}`}>
                {tour.cluster_label}
              </span>
            </div>
            <h3 className={`font-display font-semibold text-white leading-snug ${compact ? 'text-base' : 'text-lg'}`}>
              {tour.tour_title}
            </h3>
            <p className="text-stone-400 text-sm mt-0.5">{tour.city}</p>
          </div>
          <div className="text-right shrink-0">
            <div className="text-2xl font-bold text-sand-400 font-display">
              ${tour.base_price_usd?.toFixed(0)}
            </div>
            <div className="text-stone-500 text-xs">per person</div>
          </div>
        </div>

        {/* Ratings row */}
        <div className="flex items-center gap-3 mb-3">
          <StarRating rating={tour.rating} />
          <span className="text-stone-500 text-xs">({tour.number_of_reviews?.toLocaleString()} reviews)</span>
        </div>

        {/* Quality score bar */}
        <div className="mb-4">
          <div className="flex justify-between items-center mb-1">
            <span className="text-xs text-stone-500">Quality Score</span>
            <span className="text-xs font-semibold text-sand-400">{qualityPct}%</span>
          </div>
          <div className="h-1.5 bg-stone-800 rounded-full overflow-hidden">
            <div
              className="h-full bg-gradient-to-r from-sand-600 to-sand-400 rounded-full transition-all duration-700"
              style={{ width: `${qualityPct}%` }}
            />
          </div>
        </div>

        {/* Feature badges */}
        <div className="flex flex-wrap gap-1.5 mb-4">
          <Badge>{tour.duration_hours}h duration</Badge>
          {tour.transport_included && <Badge variant="green">🚌 Transport</Badge>}
          {tour.meals_included && <Badge variant="green">🍽️ Meals</Badge>}
          {tour.languages_spoken && (
            <Badge>🌐 {tour.languages_spoken.split(',').length} lang{tour.languages_spoken.split(',').length > 1 ? 's' : ''}</Badge>
          )}
        </div>

        {/* Recommendation reason */}
        {tour.recommendation_reason && (
          <div className="bg-sand-500/8 border border-sand-400/15 rounded-xl p-3 mb-4">
            <p className="text-xs text-sand-300 leading-relaxed">
              <span className="font-semibold text-sand-400">✦ Why this tour: </span>
              {tour.recommendation_reason}
            </p>
          </div>
        )}

        {/* Description snippet */}
        {!compact && tour.tour_description && (
          <p className="text-stone-400 text-sm leading-relaxed mb-4 line-clamp-2">
            {tour.tour_description}
          </p>
        )}

        {/* Upgrade delta info */}
        {isUpgrade && upgradeInfo && (
          <div className="flex gap-3 mb-4">
            {upgradeInfo.quality_improvement > 0 && (
              <div className="flex-1 bg-emerald-500/8 border border-emerald-400/15 rounded-xl p-2.5 text-center">
                <div className="text-emerald-400 font-bold text-sm">
                  +{(upgradeInfo.quality_improvement * 100).toFixed(1)}%
                </div>
                <div className="text-stone-500 text-xs">quality gain</div>
              </div>
            )}
            <div className="flex-1 bg-sand-500/8 border border-sand-400/15 rounded-xl p-2.5 text-center">
              <div className="text-sand-400 font-bold text-sm">
                +${upgradeInfo.price_difference?.toFixed(0)}
              </div>
              <div className="text-stone-500 text-xs">price diff</div>
            </div>
          </div>
        )}

        {/* Actions */}
        {onRegenerate && (
          <button
            onClick={onRegenerate}
            className="w-full bg-stone-800/80 hover:bg-stone-700/80 border border-white/10 hover:border-sand-400/30 text-stone-300 hover:text-sand-400 text-sm font-medium py-2.5 rounded-xl transition-all duration-200 flex items-center justify-center gap-2"
          >
            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
            </svg>
            Find Better Option
          </button>
        )}
      </div>
    </div>
  )
}

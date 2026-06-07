# =============================================================
# regeneration/session.py
# Session management — tracks what has been shown, prevents
# duplicates, and stores the full recommendation history.
# =============================================================

from dataclasses import dataclass, field
from typing import List, Optional
import pandas as pd


@dataclass
class RecommendationSession:
    """
    Stores state for a single user session.

    Attributes
    ----------
    history         : ordered list of tours shown (pd.Series)
    shown_ids       : set of tour_id values seen (for O(1) lookup)
    regen_count     : number of regeneration calls made
    current         : the currently active recommendation
    city            : city filter active for this session
    cluster         : cluster filter active for this session
    city_budget     : price ceiling from Local Budget Divider
    """
    history:      List[pd.Series] = field(default_factory=list)
    shown_ids:    set             = field(default_factory=set)
    regen_count:  int             = 0
    current:      Optional[pd.Series] = None
    city:         Optional[str]   = None
    cluster:      Optional[str]   = None
    city_budget:  Optional[float] = None

    def record(self, tour: pd.Series) -> None:
        """Record a newly displayed tour in session history."""
        self.history.append(tour)
        self.shown_ids.add(int(tour["tour_id"]))
        self.current = tour

    def already_seen(self, tour_id: int) -> bool:
        return tour_id in self.shown_ids

    def increment_regen(self) -> None:
        self.regen_count += 1

    def print_history(self) -> None:
        """Print a summary of all tours shown in this session."""
        print("\n" + "=" * 60)
        print("  Session History")
        print("=" * 60)
        for i, tour in enumerate(self.history):
            tag = " ◄ CURRENT" if i == len(self.history) - 1 else ""
            print(
                f"  [{i+1}] {tour.get('tour_title', 'N/A')[:45]:<45} "
                f"${tour['base_price_usd']:>7.2f}  "
                f"Q={tour.get('quality_score', 0):.3f}{tag}"
            )
        print("=" * 60)

class HotelRecommendationError(Exception):
    """Base exception for hotel recommendation errors."""
    pass


class BudgetTooLowError(HotelRecommendationError):
    def __init__(self, current_budget: float, cheapest_available_price: float):
        self.current_budget = current_budget
        self.cheapest_available_price = cheapest_available_price
        self.budget_gap = round(cheapest_available_price - current_budget, 2)
        super().__init__(
            f"Budget is too low. Current budget={current_budget}, "
            f"cheapest_available_price={cheapest_available_price}"
        )

    def to_dict(self):
        return {
            "detail": "Budget is too low for the selected criteria.",
            "current_budget": float(self.current_budget),
            "cheapest_available_price": float(self.cheapest_available_price),
            "budget_gap": float(self.budget_gap),
        }


class NoHotelsFoundError(HotelRecommendationError):
    def __init__(self, cities: list = None, message: str = None):
        self.cities = cities or []
        if message is None:
            if self.cities:
                message = f"No hotels found for cities: {', '.join(self.cities)}"
            else:
                message = "No hotels found matching the criteria."
        super().__init__(message)

    def to_dict(self):
        return {
            "detail": str(self),
            "cities": self.cities,
        }


class MissingColumnsError(HotelRecommendationError):
    def __init__(self, missing_columns: list):
        self.missing_columns = missing_columns
        super().__init__(f"Missing required columns: {', '.join(missing_columns)}")

    def to_dict(self):
        return {
            "detail": "Required CSV columns are missing.",
            "missing_columns": self.missing_columns,
        }


class InvalidRecommendationInputError(HotelRecommendationError):
    def __init__(self, message: str):
        super().__init__(message)

    def to_dict(self):
        return {
            "detail": str(self),
        }

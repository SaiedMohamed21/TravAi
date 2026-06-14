INSERT INTO tourguide_Tours (
    TourGuideId, City, TourTitle, TourType, TourDescription, BasePriceUsd, Currency, DurationHours,
    GroupSizeMax, SitesCovered, Rating, NumberOfReviews, StartingPoint, AgeRestriction,
    TransportIncluded, MealsIncluded, IsAccessible, Accessibility, Customizable, Season,
    IncludedServices, ExcludedServices, SafetyMeasures, BestTimeToVisit, PickupDetails,
    SourcePlatform, AvailableDateTime, CancellationPolicy, Active, CreatedAt, UpdatedAt, TourScore
) VALUES (
    153, 'Cairo', 'Giza Pyramids and Sphinx Private Tour', 'Historical', 'Experience the ancient wonders of Egypt.', 50.00, 'USD', 4,
    10, 'Pyramids, Sphinx', 0.0, 0, 'Hotel pickup', 'All ages',
    1, 0, 1, 'Wheelchair accessible', 1, 'All Seasons',
    'Private guide, entry tickets', 'Lunch, tips', 'Masks required', 'Morning', 'Hotel lobby',
    'TravAi', GETDATE(), 1, 0, GETDATE(), GETDATE(), 0.0
), (
    154, 'Luxor', 'Luxor Valley of the Kings Guided Day Trip', 'Historical', 'Visit the tombs of the Pharaohs.', 75.00, 'USD', 6,
    8, 'Valley of the Kings, Hatshepsut Temple', 0.0, 0, 'Luxor hotel pickup', '10+',
    1, 1, 0, 'Not wheelchair accessible', 0, 'Winter',
    'Egyptologist guide, AC transport, lunch', 'Drinks, tickets', 'AC vehicle', 'Cooler months', 'Hotel lobby',
    'TravAi', GETDATE(), 1, 0, GETDATE(), GETDATE(), 0.0
), (
    153, 'Aswan', 'Philae Temple Tour by Motorboat', 'Cultural', 'Explore the beautiful island temple of Isis.', 30.00, 'USD', 3,
    12, 'Philae Temple, High Dam', 0.0, 0, 'Aswan marina', 'All ages',
    1, 0, 1, 'Assistance needed', 1, 'All Seasons',
    'Motorboat ride, guide', 'Entrance fees', 'Life jackets provided', 'Afternoon', 'Aswan Marina lobby',
    'TravAi', GETDATE(), 1, 0, GETDATE(), GETDATE(), 0.0
);

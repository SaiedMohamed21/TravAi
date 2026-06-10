import random
from datetime import datetime, timedelta

def get_sql():
    sql = [
        "SET NOCOUNT ON;",
        "DECLARE @TourId BIGINT;",
        "DECLARE @BookingId BIGINT;",
        "DECLARE @UserId BIGINT = 1;",
        "DECLARE @TourGuideId BIGINT = 200;",
        "DECLARE @TourIds TABLE (Id BIGINT);"
        "INSERT INTO @TourIds SELECT TOP 10 Id FROM tourguide_Tours WHERE TourGuideId = @TourGuideId;"
    ]

    # Create 10 bookings
    configs = [
        # Upcoming Confirmed
        {"days_offset": 5, "status": 1, "payment": 1, "participants": 2, "price": 400, "req": "Honeymoon trip, please provide a nice view."},
        # Upcoming Pending
        {"days_offset": 12, "status": 0, "payment": 0, "participants": 1, "price": 150, "req": "Window seat requested"},
        # Upcoming Confirmed
        {"days_offset": 3, "status": 1, "payment": 1, "participants": 4, "price": 800, "req": "Family trip, kids included."},
        # Upcoming Pending
        {"days_offset": 8, "status": 0, "payment": 0, "participants": 2, "price": 300, "req": ""},
        
        # Completed
        {"days_offset": -5, "status": 2, "payment": 1, "participants": 3, "price": 600, "req": "Vegetarian meals for 2."},
        # Completed
        {"days_offset": -15, "status": 2, "payment": 1, "participants": 2, "price": 250, "req": ""},
        # Completed
        {"days_offset": -30, "status": 2, "payment": 1, "participants": 1, "price": 100, "req": "Photography tour."},
        
        # Cancelled (was upcoming)
        {"days_offset": 10, "status": 3, "payment": 3, "participants": 2, "price": 400, "req": "Flight got cancelled."},
        # Cancelled (was past)
        {"days_offset": -2, "status": 3, "payment": 3, "participants": 5, "price": 1000, "req": ""},
        
        # Another Upcoming Confirmed
        {"days_offset": 20, "status": 1, "payment": 1, "participants": 2, "price": 500, "req": "Anniversary celebration."}
    ]

    for i, cfg in enumerate(configs):
        sql.append(f"SELECT TOP 1 @TourId = Id FROM @TourIds ORDER BY NEWID();")
        
        book_date = f"DATEADD(day, -{random.randint(1, 20)}, GETUTCDATE())"
        tour_date = f"DATEADD(day, {cfg['days_offset']}, GETUTCDATE())"
        
        sql.append(f"""
INSERT INTO tourguide_TourBookings (UserId, TourId, TourGuideId, BookingDate, TourDate, TourTime, ParticipantsCount, TotalPrice, Currency, SpecialRequests, PaymentStatus, Status, CreatedAt, UpdatedAt)
VALUES (@UserId, @TourId, @TourGuideId, {book_date}, {tour_date}, '09:00:00', {cfg['participants']}, {cfg['price']}, 'USD', '{cfg['req']}', {cfg['payment']}, {cfg['status']}, {book_date}, GETUTCDATE());
SET @BookingId = SCOPE_IDENTITY();
""")
        
        for p in range(cfg['participants']):
            first = random.choice(['John', 'Jane', 'Michael', 'Sarah', 'David', 'Emma', 'Ali', 'Fatima'])
            last = random.choice(['Smith', 'Johnson', 'Williams', 'Brown', 'Jones', 'Garcia', 'Miller'])
            price_per = cfg['price'] / cfg['participants']
            sql.append(f"""
INSERT INTO tourguide_TourBookingParticipants (BookingId, ParticipantType, AgeType, FirstName, MiddleName, LastName, DateOfBirth, Gender, Nationality, SpecialNeeds, DietaryRequirements, Price)
VALUES (@BookingId, 0, 0, '{first}', '', '{last}', '1990-01-01', {random.choice([0,1])}, 'US', '', '', {price_per});
""")

    return "\n".join(sql)

out_path = r"c:\Users\saied mohamed\Desktop\hotel\TravAi\seed_bookings.sql"
with open(out_path, "w", encoding="utf-8") as out:
    out.write(get_sql())

print("Generated seed_bookings.sql successfully.")

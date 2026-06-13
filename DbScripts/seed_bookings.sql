SET NOCOUNT ON;
DECLARE @TourId BIGINT;
DECLARE @BookingId BIGINT;
DECLARE @UserId BIGINT = 1;
DECLARE @TourGuideId BIGINT = 200;
DECLARE @TourIds TABLE (Id BIGINT);INSERT INTO @TourIds SELECT TOP 10 Id FROM tourguide_Tours WHERE TourGuideId = @TourGuideId;
SELECT TOP 1 @TourId = Id FROM @TourIds ORDER BY NEWID();

INSERT INTO tourguide_TourBookings (UserId, TourId, TourGuideId, BookingDate, TourDate, TourTime, ParticipantsCount, TotalPrice, Currency, SpecialRequests, PaymentStatus, Status, CreatedAt, UpdatedAt)
VALUES (@UserId, @TourId, @TourGuideId, DATEADD(day, -16, GETUTCDATE()), DATEADD(day, 5, GETUTCDATE()), '09:00:00', 2, 400, 'USD', 'Honeymoon trip, please provide a nice view.', 1, 1, DATEADD(day, -16, GETUTCDATE()), GETUTCDATE());
SET @BookingId = SCOPE_IDENTITY();


INSERT INTO tourguide_TourBookingParticipants (BookingId, ParticipantType, AgeType, FirstName, MiddleName, LastName, DateOfBirth, Gender, Nationality, SpecialNeeds, DietaryRequirements, Price)
VALUES (@BookingId, 0, 0, 'Ali', '', 'Jones', '1990-01-01', 0, 'US', '', '', 200.0);


INSERT INTO tourguide_TourBookingParticipants (BookingId, ParticipantType, AgeType, FirstName, MiddleName, LastName, DateOfBirth, Gender, Nationality, SpecialNeeds, DietaryRequirements, Price)
VALUES (@BookingId, 0, 0, 'Emma', '', 'Miller', '1990-01-01', 0, 'US', '', '', 200.0);

SELECT TOP 1 @TourId = Id FROM @TourIds ORDER BY NEWID();

INSERT INTO tourguide_TourBookings (UserId, TourId, TourGuideId, BookingDate, TourDate, TourTime, ParticipantsCount, TotalPrice, Currency, SpecialRequests, PaymentStatus, Status, CreatedAt, UpdatedAt)
VALUES (@UserId, @TourId, @TourGuideId, DATEADD(day, -18, GETUTCDATE()), DATEADD(day, 12, GETUTCDATE()), '09:00:00', 1, 150, 'USD', 'Window seat requested', 0, 0, DATEADD(day, -18, GETUTCDATE()), GETUTCDATE());
SET @BookingId = SCOPE_IDENTITY();


INSERT INTO tourguide_TourBookingParticipants (BookingId, ParticipantType, AgeType, FirstName, MiddleName, LastName, DateOfBirth, Gender, Nationality, SpecialNeeds, DietaryRequirements, Price)
VALUES (@BookingId, 0, 0, 'Ali', '', 'Miller', '1990-01-01', 1, 'US', '', '', 150.0);

SELECT TOP 1 @TourId = Id FROM @TourIds ORDER BY NEWID();

INSERT INTO tourguide_TourBookings (UserId, TourId, TourGuideId, BookingDate, TourDate, TourTime, ParticipantsCount, TotalPrice, Currency, SpecialRequests, PaymentStatus, Status, CreatedAt, UpdatedAt)
VALUES (@UserId, @TourId, @TourGuideId, DATEADD(day, -13, GETUTCDATE()), DATEADD(day, 3, GETUTCDATE()), '09:00:00', 4, 800, 'USD', 'Family trip, kids included.', 1, 1, DATEADD(day, -13, GETUTCDATE()), GETUTCDATE());
SET @BookingId = SCOPE_IDENTITY();


INSERT INTO tourguide_TourBookingParticipants (BookingId, ParticipantType, AgeType, FirstName, MiddleName, LastName, DateOfBirth, Gender, Nationality, SpecialNeeds, DietaryRequirements, Price)
VALUES (@BookingId, 0, 0, 'John', '', 'Miller', '1990-01-01', 0, 'US', '', '', 200.0);


INSERT INTO tourguide_TourBookingParticipants (BookingId, ParticipantType, AgeType, FirstName, MiddleName, LastName, DateOfBirth, Gender, Nationality, SpecialNeeds, DietaryRequirements, Price)
VALUES (@BookingId, 0, 0, 'Sarah', '', 'Garcia', '1990-01-01', 1, 'US', '', '', 200.0);


INSERT INTO tourguide_TourBookingParticipants (BookingId, ParticipantType, AgeType, FirstName, MiddleName, LastName, DateOfBirth, Gender, Nationality, SpecialNeeds, DietaryRequirements, Price)
VALUES (@BookingId, 0, 0, 'Jane', '', 'Miller', '1990-01-01', 1, 'US', '', '', 200.0);


INSERT INTO tourguide_TourBookingParticipants (BookingId, ParticipantType, AgeType, FirstName, MiddleName, LastName, DateOfBirth, Gender, Nationality, SpecialNeeds, DietaryRequirements, Price)
VALUES (@BookingId, 0, 0, 'Emma', '', 'Miller', '1990-01-01', 0, 'US', '', '', 200.0);

SELECT TOP 1 @TourId = Id FROM @TourIds ORDER BY NEWID();

INSERT INTO tourguide_TourBookings (UserId, TourId, TourGuideId, BookingDate, TourDate, TourTime, ParticipantsCount, TotalPrice, Currency, SpecialRequests, PaymentStatus, Status, CreatedAt, UpdatedAt)
VALUES (@UserId, @TourId, @TourGuideId, DATEADD(day, -3, GETUTCDATE()), DATEADD(day, 8, GETUTCDATE()), '09:00:00', 2, 300, 'USD', '', 0, 0, DATEADD(day, -3, GETUTCDATE()), GETUTCDATE());
SET @BookingId = SCOPE_IDENTITY();


INSERT INTO tourguide_TourBookingParticipants (BookingId, ParticipantType, AgeType, FirstName, MiddleName, LastName, DateOfBirth, Gender, Nationality, SpecialNeeds, DietaryRequirements, Price)
VALUES (@BookingId, 0, 0, 'Sarah', '', 'Smith', '1990-01-01', 1, 'US', '', '', 150.0);


INSERT INTO tourguide_TourBookingParticipants (BookingId, ParticipantType, AgeType, FirstName, MiddleName, LastName, DateOfBirth, Gender, Nationality, SpecialNeeds, DietaryRequirements, Price)
VALUES (@BookingId, 0, 0, 'Fatima', '', 'Garcia', '1990-01-01', 1, 'US', '', '', 150.0);

SELECT TOP 1 @TourId = Id FROM @TourIds ORDER BY NEWID();

INSERT INTO tourguide_TourBookings (UserId, TourId, TourGuideId, BookingDate, TourDate, TourTime, ParticipantsCount, TotalPrice, Currency, SpecialRequests, PaymentStatus, Status, CreatedAt, UpdatedAt)
VALUES (@UserId, @TourId, @TourGuideId, DATEADD(day, -8, GETUTCDATE()), DATEADD(day, -5, GETUTCDATE()), '09:00:00', 3, 600, 'USD', 'Vegetarian meals for 2.', 1, 2, DATEADD(day, -8, GETUTCDATE()), GETUTCDATE());
SET @BookingId = SCOPE_IDENTITY();


INSERT INTO tourguide_TourBookingParticipants (BookingId, ParticipantType, AgeType, FirstName, MiddleName, LastName, DateOfBirth, Gender, Nationality, SpecialNeeds, DietaryRequirements, Price)
VALUES (@BookingId, 0, 0, 'Sarah', '', 'Smith', '1990-01-01', 0, 'US', '', '', 200.0);


INSERT INTO tourguide_TourBookingParticipants (BookingId, ParticipantType, AgeType, FirstName, MiddleName, LastName, DateOfBirth, Gender, Nationality, SpecialNeeds, DietaryRequirements, Price)
VALUES (@BookingId, 0, 0, 'Fatima', '', 'Jones', '1990-01-01', 1, 'US', '', '', 200.0);


INSERT INTO tourguide_TourBookingParticipants (BookingId, ParticipantType, AgeType, FirstName, MiddleName, LastName, DateOfBirth, Gender, Nationality, SpecialNeeds, DietaryRequirements, Price)
VALUES (@BookingId, 0, 0, 'Michael', '', 'Smith', '1990-01-01', 0, 'US', '', '', 200.0);

SELECT TOP 1 @TourId = Id FROM @TourIds ORDER BY NEWID();

INSERT INTO tourguide_TourBookings (UserId, TourId, TourGuideId, BookingDate, TourDate, TourTime, ParticipantsCount, TotalPrice, Currency, SpecialRequests, PaymentStatus, Status, CreatedAt, UpdatedAt)
VALUES (@UserId, @TourId, @TourGuideId, DATEADD(day, -5, GETUTCDATE()), DATEADD(day, -15, GETUTCDATE()), '09:00:00', 2, 250, 'USD', '', 1, 2, DATEADD(day, -5, GETUTCDATE()), GETUTCDATE());
SET @BookingId = SCOPE_IDENTITY();


INSERT INTO tourguide_TourBookingParticipants (BookingId, ParticipantType, AgeType, FirstName, MiddleName, LastName, DateOfBirth, Gender, Nationality, SpecialNeeds, DietaryRequirements, Price)
VALUES (@BookingId, 0, 0, 'Sarah', '', 'Garcia', '1990-01-01', 1, 'US', '', '', 125.0);


INSERT INTO tourguide_TourBookingParticipants (BookingId, ParticipantType, AgeType, FirstName, MiddleName, LastName, DateOfBirth, Gender, Nationality, SpecialNeeds, DietaryRequirements, Price)
VALUES (@BookingId, 0, 0, 'Jane', '', 'Miller', '1990-01-01', 0, 'US', '', '', 125.0);

SELECT TOP 1 @TourId = Id FROM @TourIds ORDER BY NEWID();

INSERT INTO tourguide_TourBookings (UserId, TourId, TourGuideId, BookingDate, TourDate, TourTime, ParticipantsCount, TotalPrice, Currency, SpecialRequests, PaymentStatus, Status, CreatedAt, UpdatedAt)
VALUES (@UserId, @TourId, @TourGuideId, DATEADD(day, -3, GETUTCDATE()), DATEADD(day, -30, GETUTCDATE()), '09:00:00', 1, 100, 'USD', 'Photography tour.', 1, 2, DATEADD(day, -3, GETUTCDATE()), GETUTCDATE());
SET @BookingId = SCOPE_IDENTITY();


INSERT INTO tourguide_TourBookingParticipants (BookingId, ParticipantType, AgeType, FirstName, MiddleName, LastName, DateOfBirth, Gender, Nationality, SpecialNeeds, DietaryRequirements, Price)
VALUES (@BookingId, 0, 0, 'John', '', 'Williams', '1990-01-01', 0, 'US', '', '', 100.0);

SELECT TOP 1 @TourId = Id FROM @TourIds ORDER BY NEWID();

INSERT INTO tourguide_TourBookings (UserId, TourId, TourGuideId, BookingDate, TourDate, TourTime, ParticipantsCount, TotalPrice, Currency, SpecialRequests, PaymentStatus, Status, CreatedAt, UpdatedAt)
VALUES (@UserId, @TourId, @TourGuideId, DATEADD(day, -8, GETUTCDATE()), DATEADD(day, 10, GETUTCDATE()), '09:00:00', 2, 400, 'USD', 'Flight got cancelled.', 3, 3, DATEADD(day, -8, GETUTCDATE()), GETUTCDATE());
SET @BookingId = SCOPE_IDENTITY();


INSERT INTO tourguide_TourBookingParticipants (BookingId, ParticipantType, AgeType, FirstName, MiddleName, LastName, DateOfBirth, Gender, Nationality, SpecialNeeds, DietaryRequirements, Price)
VALUES (@BookingId, 0, 0, 'Ali', '', 'Miller', '1990-01-01', 0, 'US', '', '', 200.0);


INSERT INTO tourguide_TourBookingParticipants (BookingId, ParticipantType, AgeType, FirstName, MiddleName, LastName, DateOfBirth, Gender, Nationality, SpecialNeeds, DietaryRequirements, Price)
VALUES (@BookingId, 0, 0, 'John', '', 'Miller', '1990-01-01', 0, 'US', '', '', 200.0);

SELECT TOP 1 @TourId = Id FROM @TourIds ORDER BY NEWID();

INSERT INTO tourguide_TourBookings (UserId, TourId, TourGuideId, BookingDate, TourDate, TourTime, ParticipantsCount, TotalPrice, Currency, SpecialRequests, PaymentStatus, Status, CreatedAt, UpdatedAt)
VALUES (@UserId, @TourId, @TourGuideId, DATEADD(day, -1, GETUTCDATE()), DATEADD(day, -2, GETUTCDATE()), '09:00:00', 5, 1000, 'USD', '', 3, 3, DATEADD(day, -1, GETUTCDATE()), GETUTCDATE());
SET @BookingId = SCOPE_IDENTITY();


INSERT INTO tourguide_TourBookingParticipants (BookingId, ParticipantType, AgeType, FirstName, MiddleName, LastName, DateOfBirth, Gender, Nationality, SpecialNeeds, DietaryRequirements, Price)
VALUES (@BookingId, 0, 0, 'Jane', '', 'Miller', '1990-01-01', 0, 'US', '', '', 200.0);


INSERT INTO tourguide_TourBookingParticipants (BookingId, ParticipantType, AgeType, FirstName, MiddleName, LastName, DateOfBirth, Gender, Nationality, SpecialNeeds, DietaryRequirements, Price)
VALUES (@BookingId, 0, 0, 'Ali', '', 'Johnson', '1990-01-01', 1, 'US', '', '', 200.0);


INSERT INTO tourguide_TourBookingParticipants (BookingId, ParticipantType, AgeType, FirstName, MiddleName, LastName, DateOfBirth, Gender, Nationality, SpecialNeeds, DietaryRequirements, Price)
VALUES (@BookingId, 0, 0, 'Emma', '', 'Smith', '1990-01-01', 1, 'US', '', '', 200.0);


INSERT INTO tourguide_TourBookingParticipants (BookingId, ParticipantType, AgeType, FirstName, MiddleName, LastName, DateOfBirth, Gender, Nationality, SpecialNeeds, DietaryRequirements, Price)
VALUES (@BookingId, 0, 0, 'David', '', 'Smith', '1990-01-01', 1, 'US', '', '', 200.0);


INSERT INTO tourguide_TourBookingParticipants (BookingId, ParticipantType, AgeType, FirstName, MiddleName, LastName, DateOfBirth, Gender, Nationality, SpecialNeeds, DietaryRequirements, Price)
VALUES (@BookingId, 0, 0, 'Emma', '', 'Jones', '1990-01-01', 0, 'US', '', '', 200.0);

SELECT TOP 1 @TourId = Id FROM @TourIds ORDER BY NEWID();

INSERT INTO tourguide_TourBookings (UserId, TourId, TourGuideId, BookingDate, TourDate, TourTime, ParticipantsCount, TotalPrice, Currency, SpecialRequests, PaymentStatus, Status, CreatedAt, UpdatedAt)
VALUES (@UserId, @TourId, @TourGuideId, DATEADD(day, -19, GETUTCDATE()), DATEADD(day, 20, GETUTCDATE()), '09:00:00', 2, 500, 'USD', 'Anniversary celebration.', 1, 1, DATEADD(day, -19, GETUTCDATE()), GETUTCDATE());
SET @BookingId = SCOPE_IDENTITY();


INSERT INTO tourguide_TourBookingParticipants (BookingId, ParticipantType, AgeType, FirstName, MiddleName, LastName, DateOfBirth, Gender, Nationality, SpecialNeeds, DietaryRequirements, Price)
VALUES (@BookingId, 0, 0, 'Sarah', '', 'Garcia', '1990-01-01', 1, 'US', '', '', 250.0);


INSERT INTO tourguide_TourBookingParticipants (BookingId, ParticipantType, AgeType, FirstName, MiddleName, LastName, DateOfBirth, Gender, Nationality, SpecialNeeds, DietaryRequirements, Price)
VALUES (@BookingId, 0, 0, 'David', '', 'Williams', '1990-01-01', 0, 'US', '', '', 250.0);

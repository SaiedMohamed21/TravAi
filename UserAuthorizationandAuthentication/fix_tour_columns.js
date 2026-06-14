const fs = require('fs');
const file = 'Scripts/seed_january_tour_payout_test_data.sql';
let text = fs.readFileSync(file, 'utf8');

text = text.replace(
  /INSERT INTO tourguide_Tours \(TourGuideId, Title, Description, CityId, Price, DurationHours, MaxParticipants, MeetingPoint, Itinerary, Includes, Excludes, Status, Type, TourLocationDetails, CancellationPolicy, TotalReviews, AverageRating, StartTime, EndTime\)/g,
  'INSERT INTO tourguide_Tours (TourGuideId, TourTitle, Currency, TransportIncluded, MealsIncluded, IsAccessible, Customizable, CancellationPolicy, Active, CreatedAt)'
);

text = text.replace(
  /VALUES \(@TourA_Id, 'Tour A', 'Test Tour A', 1, 100, 2, 10, 'Center', '\[\]', '\[\]', '\[\]', 'Active', 'Public', '', 'Strict', 0, 0, '10:00:00', '12:00:00'\);/g,
  "VALUES (@TourA_Id, 'Tour A', 'USD', 0, 0, 0, 0, 1, 1, GETUTCDATE());"
);

text = text.replace(
  /VALUES \(@TourB_Id, 'Tour B', 'Test Tour B', 1, 100, 2, 10, 'Center', '\[\]', '\[\]', '\[\]', 'Active', 'Public', '', 'Strict', 0, 0, '10:00:00', '12:00:00'\);/g,
  "VALUES (@TourB_Id, 'Tour B', 'USD', 0, 0, 0, 0, 1, 1, GETUTCDATE());"
);

text = text.replace(
  /VALUES \(@TourC_Id, 'Tour C 1', 'Test Tour C 1', 1, 100, 2, 10, 'Center', '\[\]', '\[\]', '\[\]', 'Active', 'Public', '', 'Strict', 0, 0, '10:00:00', '12:00:00'\);/g,
  "VALUES (@TourC_Id, 'Tour C 1', 'USD', 0, 0, 0, 0, 1, 1, GETUTCDATE());"
);

text = text.replace(
  /VALUES \(@TourC_Id, 'Tour C 2', 'Test Tour C 2', 1, 100, 2, 10, 'Center', '\[\]', '\[\]', '\[\]', 'Active', 'Public', '', 'Strict', 0, 0, '10:00:00', '12:00:00'\);/g,
  "VALUES (@TourC_Id, 'Tour C 2', 'USD', 0, 0, 0, 0, 1, 1, GETUTCDATE());"
);

text = text.replace(
  /VALUES \(@TourD_Id, 'Tour D 1', 'Test Tour D 1', 1, 100, 2, 10, 'Center', '\[\]', '\[\]', '\[\]', 'Active', 'Public', '', 'Strict', 0, 0, '10:00:00', '12:00:00'\);/g,
  "VALUES (@TourD_Id, 'Tour D 1', 'USD', 0, 0, 0, 0, 1, 1, GETUTCDATE());"
);

text = text.replace(
  /VALUES \(@TourD_Id, 'Tour D 2', 'Test Tour D 2', 1, 100, 2, 10, 'Center', '\[\]', '\[\]', '\[\]', 'Active', 'Public', '', 'Strict', 0, 0, '10:00:00', '12:00:00'\);/g,
  "VALUES (@TourD_Id, 'Tour D 2', 'USD', 0, 0, 0, 0, 1, 1, GETUTCDATE());"
);

fs.writeFileSync(file, text);
console.log('Fixed tour scripts');

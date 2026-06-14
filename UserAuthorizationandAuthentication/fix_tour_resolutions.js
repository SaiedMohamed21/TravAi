const fs = require('fs');
const file = 'Scripts/seed_january_tour_payout_test_data.sql';
let text = fs.readFileSync(file, 'utf8');

text = text.replace(
  /INSERT INTO tourguide_TourBookingResolutions \(OriginalBookingId, UserAction, ProviderAction, Status, RefundAmount, CreatedAt\)/g,
  'INSERT INTO tourguide_TourBookingResolutions (OriginalBookingId, UserId, ResolutionType, RefundAmount, ResolvedAt)'
);

text = text.replace(
  /\(@B_Booking3, 'Cancel', 'Approve', 'Resolved', 20\.00, GETUTCDATE\(\)\),/g,
  "(@B_Booking3, @UserId, 'Refund', 20.00, GETUTCDATE()),"
);

text = text.replace(
  /\(@B_Booking4, 'Cancel', 'Approve', 'Resolved', 50\.00, GETUTCDATE\(\)\),/g,
  "(@B_Booking4, @UserId, 'Refund', 50.00, GETUTCDATE()),"
);

text = text.replace(
  /\(@B_Booking5, 'Cancel', 'Approve', 'Resolved', 100\.00, GETUTCDATE\(\)\);/g,
  "(@B_Booking5, @UserId, 'Refund', 100.00, GETUTCDATE());"
);

fs.writeFileSync(file, text);
console.log('Fixed tour resolutions');

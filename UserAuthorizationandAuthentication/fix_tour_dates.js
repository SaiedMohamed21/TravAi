const fs = require('fs');
const file = 'Scripts/seed_january_tour_payout_test_data.sql';
let text = fs.readFileSync(file, 'utf8');

text = text.replace(/GETUTCDATE\(\), GETUTCDATE\(\)\);/g, 'GETUTCDATE());');
text = text.replace(/GETUTCDATE\(\), GETUTCDATE\(\)/g, 'GETUTCDATE()');

text = text.replace(/ProviderPayoutAccountNumber, StripeConnectedAccountId, Currency, IsActive, CreatedAt, UpdatedAt/g, 'ProviderPayoutAccountNumber, StripeConnectedAccountId, Currency, IsActive, CreatedAt');

fs.writeFileSync(file, text);
console.log('Fixed tour dates');

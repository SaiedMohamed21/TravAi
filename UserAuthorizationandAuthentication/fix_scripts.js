const fs = require('fs');
const files = ['Scripts/seed_january_airline_payout_test_data.sql', 'Scripts/seed_january_tour_payout_test_data.sql'];
files.forEach(f => {
  let text = fs.readFileSync(f, 'utf8');
  text = text.replace(/StripeConnectedAccountId, IsActive/g, 'ProviderPayoutAccountNumber, StripeConnectedAccountId, IsActive');
  text = text.replace(/@StripeAccountId/g, "''test_acct'', @StripeAccountId");
  text = text.replace(/@StripeAccountIdTour/g, "''test_acct'', @StripeAccountIdTour");
  fs.writeFileSync(f, text);
});
console.log('Fixed scripts');

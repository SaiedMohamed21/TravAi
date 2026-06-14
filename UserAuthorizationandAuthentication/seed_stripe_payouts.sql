-- Seed Hotels
INSERT INTO admin_ProviderStripePayoutAccounts
(
    ProviderType,
    ProviderId,
    ProviderNameSnapshot,
    ProviderPayoutAccountNumber,
    StripeConnectedAccountId,
    StripeAccountDisplayName,
    Currency,
    BankName,
    BankLast4,
    IsActive,
    CreatedAt,
    UpdatedAt
)
SELECT
    1, -- 1 corresponds to ProviderType.Hotel enum value
    h.Id,
    h.HotelName,
    CONCAT('PAY-HOTEL-', RIGHT('0000' + CAST(h.Id AS varchar(20)), 4)),
    'acct_1ThtTGFP2e17lKge',
    'Hotel Provider Stripe Account',
    'usd',
    'STRIPE TEST BANK',
    '6789',
    1,
    GETUTCDATE(),
    GETUTCDATE()
FROM hotel_Hotels h
WHERE NOT EXISTS (
    SELECT 1
    FROM admin_ProviderStripePayoutAccounts a
    WHERE a.ProviderType = 1
      AND a.ProviderId = h.Id
);

-- Seed TourGuides
INSERT INTO admin_ProviderStripePayoutAccounts
(
    ProviderType,
    ProviderId,
    ProviderNameSnapshot,
    ProviderPayoutAccountNumber,
    StripeConnectedAccountId,
    StripeAccountDisplayName,
    Currency,
    BankName,
    BankLast4,
    IsActive,
    CreatedAt,
    UpdatedAt
)
SELECT
    2, -- 2 corresponds to ProviderType.TourGuide enum value
    tg.Id,
    tg.Name,
    CONCAT('PAY-TOURGUIDE-', RIGHT('0000' + CAST(tg.Id AS varchar(20)), 4)),
    'acct_1ThtTGFP2e17lKge',
    'Tour Guide Provider Stripe Account',
    'usd',
    'STRIPE TEST BANK',
    '6789',
    1,
    GETUTCDATE(),
    GETUTCDATE()
FROM tourguide_TourGuides tg
WHERE NOT EXISTS (
    SELECT 1
    FROM admin_ProviderStripePayoutAccounts a
    WHERE a.ProviderType = 2
      AND a.ProviderId = tg.Id
);

-- Seed Airlines
INSERT INTO admin_ProviderStripePayoutAccounts
(
    ProviderType,
    ProviderId,
    ProviderNameSnapshot,
    ProviderPayoutAccountNumber,
    StripeConnectedAccountId,
    StripeAccountDisplayName,
    Currency,
    BankName,
    BankLast4,
    IsActive,
    CreatedAt,
    UpdatedAt
)
SELECT
    3, -- 3 corresponds to ProviderType.Airline enum value
    al.Id,
    al.Name,
    CONCAT('PAY-AIRLINE-', RIGHT('0000' + CAST(al.Id AS varchar(20)), 4)),
    'acct_1ThtTGFP2e17lKge',
    'Airline Provider Stripe Account',
    'usd',
    'STRIPE TEST BANK',
    '6789',
    1,
    GETUTCDATE(),
    GETUTCDATE()
FROM airline_Airlines al
WHERE NOT EXISTS (
    SELECT 1
    FROM admin_ProviderStripePayoutAccounts a
    WHERE a.ProviderType = 3
      AND a.ProviderId = al.Id
);

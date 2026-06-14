SET NOCOUNT ON;

DECLARE @UserId BIGINT = (SELECT TOP 1 Id FROM Users WHERE Role = 'Admin');
IF @UserId IS NULL SET @UserId = 1;

-- Airline Missing
DECLARE @AirlineA_Id BIGINT = (SELECT Id FROM airline_Airlines WHERE Name = 'PAYOUT_TEST_JANUARY_2026_AIRLINE_A_NO_REFUND');
DECLARE @AirlineB_Id BIGINT = (SELECT Id FROM airline_Airlines WHERE Name = 'PAYOUT_TEST_JANUARY_2026_AIRLINE_B_WITH_REFUNDS');
DECLARE @AirlineC_Id BIGINT = (SELECT Id FROM airline_Airlines WHERE Name = 'PAYOUT_TEST_JANUARY_2026_AIRLINE_C_ONE_FINE');
DECLARE @AirlineD_Id BIGINT = (SELECT Id FROM airline_Airlines WHERE Name = 'PAYOUT_TEST_JANUARY_2026_AIRLINE_D_TWO_FINES');

DELETE FROM admin_ProviderFines WHERE ProviderType = 'Airline' AND ProviderId IN (@AirlineA_Id, @AirlineB_Id, @AirlineC_Id, @AirlineD_Id);

INSERT INTO admin_ProviderFines (ProviderType, ProviderId, SourceType, ComplaintId, Amount, Currency, Reason, Status, CreatedByAdminUserId, CreatedAt)
VALUES ('Airline', @AirlineC_Id, 'Complaint', NULL, 30.00, 'USD', 'Test complaint fine C', 'Active', @UserId, '2025-12-10 00:00:00');

INSERT INTO admin_ProviderFines (ProviderType, ProviderId, SourceType, ComplaintId, Amount, Currency, Reason, Status, CreatedByAdminUserId, CreatedAt)
VALUES 
('Airline', @AirlineD_Id, 'Complaint', NULL, 25.00, 'USD', 'Test complaint fine D1', 'Active', @UserId, '2025-12-01 00:00:00'),
('Airline', @AirlineD_Id, 'Complaint', NULL, 40.00, 'USD', 'Test complaint fine D2', 'Active', @UserId, '2025-12-20 00:00:00');

DECLARE @StripeAccountId NVARCHAR(255) = (SELECT TOP 1 StripeConnectedAccountId FROM admin_ProviderStripePayoutAccounts WHERE ProviderType = 1 AND IsActive = 1 ORDER BY Id DESC);
IF @StripeAccountId IS NULL SET @StripeAccountId = 'acct_1OuXXXX';

IF NOT EXISTS (SELECT 1 FROM admin_ProviderStripePayoutAccounts WHERE ProviderType = 3 AND ProviderId = @AirlineA_Id)
    INSERT INTO admin_ProviderStripePayoutAccounts (ProviderType, ProviderId, ProviderPayoutAccountNumber, StripeConnectedAccountId, Currency, IsActive, CreatedAt, UpdatedAt) VALUES (3, @AirlineA_Id, 'TEST-ACCT-A', @StripeAccountId, 'USD', 1, GETUTCDATE(), GETUTCDATE());
IF NOT EXISTS (SELECT 1 FROM admin_ProviderStripePayoutAccounts WHERE ProviderType = 3 AND ProviderId = @AirlineB_Id)
    INSERT INTO admin_ProviderStripePayoutAccounts (ProviderType, ProviderId, ProviderPayoutAccountNumber, StripeConnectedAccountId, Currency, IsActive, CreatedAt, UpdatedAt) VALUES (3, @AirlineB_Id, 'TEST-ACCT-B', @StripeAccountId, 'USD', 1, GETUTCDATE(), GETUTCDATE());
IF NOT EXISTS (SELECT 1 FROM admin_ProviderStripePayoutAccounts WHERE ProviderType = 3 AND ProviderId = @AirlineC_Id)
    INSERT INTO admin_ProviderStripePayoutAccounts (ProviderType, ProviderId, ProviderPayoutAccountNumber, StripeConnectedAccountId, Currency, IsActive, CreatedAt, UpdatedAt) VALUES (3, @AirlineC_Id, 'TEST-ACCT-C', @StripeAccountId, 'USD', 1, GETUTCDATE(), GETUTCDATE());
IF NOT EXISTS (SELECT 1 FROM admin_ProviderStripePayoutAccounts WHERE ProviderType = 3 AND ProviderId = @AirlineD_Id)
    INSERT INTO admin_ProviderStripePayoutAccounts (ProviderType, ProviderId, ProviderPayoutAccountNumber, StripeConnectedAccountId, Currency, IsActive, CreatedAt, UpdatedAt) VALUES (3, @AirlineD_Id, 'TEST-ACCT-D', @StripeAccountId, 'USD', 1, GETUTCDATE(), GETUTCDATE());


-- Tour Missing
DECLARE @TourA_Id BIGINT = (SELECT Id FROM tourguide_TourGuides WHERE Name = 'PAYOUT_TEST_JANUARY_2026_TOURGUIDE_A_NO_REFUND');
DECLARE @TourB_Id BIGINT = (SELECT Id FROM tourguide_TourGuides WHERE Name = 'PAYOUT_TEST_JANUARY_2026_TOURGUIDE_B_WITH_REFUNDS');
DECLARE @TourC_Id BIGINT = (SELECT Id FROM tourguide_TourGuides WHERE Name = 'PAYOUT_TEST_JANUARY_2026_TOURGUIDE_C_ONE_FINE');
DECLARE @TourD_Id BIGINT = (SELECT Id FROM tourguide_TourGuides WHERE Name = 'PAYOUT_TEST_JANUARY_2026_TOURGUIDE_D_TWO_FINES');

DELETE FROM admin_ProviderFines WHERE ProviderType = 'TourGuide' AND ProviderId IN (@TourA_Id, @TourB_Id, @TourC_Id, @TourD_Id);

INSERT INTO admin_ProviderFines (ProviderType, ProviderId, SourceType, ComplaintId, Amount, Currency, Reason, Status, CreatedByAdminUserId, CreatedAt)
VALUES ('TourGuide', @TourC_Id, 'Complaint', NULL, 30.00, 'USD', 'Test complaint fine C', 'Active', @UserId, '2025-12-10 00:00:00');

INSERT INTO admin_ProviderFines (ProviderType, ProviderId, SourceType, ComplaintId, Amount, Currency, Reason, Status, CreatedByAdminUserId, CreatedAt)
VALUES 
('TourGuide', @TourD_Id, 'Complaint', NULL, 25.00, 'USD', 'Test complaint fine D1', 'Active', @UserId, '2025-12-01 00:00:00'),
('TourGuide', @TourD_Id, 'Complaint', NULL, 40.00, 'USD', 'Test complaint fine D2', 'Active', @UserId, '2025-12-20 00:00:00');


IF NOT EXISTS (SELECT 1 FROM admin_ProviderStripePayoutAccounts WHERE ProviderType = 2 AND ProviderId = @TourA_Id)
    INSERT INTO admin_ProviderStripePayoutAccounts (ProviderType, ProviderId, ProviderPayoutAccountNumber, StripeConnectedAccountId, Currency, IsActive, CreatedAt, UpdatedAt) VALUES (2, @TourA_Id, 'TEST-ACCT-TA', @StripeAccountId, 'USD', 1, GETUTCDATE(), GETUTCDATE());
IF NOT EXISTS (SELECT 1 FROM admin_ProviderStripePayoutAccounts WHERE ProviderType = 2 AND ProviderId = @TourB_Id)
    INSERT INTO admin_ProviderStripePayoutAccounts (ProviderType, ProviderId, ProviderPayoutAccountNumber, StripeConnectedAccountId, Currency, IsActive, CreatedAt, UpdatedAt) VALUES (2, @TourB_Id, 'TEST-ACCT-TB', @StripeAccountId, 'USD', 1, GETUTCDATE(), GETUTCDATE());
IF NOT EXISTS (SELECT 1 FROM admin_ProviderStripePayoutAccounts WHERE ProviderType = 2 AND ProviderId = @TourC_Id)
    INSERT INTO admin_ProviderStripePayoutAccounts (ProviderType, ProviderId, ProviderPayoutAccountNumber, StripeConnectedAccountId, Currency, IsActive, CreatedAt, UpdatedAt) VALUES (2, @TourC_Id, 'TEST-ACCT-TC', @StripeAccountId, 'USD', 1, GETUTCDATE(), GETUTCDATE());
IF NOT EXISTS (SELECT 1 FROM admin_ProviderStripePayoutAccounts WHERE ProviderType = 2 AND ProviderId = @TourD_Id)
    INSERT INTO admin_ProviderStripePayoutAccounts (ProviderType, ProviderId, ProviderPayoutAccountNumber, StripeConnectedAccountId, Currency, IsActive, CreatedAt, UpdatedAt) VALUES (2, @TourD_Id, 'TEST-ACCT-TD', @StripeAccountId, 'USD', 1, GETUTCDATE(), GETUTCDATE());

PRINT 'Missing test data seeded successfully.';

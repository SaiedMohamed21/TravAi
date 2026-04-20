# Multi-Vendor Travel Marketplace Architecture

## 1. System Flows

### Role-Upgrade Flow
1.  **Initial State**: User registers as a standard `user`.
2.  **Application**: User applies for a provider role (`tourguide`, `hotel_company`, `airplane_company`) by submitting legal data (licenses, tax IDs).
3.  **Verification**: Admin reviews submitted data.
4.  **Approval**: Upon approval, specific tables (e.g., `airlines`, `hotels`, `tour_guides`) update `verified=true`. User role updates, granting CRUD access.

### Core Service Modules
-   **Airlines**: Manage flights, segments, baggage. Bookings track passengers (passport/emergency data).
-   **Hotels**: Manage listings, rooms, amenities. Bookings are date-based (checkin/checkout), tracking occupancy and room details.
-   **Tours**: Managed by `tour_guides`. Defined by schedules, languages, and dates. Bookings track participants.

### Financial & Stripe Connect
-   **Structure**: Uses Stripe Connect for payouts.
-   **Entities**: `stripe_accounts` for providers, `payout_transactions` for history.
-   **Reconciliation**: `amount_paid` = `platform_commission` (App Profit) + `provider_net_amount` (Provider Profit).
-   **Payments**: Supports saved cards via `user_saved_cards` (`stripe_payment_method_id`).

### Data Integrity
-   **Snapshots**: Booking tables (`hotel_booking_rooms`, `booking_passengers`) store data snapshots (price, names) to prevent historical corruption if listings change.
-   **Reviews**: Linked to `booking_id` for "Verified Purchase" status. require `is_approved` by admin/system.

## 2. Database Schema

### Users & Authentication
-   **Users**: `id`, `user_name`, `email`, `role`, `status`, `provider_id` (social).
-   **UserPhones**: One-to-many, verified status.
-   **UserSavedCards**: Stores Stripe `pm_xxxx`, card brand, last4.

### Passengers (Flights)
-   **SavedFlightPassengerDetails**: Profile for frequent travelers.
-   **SavedFlightPassengerCompanions**: Friends/Family linked to main passenger.
-   **Phones/Emergency**: Normalized tables for contact info.

### Airlines Module
-   **Airlines**: Profile, license, verification status, rating.
-   **Flights**: Source/Dest, times, prices, baggage, status (`DRAFT`, `SCHEDULED`, ...).
-   **FlightBookings**: `PNR`, price, status.
-   **BookingPassengers**: Snapshot of passenger data specific to the booking.
-   **FlightPayments**: Splits `platform_commission` vs `provider_net_amount`.

### Hotels Module
-   **Hotels**: Company profile, verification.
-   **HotelListings**: Specific property details, location, ratings.
-   **HotelRooms**: Inventory definitions, pricing, availability.
-   **HotelBookings**: Checkin/out, total price.
-   **HotelBookingRooms**: Snapshot of room details at booking time.
-   **HotelPayments**: Financial reconciliation.

### Tours Module
-   **TourGuides**: Profile, license, verification.
-   **Tours**: Description, schedule, itinerary, price.
-   **TourBookings**: Date, participants.
-   **TourPayments**: Financial reconciliation.

### Reviews
-   **Common Structure**: `user_id`, `rating` (1-5), `comment`, `is_approved`, `booking_id` (optional/verified).
-   **Types**: `hotel_reviews`, `airline_reviews`, `tour_reviews`.

### Payouts & Stripe
-   **StripeAccounts**: Stores `acct_xxxx`, onboarding status (`details_submitted`, `charges_enabled`).
-   **PayoutTransactions**: Record of transfers to providers (`amount`, `currency`, `stripe_transfer_id`).

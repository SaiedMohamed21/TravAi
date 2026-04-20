# Hotel Booking System Test Plan

This document outlines the steps to verify the Hotel Booking System functionality using Swagger UI or Postman.

## 1. Prerequisites
- **Server**: Ensure `dotnet watch run` is running.
- **Base URL**: `http://localhost:5210` (or your port).
- **Users**:
  - `user1@gmail.com` (password: `Password123!`) - Regular User
  - `hotel1@gmail.com` (password: `Password123!`) - Hotel Owner (Cairo listing)

## 2. Test Scenarios

### Scenario A: Public Room Search
**Goal**: Find an available room.
1. **Endpoint**: `GET /api/PublicRooms`
2. **Parameters**:
   - `checkInDate`: Tomorrow
   - `checkOutDate`: Day after tomorrow
   - `location`: "Cairo"
3. **Expected Result**: 
   - List of available rooms in Cairo.
   - Note the `roomId` and `listingId` of a room managed by `Sunrise Group` (listing1).

### Scenario B: Create a Booking
**Goal**: Book a specific room.
1. **Login**: `POST /api/Auth/login` with `user1@gmail.com`. Copy the `token`.
2. **Endpoint**: `POST /api/HotelBookings`
3. **Headers**: `Authorization: Bearer <token>`
4. **Body**:
   ```json
   {
     "listingId": <listing_id>,
     "checkInDate": "2026-03-01",
     "checkOutDate": "2026-03-03",
     "roomIds": [<room_id>]
   }
   ```
5. **Expected Result**: `201 Created` with booking details and status `Pending`.

### Scenario C: Verify Booking & Availability
**Goal**: Ensure booking is saved and availability is updated.
1. **Endpoint**: `GET /api/HotelBookings/mine`
2. **Expected Result**: The new booking should appear in the list.
3. **Re-run Scenario A** for the same dates and room.
   - **Expected Result**: If the room had only 1 availability, it should NO LONGER appear in search, or its available count should decrease.

### Scenario D: Hotel Owner View
**Goal**: Hotel owner sees the new booking.
1. **Login**: `POST /api/Auth/login` with `hotel1@gmail.com`. Copy the `token`.
2. **Endpoint**: `GET /api/HotelBookings/hotel/<listing_id>`
3. **Expected Result**: List of bookings for their hotel, including the one from User1.

### Scenario E: Update Booking Status
**Goal**: Owner confirms the booking.
1. **Endpoint**: `PUT /api/HotelBookings/<booking_id>/status`
2. **Body**: `{"action": "Confirmed"}`
3. **Expected Result**: `200 OK`, status changes to `Confirmed`.

### Scenario F: Cancel Booking
**Goal**: User cancels the booking.
1. **Login**: As `user1` again.
2. **Endpoint**: `PUT /api/HotelBookings/<booking_id>/cancel`
3. **Expected Result**: `200 OK`, booking status becomes `Cancelled`.

### Scenario G: Overbooking Prevention
**Goal**: Try to book a fully booked room.
1. Identify a room with `Availability: 0` or try to book the same room for the same dates multiple times until availability runs out.
2. **Expected Result**: `409 Conflict` - "Room ... is not available for the selected dates."
### Scenario H: Smart Availability Logic
**Goal**: Verify that availability changes based on selected dates.
1. **Check Today**: Search for a hotel without dates (defaults to today). Note `availableRooms`.
2. **Book Today**: Create a booking for *today* (1 night).
3. **Re-Check Today**: Search again (no dates). `availableRooms` should decrease by 1.
4. **Check Future**: Search for dates *next month*. `availableRooms` should be back to full capacity (showing the room is free in the future).

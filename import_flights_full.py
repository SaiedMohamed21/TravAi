import csv
import sys
import datetime

city_to_code = {
    'Aswan': 'ASW', 'Cairo': 'CAI', 'Frankfurt': 'FRA', 'Hurghada': 'HRG', 
    'London': 'LHR', 'Luxor': 'LXR', 'Sharm El-Sheikh': 'SSH', 'Moscow': 'SVO', 
    'Warsaw': 'WAW', 'Jaddah': 'JED'
}

airline_map = {
    'EgyptAir': 169, 'Saudia': 170, 'Turkish Airlines': 171, 'Emirates': 172, 'Qatar Airways': 173,
    'Flynas': 174, 'flyadeal': 175, 'Air Arabia Egypt': 176, 'Air Cairo': 177, 'Wizz Air': 178,
    'easyJet': 179, 'Lufthansa': 180, 'British Airways': 181, 'Air France': 182, 'Aegean': 183,
    'Royal Jordanian': 184, 'Gulf Air': 185, 'Etihad': 186, 'Austrian': 187, 'SWISS': 188,
    'ITA': 189, 'TAROM': 190, 'Pegasus': 191, 'SunExpress': 192, 'Condor': 193, 'TUI fly': 194,
    'TUI Airways': 195, 'Royal Air Maroc': 196, 'Tunisair': 197, 'Ethiopian': 198, 'Oman Air': 199,
    'Azerbaijan Airlines': 200, 'Nile Air': 201, 'AJet': 171, 'LOT': 180
}

sql_statements = [
    "SET NOCOUNT ON;",
    "DELETE FROM airline_FlightLayovers;",
    "DELETE FROM airline_FlightSegments;",
    "DELETE FROM airline_Flights;",
    "DECLARE @FlightId BIGINT;"
]

with open(r"C:\Users\saied mohamed\Desktop\data\flights.csv", encoding="utf-8-sig") as f:
    reader = csv.DictReader(f)
    for row in reader:
        dep = city_to_code.get(row['departure_city'])
        arr = city_to_code.get(row['arrival_city'])
        
        if not dep or not arr: continue
        
        airlines_in_str = row['airline'].split(' and ')[0].split(', ')[0]
        airline_id = airline_map.get(airlines_in_str, "NULL")
        
        dep_dt = row['departure_datetime']
        arr_dt = row['arrival_datetime']
        duration = row['duration']
        try:
            dur_min = int(float(row['duration_minutes']))
        except:
            dur_min = 0
            
        try:
            price = float(row['price_USD'])
        except:
            price = 0
            
        try:
            stops = int(float(row['stops']))
        except:
            stops = 0
            
        f_class = row['flight_class'].replace("'", "''")
        
        # Flight
        sql_flight = f"INSERT INTO airline_Flights (DepartureAirportCode, ArrivalAirportCode, DepartureTime, ArrivalTime, Price, AirlineId, FlightClass, Duration, DurationMinutes, Status, Currency, NumberOfStops) VALUES ('{dep}', '{arr}', '{dep_dt}', '{arr_dt}', {price}, {airline_id}, '{f_class}', '{duration}', {dur_min}, 'Active', 'USD', {stops});"
        sql_statements.append(sql_flight)
        sql_statements.append("SET @FlightId = SCOPE_IDENTITY();")
        
        # Amenities & Legroom
        amenities = []
        if str(row.get('has_wifi', '0')) == '1': amenities.append('WiFi')
        if str(row.get('has_power', '0')) == '1': amenities.append('Power')
        if str(row.get('has_entertainment', '0')) == '1': amenities.append('Entertainment')
        amenities_str = ", ".join(amenities)
        
        try:
            legroom = float(row.get('Legroom(inches)', 0))
        except:
            legroom = 0.0
            
        # Insert 1 Segment to hold the amenities and legroom
        sql_segment = f"INSERT INTO airline_FlightSegments (FlightId, SegmentNumber, Amenities, LegroomInches, FromAirportCode, ToAirportCode, DepartureTime, ArrivalTime) VALUES (@FlightId, 1, '{amenities_str}', {legroom}, '{dep}', '{arr}', '{dep_dt}', '{arr_dt}');"
        sql_statements.append(sql_segment)
        
        # Insert Layovers if stops > 0
        if stops > 0:
            stop_1 = row.get('stop_1_airline', 'None')
            if stop_1 != 'None' and stop_1.strip():
                sql_layover = f"INSERT INTO airline_FlightLayovers (FlightId, LayoverOrder, AirportName, DurationString) VALUES (@FlightId, 1, 'Transit (Airline: {stop_1})', 'Unknown');"
                sql_statements.append(sql_layover)
            
            if stops > 1:
                stop_2 = row.get('stop_2_airline', 'None')
                if stop_2 != 'None' and stop_2.strip():
                    sql_layover2 = f"INSERT INTO airline_FlightLayovers (FlightId, LayoverOrder, AirportName, DurationString) VALUES (@FlightId, 2, 'Transit (Airline: {stop_2})', 'Unknown');"
                    sql_statements.append(sql_layover2)
                    
            if stops > 2:
                stop_3 = row.get('stop_3_airline', 'None')
                if stop_3 != 'None' and stop_3.strip():
                    sql_layover3 = f"INSERT INTO airline_FlightLayovers (FlightId, LayoverOrder, AirportName, DurationString) VALUES (@FlightId, 3, 'Transit (Airline: {stop_3})', 'Unknown');"
                    sql_statements.append(sql_layover3)

with open("import_flights_full.sql", "w", encoding="utf-8") as f:
    f.write("\n".join(sql_statements))

print(f"Generated complete SQL script.")

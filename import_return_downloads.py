import csv
import sys
import datetime
import ast

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

transit_map = {
    "Rome": "FCO", "Heathrow": "LHR", "Frankfurt": "FRA", "Schiphol": "AMS", "Warsaw": "WAW",
    "Berlin": "BER", "Bucharest": "OTP", "Vienna": "VIE", "Geneva": "GVA", "Stuttgart": "STR",
    "Abu Dhabi": "AUH", "Budapest": "BUD", "Antalya": "AYT", "Ankara": "ESB", "Casablanca": "CMN",
    "Sabiha": "SAW", "Thessaloniki": "SKG", "Malpensa": "MXP", "Amman": "AMM", "Charles de Gaulle": "CDG",
    "Linate": "LIN", "Athens": "ATH", "Dubai": "DXB", "Duesseldorf": "DUS", "Bahrain": "BAH",
    "Madrid": "MAD", "Barcelona": "BCN", "Izmir": "ADB", "Brussels": "BRU", "Riyadh": "RUH",
    "Baku": "GYD", "Munich": "MUC", "Zurich": "ZRH", "Istanbul": "IST", "Muscat": "MCT",
    "Cairo": "CAI", "Kuwait": "KWI", "Tunis": "TUN", "Jeddah": "JED", "Doha": "DOH"
}

def get_transit_code(transit_str):
    if not transit_str or transit_str == 'None': return 'TRN'
    for key, code in transit_map.items():
        if key.lower() in transit_str.lower():
            return code
    return 'TRN'

sql_statements = [
    "SET NOCOUNT ON;",
    "DECLARE @FlightId BIGINT;"
]

def clean_amenities(amenities_str):
    if not amenities_str or amenities_str.strip() == '': return ''
    try:
        lst = ast.literal_eval(amenities_str)
        cleaned = [x for x in lst if 'Emissions estimate' not in x and 'Contrail' not in x]
        return ", ".join(cleaned).replace("'", "''")
    except:
        return amenities_str.replace("'", "''")

with open(r"D:\Downloads\return_flights.csv", encoding="utf-8-sig") as f:
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
        try: dur_min = int(float(row['duration_minutes']))
        except: dur_min = 0
            
        try: price = float(row['price_USD'])
        except: price = 0
            
        try: stops = int(float(row['stops']))
        except: stops = 0
            
        f_class = row['flight_class'].replace("'", "''")
        
        sql_flight = f"INSERT INTO airline_Flights (DepartureAirportCode, ArrivalAirportCode, DepartureTime, ArrivalTime, Price, AirlineId, FlightClass, Duration, DurationMinutes, Status, Currency, NumberOfStops, AvailableSeats) VALUES ('{dep}', '{arr}', '{dep_dt}', '{arr_dt}', {price}, {airline_id}, '{f_class}', '{duration}', {dur_min}, 'Active', 'USD', {stops}, 100);"
        sql_statements.append(sql_flight)
        sql_statements.append("SET @FlightId = SCOPE_IDENTITY();")
        
        # Determine codes
        t1 = get_transit_code(row.get('stop_1_airline')) if stops >= 1 else None
        t2 = get_transit_code(row.get('stop_2_airline')) if stops >= 2 else None
        t3 = get_transit_code(row.get('stop_3_airline')) if stops >= 3 else None
        
        # Segment 1
        am_1 = clean_amenities(row.get('Amenities_segment_1', ''))
        try: leg_1 = float(row.get('Legroom(inches)_segment_1', 0))
        except: leg_1 = 0.0
        
        seg1_from = dep
        seg1_to = t1 if stops >= 1 else arr
        sql_statements.append(f"INSERT INTO airline_FlightSegments (FlightId, SegmentNumber, Amenities, LegroomInches, FromAirportCode, ToAirportCode, DepartureTime) VALUES (@FlightId, 1, '{am_1}', {leg_1}, '{seg1_from}', '{seg1_to}', '{dep_dt}');")
        
        # Segment 2
        if stops >= 1:
            am_2 = clean_amenities(row.get('Amenities_segment_2', ''))
            try: leg_2 = float(row.get('Legroom(inches)_segment_2', 0))
            except: leg_2 = 0.0
            seg2_from = t1
            seg2_to = t2 if stops >= 2 else arr
            if stops == 1:
                sql_statements.append(f"INSERT INTO airline_FlightSegments (FlightId, SegmentNumber, Amenities, LegroomInches, FromAirportCode, ToAirportCode, ArrivalTime) VALUES (@FlightId, 2, '{am_2}', {leg_2}, '{seg2_from}', '{seg2_to}', '{arr_dt}');")
            else:
                sql_statements.append(f"INSERT INTO airline_FlightSegments (FlightId, SegmentNumber, Amenities, LegroomInches, FromAirportCode, ToAirportCode) VALUES (@FlightId, 2, '{am_2}', {leg_2}, '{seg2_from}', '{seg2_to}');")
            
        # Segment 3
        if stops >= 2:
            am_3 = clean_amenities(row.get('Amenities_segment_3', ''))
            try: leg_3 = float(row.get('Legroom(inches)_segment_3', 0))
            except: leg_3 = 0.0
            seg3_from = t2
            seg3_to = t3 if stops >= 3 else arr
            if stops == 2:
                sql_statements.append(f"INSERT INTO airline_FlightSegments (FlightId, SegmentNumber, Amenities, LegroomInches, FromAirportCode, ToAirportCode, ArrivalTime) VALUES (@FlightId, 3, '{am_3}', {leg_3}, '{seg3_from}', '{seg3_to}', '{arr_dt}');")
            else:
                sql_statements.append(f"INSERT INTO airline_FlightSegments (FlightId, SegmentNumber, Amenities, LegroomInches, FromAirportCode, ToAirportCode) VALUES (@FlightId, 3, '{am_3}', {leg_3}, '{seg3_from}', '{seg3_to}');")

        # Layovers
        if stops > 0:
            stop_1 = row.get('stop_1_airline', 'None').replace("'", "''")
            if stop_1 != 'None' and stop_1.strip():
                sql_statements.append(f"INSERT INTO airline_FlightLayovers (FlightId, LayoverOrder, AirportName, DurationString) VALUES (@FlightId, 1, '{stop_1}', 'Unknown');")
            
            if stops > 1:
                stop_2 = row.get('stop_2_airline', 'None').replace("'", "''")
                if stop_2 != 'None' and stop_2.strip():
                    sql_statements.append(f"INSERT INTO airline_FlightLayovers (FlightId, LayoverOrder, AirportName, DurationString) VALUES (@FlightId, 2, '{stop_2}', 'Unknown');")
                    
            if stops > 2:
                stop_3 = row.get('stop_3_airline', 'None').replace("'", "''")
                if stop_3 != 'None' and stop_3.strip():
                    sql_statements.append(f"INSERT INTO airline_FlightLayovers (FlightId, LayoverOrder, AirportName, DurationString) VALUES (@FlightId, 3, '{stop_3}', 'Unknown');")
                    
        sql_statements.append("GO")
        sql_statements.append("DECLARE @FlightId BIGINT;")

with open("import_return_downloads.sql", "w", encoding="utf-8") as f:
    f.write("\n".join(sql_statements))

print("Generated complete return flights SQL script from Downloads.")

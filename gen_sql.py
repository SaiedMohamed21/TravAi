import csv

city_to_code = {'Aswan': 'ASW', 'Frankfurt': 'FRA', 'Cairo': 'CAI', 'Hurghada': 'HRG', 'Luxor': 'LXR'}

rows = []
with open(r'C:\Users\saied mohamed\Desktop\return_flights.csv', encoding='utf-8') as f:
    reader = csv.DictReader(f)
    for r in reader:
        dep = r['departure_city'].strip()
        arr = r['arrival_city'].strip()
        dep_code = city_to_code.get(dep)
        arr_code = city_to_code.get(arr)
        if not dep_code or not arr_code:
            continue
        stops = int(float(r['stops'].strip() or '0'))
        duration_min = r['duration_minutes'].strip() or '0'
        price = r['price_USD'].strip()
        flight_class = r['flight_class'].strip().replace("'", "''")
        dep_t = r['departure_datetime'].strip()
        arr_t = r['arrival_datetime'].strip()
        duration = r['duration'].strip()[:50].replace("'", "''")
        rows.append({
            'dep_code': dep_code, 'arr_code': arr_code,
            'dep_time': dep_t, 'arr_time': arr_t,
            'price': price, 'stops': stops,
            'duration': duration, 'duration_min': duration_min,
            'flight_class': flight_class
        })

lines = []
lines.append('-- ====================================================')
lines.append('-- Return Flights: Egypt Cities -> Frankfurt')
lines.append('-- Total: ' + str(len(rows)) + ' flights')
lines.append('-- ====================================================')
lines.append('')
lines.append("IF NOT EXISTS (SELECT 1 FROM airline_Airports WHERE Code='HRG')")
lines.append("    INSERT INTO airline_Airports (Code,Name,City,Country) VALUES ('HRG','Hurghada International Airport','Hurghada','Egypt');")
lines.append("IF NOT EXISTS (SELECT 1 FROM airline_Airports WHERE Code='LXR')")
lines.append("    INSERT INTO airline_Airports (Code,Name,City,Country) VALUES ('LXR','Luxor International Airport','Luxor','Egypt');")
lines.append("IF NOT EXISTS (SELECT 1 FROM airline_Airports WHERE Code='CAI')")
lines.append("    INSERT INTO airline_Airports (Code,Name,City,Country) VALUES ('CAI','Cairo International Airport','Cairo','Egypt');")
lines.append("IF NOT EXISTS (SELECT 1 FROM airline_Airports WHERE Code='ASW')")
lines.append("    INSERT INTO airline_Airports (Code,Name,City,Country) VALUES ('ASW','Aswan International Airport','Aswan','Egypt');")
lines.append("IF NOT EXISTS (SELECT 1 FROM airline_Airports WHERE Code='FRA')")
lines.append("    INSERT INTO airline_Airports (Code,Name,City,Country) VALUES ('FRA','Frankfurt Airport','Frankfurt','Germany');")
lines.append('')
lines.append("DECLARE @aid BIGINT = (SELECT TOP 1 Id FROM airline_Airlines WHERE Name LIKE '%Egypt%');")
lines.append("IF @aid IS NULL SET @aid = (SELECT TOP 1 Id FROM airline_Airlines);")
lines.append('')

# Split into batches of 500 to avoid hitting SQL limits
batch_size = 500
for i in range(0, len(rows), batch_size):
    batch = rows[i:i+batch_size]
    lines.append(f'-- Batch {i//batch_size + 1}')
    lines.append('INSERT INTO airline_Flights')
    lines.append('    (DepartureAirportCode,ArrivalAirportCode,DepartureTime,ArrivalTime,Price,AvailableSeats,')
    lines.append('     AirlineId,NumberOfStops,FlightClass,Currency,Duration,DurationMinutes,Status)')
    lines.append('VALUES')
    vals = []
    for r in batch:
        val = f"('{r['dep_code']}','{r['arr_code']}','{r['dep_time']}','{r['arr_time']}',{r['price']},120,@aid,{r['stops']},'{r['flight_class']}','USD','{r['duration']}',{r['duration_min']},'Active')"
        vals.append(val)
    lines.append(',\n'.join(vals) + ';')
    lines.append('')

lines.append('-- Verify result')
lines.append('SELECT DepartureAirportCode, ArrivalAirportCode, FlightClass, COUNT(*) as Count,')
lines.append('       MIN(Price) as MinPrice, MAX(Price) as MaxPrice')
lines.append('FROM airline_Flights')
lines.append("WHERE ArrivalAirportCode = 'FRA' AND Status = 'Active'")
lines.append('GROUP BY DepartureAirportCode, ArrivalAirportCode, FlightClass')
lines.append('ORDER BY DepartureAirportCode, FlightClass;')

output_path = r'C:\Users\saied mohamed\Desktop\hotel\TravAi\return_flights_insert.sql'
with open(output_path, 'w', encoding='utf-8') as out:
    out.write('\n'.join(lines))

print('Done! File saved to:', output_path)
print('Total INSERT rows:', len(rows))

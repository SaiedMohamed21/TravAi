import csv
import os

sql_statements = [
    "SET NOCOUNT ON;",
    "BEGIN TRAN;"
]

csv_path = r"C:\Users\saied mohamed\Desktop\tours_backend_complete.csv"
guide_ids = list(range(150, 201))

guide_mapping = {
    'mahmoud ali': 150,
    'ali saad': 151,
    'fatma mostafa': 152,
    'khaled ali': 153,
    'aisha ibrahim': 154,
    'ahmed ibrahim': 155,
    'omar ibrahim': 156,
    'omar ali': 157,
    'hassan hassan': 158,
    'hassan mostafa': 159,
    'ahmed saad': 160,
    'aisha mostafa': 161,
    'aisha ali': 162,
    'fatma hassan': 163,
    'hassan ibrahim': 164,
    'mohamed mostafa': 165,
    'mohamed hassan': 166,
    'ali mostafa': 167,
    'mahmoud hassan': 168,
    'fatma ali': 169,
    'omar saad': 170,
    'ali ibrahim': 171,
    'mohamed ali': 172,
    'mahmoud mostafa': 173,
    'ahmed ali': 174,
    'mahmoud ibrahim': 175,
    'ali hassan': 176,
    'hassan saad': 177,
    'sara hassan': 178,
    'sara mostafa': 179,
    'ahmed hassan': 180,
    'aisha hassan': 181,
    'khaled hassan': 182,
    'sara ibrahim': 183,
    'omar hassan': 184,
    'mohamed ibrahim': 185,
    'fatma saad': 186,
    'mohamed saad': 187,
    'khaled mostafa': 188,
    'mahmoud saad': 189,
    'aisha saad': 190,
    'omar mostafa': 191,
    'khaled saad': 192,
    'khaled ibrahim': 193,
    'ali ali': 194,
    'ahmed mostafa': 195,
    'hassan ali': 196,
    'fatma ibrahim': 197,
    'sara ali': 198,
    'sara saad': 199,
    'omar hassan mo': 200,
    'test guide emergency': 201
}

def escape_sql(val):
    if val is None:
        return ""
    return str(val).replace("'", "''")

with open(csv_path, encoding='utf-8') as f:
    reader = csv.DictReader(f)
    for row in reader:
        gname = row['guide_name'].lower().strip()
        if gname in guide_mapping:
            tour_guide_id = guide_mapping[gname]
        else:
            h = sum(ord(c) for c in row['guide_name'])
            tour_guide_id = guide_ids[h % len(guide_ids)]
        
        city = escape_sql(row['city'])
        title = escape_sql(row['tour_title'])
        ttype = escape_sql(row['tour_type'])
        desc = escape_sql(row['tour_description'])
        
        try: price = float(row['base_price_usd'])
        except: price = 0.0
            
        try: duration_int = int(float(row['duration_hours']))
        except: duration_int = 0
            
        try: group_size = int(row['group_size_max'])
        except: group_size = 0
            
        try: rating = float(row['rating'])
        except: rating = 0.0
            
        try: reviews = int(row['number_of_reviews'])
        except: reviews = 0
            
        age_rest = escape_sql(row['age_restrictions'])
        
        transport = 1 if row['transport_included'] == '1' else 0
        meals = 1 if row['meals_included'] == '1' else 0
        is_accessible = 1 if row['accessible'] == '1' else 0
        accessibility = escape_sql(row['accessibility'])
        
        customizable = 1 if (row.get('customizable_tour') == '1' or str(row.get('customizable')).lower() == 'yes') else 0
        
        inc_services = escape_sql(row['included_services'])
        exc_services = escape_sql(row['excluded_services'])
        pickup = escape_sql(row['pickup_details'])
        avail_dt = row['available_datetime']
        
        # Parse cancellation policy
        cp_str = str(row['cancellation_policy'])
        if '24' in cp_str:
            cancellation_policy = 24
        elif '48' in cp_str:
            cancellation_policy = 48
        elif '72' in cp_str:
            cancellation_policy = 72
        else:
            cancellation_policy = 24
            
        try: tour_score = float(row['tour_score'])
        except: tour_score = 0.0
        
        sql = f"""INSERT INTO tourguide_Tours (
            TourGuideId, City, TourTitle, TourType, TourDescription,
            BasePriceUsd, Currency, DurationHours, GroupSizeMax, Rating, NumberOfReviews,
            AgeRestriction, TransportIncluded, MealsIncluded, IsAccessible, Accessibility,
            Customizable, IncludedServices, ExcludedServices, PickupDetails, AvailableDateTime,
            CancellationPolicy, Active, CreatedAt, TourScore
        ) VALUES (
            {tour_guide_id}, '{city}', '{title}', '{ttype}', '{desc}',
            {price}, 'USD', {duration_int}, {group_size}, {rating}, {reviews},
            '{age_rest}', {transport}, {meals}, {is_accessible}, '{accessibility}',
            {customizable}, '{inc_services}', '{exc_services}', '{pickup}', '{avail_dt}',
            {cancellation_policy}, 1, GETUTCDATE(), {tour_score}
        );"""
        sql_statements.append(sql)

sql_statements.append("COMMIT TRAN;")
sql_statements.append("GO")

with open(r"c:\Users\saied mohamed\Desktop\hotel\TravAi\import_tours.sql", "w", encoding="utf-8") as out:
    out.write("\n".join(sql_statements))

print(f"Generated SQL script with {len(sql_statements) - 3} inserts.")

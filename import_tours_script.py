import csv
import os

sql_statements = [
    "SET NOCOUNT ON;",
    "BEGIN TRAN;",
    "DELETE FROM tourguide_Tours;"
]

csv_path = r"C:\Users\saied mohamed\Desktop\tours_dataset_backend_ready.csv"
guide_ids = list(range(150, 201))

def escape_sql(val):
    if val is None:
        return ""
    return str(val).replace("'", "''")

with open(csv_path, encoding='utf-8') as f:
    reader = csv.DictReader(f)
    for row in reader:
        guide_name = row['guide_name']
        # deterministically pick a guide id based on name hash
        h = sum(ord(c) for c in guide_name)
        tour_guide_id = guide_ids[h % len(guide_ids)]
        
        city = escape_sql(row['city'])
        title = escape_sql(row['tour_title'])
        duration = row['duration_hours']
        try: duration_int = int(float(duration))
        except: duration_int = 0
            
        group_size = int(row['group_size_max']) if row['group_size_max'] else 0
        price = float(row['base_price_usd']) if row['base_price_usd'] else 0.0
        
        transport = 1 if row['transport_included'] == '1' else 0
        meals = 1 if row['meals_included'] == '1' else 0
        
        rating = float(row['rating']) if row['rating'] else 0.0
        reviews = int(row['number_of_reviews']) if row['number_of_reviews'] else 0
        
        cluster = escape_sql(row['cluster_label'])
        avail_dt = row['available_datetime']
        accessibility = escape_sql(row['accessibility'])
        is_accessible = 1 if 'Wheelchair accessible' in accessibility else 0
        
        tour_score = float(row['tour_score']) if row['tour_score'] else 0.0
        desc = escape_sql(row['tour_description'])
        
        sql = f"""INSERT INTO tourguide_Tours (
            TourGuideId, City, TourTitle, TourType, TourDescription,
            BasePriceUsd, Currency, DurationHours, GroupSizeMax, Rating, NumberOfReviews,
            TourScore, TransportIncluded, MealsIncluded, IsAccessible, Accessibility,
            AvailableDateTime, CancellationPolicy, Active, Customizable, CreatedAt
        ) VALUES (
            {tour_guide_id}, '{city}', '{title}', '{cluster}', '{desc}',
            {price}, 'USD', {duration_int}, {group_size}, {rating}, {reviews},
            {tour_score}, {transport}, {meals}, {is_accessible}, '{accessibility}',
            '{avail_dt}', 0, 1, 0, GETUTCDATE()
        );"""
        sql_statements.append(sql)

sql_statements.append("COMMIT TRAN;")
sql_statements.append("GO")

with open(r"c:\Users\saied mohamed\Desktop\hotel\TravAi\import_tours.sql", "w", encoding="utf-8") as out:
    out.write("\n".join(sql_statements))

print(f"Generated SQL script with {len(sql_statements) - 3} inserts.")

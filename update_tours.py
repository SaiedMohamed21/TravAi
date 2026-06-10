import csv
import os

csv_path = r"D:\Downloads\Telegram Desktop\tours_dataset_with_descriptions.csv"

# We want to collect unique combinations to avoid redundant UPDATE statements.
# dictionary mapping (tour_title, city) -> dict of the 4 columns
updates = {}

def escape_sql(val):
    if val is None:
        return ""
    return str(val).replace("'", "''")

with open(csv_path, encoding='utf-8') as f:
    reader = csv.DictReader(f)
    for row in reader:
        key = (row['tour_title'], row['city'])
        if key not in updates:
            updates[key] = {
                'AgeRestriction': escape_sql(row.get('age_restrictions', '')),
                'IncludedServices': escape_sql(row.get('included_services', '')),
                'ExcludedServices': escape_sql(row.get('excluded_services', '')),
                'PickupDetails': escape_sql(row.get('pickup_details', ''))
            }

sql_statements = [
    "SET NOCOUNT ON;",
    "BEGIN TRAN;"
]

for (title, city), data in updates.items():
    title_esc = escape_sql(title)
    city_esc = escape_sql(city)
    sql = f"""UPDATE tourguide_Tours
SET AgeRestriction = '{data['AgeRestriction']}',
    IncludedServices = '{data['IncludedServices']}',
    ExcludedServices = '{data['ExcludedServices']}',
    PickupDetails = '{data['PickupDetails']}'
WHERE TourTitle = '{title_esc}' AND City = '{city_esc}';"""
    sql_statements.append(sql)

sql_statements.append("COMMIT TRAN;")
sql_statements.append("GO")

out_path = r"c:\Users\saied mohamed\Desktop\hotel\TravAi\update_tours.sql"
with open(out_path, "w", encoding="utf-8") as out:
    out.write("\n".join(sql_statements))

print(f"Generated SQL script with {len(updates)} update statements.")

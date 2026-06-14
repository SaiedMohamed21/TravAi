import pymssql
conn = pymssql.connect(
    server="ahmed-app-sql-server.database.windows.net",
    user="tour123",
    password="Ahmed123@",
    database="myappdb"
)
cursor = conn.cursor()

cursor.execute("SELECT DISTINCT City FROM tourguide_Tours")
cities = cursor.fetchall()
print("Distinct cities in database:")
for c in cities:
    print(c)

cursor.execute("SELECT COUNT(*) FROM tourguide_Tours")
print("Total tours:", cursor.fetchone()[0])

cursor.execute("SELECT Status, COUNT(*) FROM tourguide_UrgentRequests GROUP BY Status")
print("Urgent Requests status counts:")
for row in cursor.fetchall():
    print(row)

cursor.execute("SELECT Status, COUNT(*) FROM tourguide_TourGuides GROUP BY Status")
print("Tour Guides status counts:")
for row in cursor.fetchall():
    print(row)

conn.close()

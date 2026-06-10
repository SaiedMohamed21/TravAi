import os

images = {
    'Cairo': 'https://upload.wikimedia.org/wikipedia/commons/e/e3/Kheops-Pyramid.jpg',
    'Luxor': 'https://upload.wikimedia.org/wikipedia/commons/a/ab/Luxor_Temple_-_01.jpg',
    'Aswan': 'https://upload.wikimedia.org/wikipedia/commons/f/fe/Aswan_-_Philae_Temple.jpg',
    'Sharm El Sheikh': 'https://upload.wikimedia.org/wikipedia/commons/4/41/Sharm_El-Sheikh.jpg',
    'Hurghada': 'https://upload.wikimedia.org/wikipedia/commons/b/b8/Hurghada_coast.jpg'
}

sql = [
    "SET NOCOUNT ON;",
    "DELETE FROM tourguide_TourImages;",
    "BEGIN TRAN;"
]

for city, url in images.items():
    sql.append(f"""
INSERT INTO tourguide_TourImages (TourId, ImageUrl, Caption, IsPrimary, SortOrder)
SELECT Id, '{url}', '{city} Tour', 1, 1
FROM tourguide_Tours
WHERE City = '{city}';
""")

sql.append("COMMIT TRAN;")
sql.append("GO")

out_path = r"c:\Users\saied mohamed\Desktop\hotel\TravAi\add_images.sql"
with open(out_path, "w", encoding="utf-8") as out:
    out.write("\n".join(sql))

print("Generated add_images.sql successfully.")

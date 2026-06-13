SET NOCOUNT ON;

UPDATE TI
SET TI.ImageUrl = 
CASE
    WHEN T.TourTitle LIKE '%Pyramids%' THEN 'https://upload.wikimedia.org/wikipedia/commons/e/e3/Kheops-Pyramid.jpg'
    WHEN T.TourTitle LIKE '%Sphinx%' THEN 'https://upload.wikimedia.org/wikipedia/commons/3/3a/Great_Sphinx_of_Giza.jpg'
    WHEN T.TourTitle LIKE '%Egyptian Museum%' THEN 'https://upload.wikimedia.org/wikipedia/commons/f/f5/Egyptian_Museum.jpg'
    WHEN T.TourTitle LIKE '%Khan El Khalili%' THEN 'https://upload.wikimedia.org/wikipedia/commons/8/83/Khan_el-Khalili.jpg'

    WHEN T.TourTitle LIKE '%Karnak%' THEN 'https://upload.wikimedia.org/wikipedia/commons/d/d6/Karnak_Temple.jpg'
    WHEN T.TourTitle LIKE '%Luxor Temple%' THEN 'https://upload.wikimedia.org/wikipedia/commons/c/c6/Luxor_Temple.jpg'
    WHEN T.TourTitle LIKE '%Valley of the Kings%' THEN 'https://upload.wikimedia.org/wikipedia/commons/7/75/Valley_of_the_Kings.jpg'
    WHEN T.TourTitle LIKE '%Hot Air Balloon%' THEN 'https://images.unsplash.com/photo-1500530855697-b586d89ba3ee'

    WHEN T.TourTitle LIKE '%Abu Simbel%' THEN 'https://upload.wikimedia.org/wikipedia/commons/3/38/Abu_Simbel.jpg'
    WHEN T.TourTitle LIKE '%Philae%' THEN 'https://upload.wikimedia.org/wikipedia/commons/0/08/Philae_Temple.jpg'
    WHEN T.TourTitle LIKE '%Nubian%' THEN 'https://images.unsplash.com/photo-1521295121783-8a321d551ad2'

    WHEN T.TourTitle LIKE '%Hurghada%' THEN 'https://images.unsplash.com/photo-1507525428034-b723cf961d3e'
    WHEN T.TourTitle LIKE '%Giftun%' THEN 'https://images.unsplash.com/photo-1500375592092-40eb2168fd21'
    WHEN T.TourTitle LIKE '%Diving%' THEN 'https://images.unsplash.com/photo-1544551763-46a013bb70d5'
    WHEN T.TourTitle LIKE '%Snorkeling%' THEN 'https://images.unsplash.com/photo-1682687220742-aba13b6e50ba'

    WHEN T.TourTitle LIKE '%Sharm%' THEN 'https://images.unsplash.com/photo-1506744038136-46273834b3fb'
    WHEN T.TourTitle LIKE '%Ras Mohammed%' THEN 'https://images.unsplash.com/photo-1544551763-77ef2d0cfc6c'
    WHEN T.TourTitle LIKE '%Blue Hole%' THEN 'https://images.unsplash.com/photo-1551244072-5d12893278ab'

    ELSE 'https://images.unsplash.com/photo-1469474968028-56623f02e42e'
END
FROM tourguide_TourImages TI
INNER JOIN tourguide_Tours T ON TI.TourId = T.Id;
GO

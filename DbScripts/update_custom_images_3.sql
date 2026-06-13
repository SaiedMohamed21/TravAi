SET NOCOUNT ON;

UPDATE TI
SET TI.ImageUrl = 
CASE 
    WHEN T.TourTitle LIKE '%Luxor%' THEN 'https://images.pexels.com/photos/258154/pexels-photo-258154.jpeg'
    WHEN T.TourTitle LIKE '%Karnak%' THEN 'https://images.pexels.com/photos/161154/stained-glass-spiral-circle-pattern-161154.jpeg'
    WHEN T.TourTitle LIKE '%Valley of the Kings%' THEN 'https://images.pexels.com/photos/417074/pexels-photo-417074.jpeg'

    WHEN T.TourTitle LIKE '%Aswan%' THEN 'https://images.pexels.com/photos/1001682/pexels-photo-1001682.jpeg'
    WHEN T.TourTitle LIKE '%Philae%' THEN 'https://images.pexels.com/photos/2087391/pexels-photo-2087391.jpeg'
    WHEN T.TourTitle LIKE '%Nubian%' THEN 'https://images.pexels.com/photos/325185/pexels-photo-325185.jpeg'
    WHEN T.TourTitle LIKE '%Abu Simbel%' THEN 'https://images.pexels.com/photos/1001965/pexels-photo-1001965.jpeg'

    ELSE TI.ImageUrl
END
FROM tourguide_TourImages TI
INNER JOIN tourguide_Tours T ON TI.TourId = T.Id;
GO

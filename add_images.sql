SET NOCOUNT ON;
DELETE FROM tourguide_TourImages;
BEGIN TRAN;

INSERT INTO tourguide_TourImages (TourId, ImageUrl, Caption, IsPrimary, SortOrder)
SELECT Id, 'https://upload.wikimedia.org/wikipedia/commons/e/e3/Kheops-Pyramid.jpg', 'Cairo Tour', 1, 1
FROM tourguide_Tours
WHERE City = 'Cairo';


INSERT INTO tourguide_TourImages (TourId, ImageUrl, Caption, IsPrimary, SortOrder)
SELECT Id, 'https://upload.wikimedia.org/wikipedia/commons/a/ab/Luxor_Temple_-_01.jpg', 'Luxor Tour', 1, 1
FROM tourguide_Tours
WHERE City = 'Luxor';


INSERT INTO tourguide_TourImages (TourId, ImageUrl, Caption, IsPrimary, SortOrder)
SELECT Id, 'https://upload.wikimedia.org/wikipedia/commons/f/fe/Aswan_-_Philae_Temple.jpg', 'Aswan Tour', 1, 1
FROM tourguide_Tours
WHERE City = 'Aswan';


INSERT INTO tourguide_TourImages (TourId, ImageUrl, Caption, IsPrimary, SortOrder)
SELECT Id, 'https://upload.wikimedia.org/wikipedia/commons/4/41/Sharm_El-Sheikh.jpg', 'Sharm El Sheikh Tour', 1, 1
FROM tourguide_Tours
WHERE City = 'Sharm El Sheikh';


INSERT INTO tourguide_TourImages (TourId, ImageUrl, Caption, IsPrimary, SortOrder)
SELECT Id, 'https://upload.wikimedia.org/wikipedia/commons/b/b8/Hurghada_coast.jpg', 'Hurghada Tour', 1, 1
FROM tourguide_Tours
WHERE City = 'Hurghada';

COMMIT TRAN;
GO
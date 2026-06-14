INSERT INTO tourguide_UrgentRequests (
    TourGuideId, TourId, Reason, DocumentationUrl, Status, CreatedAt
) VALUES (
    153, 18676, 'Family emergency: Need to travel abroad immediately due to an unexpected family health issue.', 'https://travai-docs.blob.core.windows.net/medical/cert.pdf', 0, GETDATE()
), (
    154, 18677, 'Medical emergency: Guide has contracted a high fever and is unfit to lead the outdoor walking tour.', NULL, 0, GETDATE()
);

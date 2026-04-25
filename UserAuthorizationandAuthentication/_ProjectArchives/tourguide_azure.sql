BEGIN TRANSACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260417015646_AddTourGuideModule', N'8.0.0');
GO

COMMIT;
GO


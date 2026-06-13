using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TravAi.Data;

#nullable disable

namespace TravAi.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260613000000_FixComplaintAttachmentsNullability")]
    public partial class FixComplaintAttachmentsNullability : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Safe alteration of ComplaintId column to be nullable
            migrationBuilder.Sql(@"
                DECLARE @var0 sysname;
                SELECT @var0 = [d].[name]
                FROM [sys].[default_constraints] [d]
                INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
                WHERE ([d].[parent_object_id] = OBJECT_ID(N'[hotel_ComplaintAttachments]') AND [c].[name] = N'ComplaintId');
                IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [hotel_ComplaintAttachments] DROP CONSTRAINT [' + @var0 + '];');
                
                ALTER TABLE [hotel_ComplaintAttachments] ALTER COLUMN [ComplaintId] bigint NULL;
            ");

            // 2. Add ReplyId column if it doesn't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT * FROM sys.columns 
                    WHERE object_id = OBJECT_ID(N'[hotel_ComplaintAttachments]') 
                      AND name = N'ReplyId'
                )
                BEGIN
                    ALTER TABLE [hotel_ComplaintAttachments] ADD [ReplyId] bigint NULL;
                END
            ");

            // 3. Create index on ReplyId if it doesn't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT * FROM sys.indexes 
                    WHERE name = N'IX_hotel_ComplaintAttachments_ReplyId' 
                      AND object_id = OBJECT_ID(N'[hotel_ComplaintAttachments]')
                )
                BEGIN
                    CREATE INDEX [IX_hotel_ComplaintAttachments_ReplyId] ON [hotel_ComplaintAttachments] ([ReplyId]);
                END
            ");

            // 4. Add Foreign Key for ReplyId if it doesn't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT * FROM sys.foreign_keys 
                    WHERE name = N'FK_hotel_ComplaintAttachments_hotel_ComplaintReplies_ReplyId' 
                      AND parent_object_id = OBJECT_ID(N'[hotel_ComplaintAttachments]')
                )
                BEGIN
                    ALTER TABLE [hotel_ComplaintAttachments] 
                    ADD CONSTRAINT [FK_hotel_ComplaintAttachments_hotel_ComplaintReplies_ReplyId] 
                    FOREIGN KEY ([ReplyId]) REFERENCES [hotel_ComplaintReplies] ([Id]) ON DELETE CASCADE;
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Down migration can be basic, but we also write it safely
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT * FROM sys.foreign_keys 
                    WHERE name = N'FK_hotel_ComplaintAttachments_hotel_ComplaintReplies_ReplyId' 
                      AND parent_object_id = OBJECT_ID(N'[hotel_ComplaintAttachments]')
                )
                BEGIN
                    ALTER TABLE [hotel_ComplaintAttachments] DROP CONSTRAINT [FK_hotel_ComplaintAttachments_hotel_ComplaintReplies_ReplyId];
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT * FROM sys.indexes 
                    WHERE name = N'IX_hotel_ComplaintAttachments_ReplyId' 
                      AND object_id = OBJECT_ID(N'[hotel_ComplaintAttachments]')
                )
                BEGIN
                    DROP INDEX [IX_hotel_ComplaintAttachments_ReplyId] ON [hotel_ComplaintAttachments];
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT * FROM sys.columns 
                    WHERE object_id = OBJECT_ID(N'[hotel_ComplaintAttachments]') 
                      AND name = N'ReplyId'
                )
                BEGIN
                    ALTER TABLE [hotel_ComplaintAttachments] DROP COLUMN [ReplyId];
                END
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE [hotel_ComplaintAttachments] ALTER COLUMN [ComplaintId] bigint NOT NULL;
            ");
        }
    }
}

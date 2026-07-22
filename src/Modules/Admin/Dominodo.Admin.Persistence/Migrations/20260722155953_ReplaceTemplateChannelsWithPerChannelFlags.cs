using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dominodo.Admin.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceTemplateChannelsWithPerChannelFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EmailEnabled",
                schema: "admin",
                table: "NotificationTemplates",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PushEnabled",
                schema: "admin",
                table: "NotificationTemplates",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "InAppEnabled",
                schema: "admin",
                table: "NotificationTemplates",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Backfill the new per-channel flags from the old [Flags] Channels bitmask (Email=1, Push=2, InApp=4).
            migrationBuilder.Sql(@"
UPDATE [admin].[NotificationTemplates]
SET [EmailEnabled] = CASE WHEN ([Channels] & 1) = 1 THEN 1 ELSE 0 END,
    [PushEnabled]  = CASE WHEN ([Channels] & 2) = 2 THEN 1 ELSE 0 END,
    [InAppEnabled] = CASE WHEN ([Channels] & 4) = 4 THEN 1 ELSE 0 END;");

            migrationBuilder.DropColumn(
                name: "Channels",
                schema: "admin",
                table: "NotificationTemplates");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Channels",
                schema: "admin",
                table: "NotificationTemplates",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Rebuild the bitmask from the per-channel flags (Email=1, Push=2, InApp=4).
            migrationBuilder.Sql(@"
UPDATE [admin].[NotificationTemplates]
SET [Channels] = (CASE WHEN [EmailEnabled] = 1 THEN 1 ELSE 0 END)
               + (CASE WHEN [PushEnabled]  = 1 THEN 2 ELSE 0 END)
               + (CASE WHEN [InAppEnabled] = 1 THEN 4 ELSE 0 END);");

            migrationBuilder.DropColumn(
                name: "EmailEnabled",
                schema: "admin",
                table: "NotificationTemplates");

            migrationBuilder.DropColumn(
                name: "PushEnabled",
                schema: "admin",
                table: "NotificationTemplates");

            migrationBuilder.DropColumn(
                name: "InAppEnabled",
                schema: "admin",
                table: "NotificationTemplates");
        }
    }
}

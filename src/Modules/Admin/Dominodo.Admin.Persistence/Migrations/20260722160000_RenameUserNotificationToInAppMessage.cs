using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dominodo.Admin.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameUserNotificationToInAppMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Data-preserving rename of the materialized in-app table (UserNotification -> InAppMessage).
            migrationBuilder.RenameTable(
                name: "UserNotifications",
                schema: "admin",
                newName: "InAppMessages",
                newSchema: "admin");

            migrationBuilder.RenameIndex(
                name: "IX_UserNotifications_RecipientUserId",
                schema: "admin",
                table: "InAppMessages",
                newName: "IX_InAppMessages_RecipientUserId");

            migrationBuilder.RenameIndex(
                name: "IX_UserNotifications_TenantId",
                schema: "admin",
                table: "InAppMessages",
                newName: "IX_InAppMessages_TenantId");

            migrationBuilder.Sql("EXEC sp_rename N'[admin].[PK_UserNotifications]', N'PK_InAppMessages';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("EXEC sp_rename N'[admin].[PK_InAppMessages]', N'PK_UserNotifications';");

            migrationBuilder.RenameTable(
                name: "InAppMessages",
                schema: "admin",
                newName: "UserNotifications",
                newSchema: "admin");

            migrationBuilder.RenameIndex(
                name: "IX_InAppMessages_RecipientUserId",
                schema: "admin",
                table: "UserNotifications",
                newName: "IX_UserNotifications_RecipientUserId");

            migrationBuilder.RenameIndex(
                name: "IX_InAppMessages_TenantId",
                schema: "admin",
                table: "UserNotifications",
                newName: "IX_UserNotifications_TenantId");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dominodo.Admin.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NotificationTemplates",
                schema: "admin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Channels = table.Column<int>(type: "int", nullable: false),
                    EmailSubject = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    EmailBodyHtml = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InAppText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PushText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Localization = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationTemplates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationTemplates_Type_TenantId",
                schema: "admin",
                table: "NotificationTemplates",
                columns: new[] { "Type", "TenantId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationTemplates",
                schema: "admin");
        }
    }
}

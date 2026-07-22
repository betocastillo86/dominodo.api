using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dominodo.Admin.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterializedMessagesAndDevices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeviceRegistrations",
                schema: "admin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Platform = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Token = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceRegistrations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailMessages",
                schema: "admin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    To = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ToName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Subject = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    BodyHtml = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Priority = table.Column<byte>(type: "tinyint", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    ScheduledAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SentAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PushMessages",
                schema: "admin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipientUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    Platform = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    DedupHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SentAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PushMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserNotifications",
                schema: "admin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipientUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ReadAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TriggeredByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNotifications", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceRegistrations_UserId_Token",
                schema: "admin",
                table: "DeviceRegistrations",
                columns: new[] { "UserId", "Token" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_Status",
                schema: "admin",
                table: "EmailMessages",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_TenantId",
                schema: "admin",
                table: "EmailMessages",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PushMessages_RecipientUserId",
                schema: "admin",
                table: "PushMessages",
                column: "RecipientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PushMessages_Status",
                schema: "admin",
                table: "PushMessages",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PushMessages_TenantId",
                schema: "admin",
                table: "PushMessages",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_RecipientUserId",
                schema: "admin",
                table: "UserNotifications",
                column: "RecipientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_TenantId",
                schema: "admin",
                table: "UserNotifications",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeviceRegistrations",
                schema: "admin");

            migrationBuilder.DropTable(
                name: "EmailMessages",
                schema: "admin");

            migrationBuilder.DropTable(
                name: "PushMessages",
                schema: "admin");

            migrationBuilder.DropTable(
                name: "UserNotifications",
                schema: "admin");
        }
    }
}

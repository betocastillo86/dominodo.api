using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dominodo.Operations.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAnnouncementAggregate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Announcements",
                schema: "operations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Priority = table.Column<byte>(type: "tinyint", nullable: false),
                    PublishedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AudienceType = table.Column<int>(type: "int", nullable: false),
                    AudienceFilter = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PublishedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Announcements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AnnouncementAttachments",
                schema: "operations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnnouncementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnnouncementAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnnouncementAttachments_Announcements_AnnouncementId",
                        column: x => x.AnnouncementId,
                        principalSchema: "operations",
                        principalTable: "Announcements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnnouncementAttachments_AnnouncementId",
                schema: "operations",
                table: "AnnouncementAttachments",
                column: "AnnouncementId");

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_Category",
                schema: "operations",
                table: "Announcements",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_TenantId",
                schema: "operations",
                table: "Announcements",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_TenantId_Status",
                schema: "operations",
                table: "Announcements",
                columns: new[] { "TenantId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnnouncementAttachments",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "Announcements",
                schema: "operations");
        }
    }
}

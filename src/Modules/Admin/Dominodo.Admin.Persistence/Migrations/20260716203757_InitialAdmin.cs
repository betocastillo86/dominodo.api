using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dominodo.Admin.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "admin");

            migrationBuilder.CreateTable(
                name: "NotificationDeliveries",
                schema: "admin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Recipient = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationDeliveries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDeliveries_SourceEventId",
                schema: "admin",
                table: "NotificationDeliveries",
                column: "SourceEventId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationDeliveries",
                schema: "admin");
        }
    }
}

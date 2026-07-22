using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dominodo.Operations.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryAggregate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Deliveries",
                schema: "operations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ApartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RegisteredByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PhotoUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Carrier = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReceivedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DeliveredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReceivedByName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DeliveredToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Deliveries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Deliveries_TenantId",
                schema: "operations",
                table: "Deliveries",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Deliveries_TenantId_ApartmentId",
                schema: "operations",
                table: "Deliveries",
                columns: new[] { "TenantId", "ApartmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_Deliveries_TenantId_Code",
                schema: "operations",
                table: "Deliveries",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Deliveries_TenantId_Status",
                schema: "operations",
                table: "Deliveries",
                columns: new[] { "TenantId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Deliveries",
                schema: "operations");
        }
    }
}

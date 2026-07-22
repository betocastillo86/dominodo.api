using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dominodo.Operations.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVisitAggregate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Visits",
                schema: "operations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    VisitorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    VisitorDocument = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PhotoUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    VehiclePlate = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    AmountPaid = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    RegisteredByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorizedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EntryAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExitAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Visits", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Visits_TenantId",
                schema: "operations",
                table: "Visits",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Visits_TenantId_ApartmentId",
                schema: "operations",
                table: "Visits",
                columns: new[] { "TenantId", "ApartmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_Visits_TenantId_Status",
                schema: "operations",
                table: "Visits",
                columns: new[] { "TenantId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Visits",
                schema: "operations");
        }
    }
}

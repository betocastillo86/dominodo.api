using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dominodo.Operations.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestAggregate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "operations");

            migrationBuilder.CreateTable(
                name: "OperationSequence",
                schema: "operations",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Prefix = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationSequence", x => new { x.TenantId, x.Prefix, x.Year });
                });

            migrationBuilder.CreateTable(
                name: "Requests",
                schema: "operations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignedToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResolvedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ClosedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Requests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RequestAttachments",
                schema: "operations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestUpdateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FileUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequestAttachments_Requests_RequestId",
                        column: x => x.RequestId,
                        principalSchema: "operations",
                        principalTable: "Requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RequestParticipants",
                schema: "operations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParticipantType = table.Column<int>(type: "int", nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    JoinedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequestParticipants_Requests_RequestId",
                        column: x => x.RequestId,
                        principalSchema: "operations",
                        principalTable: "Requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RequestStatusHistory",
                schema: "operations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStatus = table.Column<int>(type: "int", nullable: false),
                    ToStatus = table.Column<int>(type: "int", nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequestStatusHistory_Requests_RequestId",
                        column: x => x.RequestId,
                        principalSchema: "operations",
                        principalTable: "Requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RequestUpdates",
                schema: "operations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsInternal = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestUpdates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequestUpdates_Requests_RequestId",
                        column: x => x.RequestId,
                        principalSchema: "operations",
                        principalTable: "Requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RequestAttachments_RequestId",
                schema: "operations",
                table: "RequestAttachments",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestParticipants_RequestId_UserId",
                schema: "operations",
                table: "RequestParticipants",
                columns: new[] { "RequestId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Requests_TenantId",
                schema: "operations",
                table: "Requests",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_TenantId_ApartmentId",
                schema: "operations",
                table: "Requests",
                columns: new[] { "TenantId", "ApartmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_Requests_TenantId_AssignedToUserId",
                schema: "operations",
                table: "Requests",
                columns: new[] { "TenantId", "AssignedToUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_Requests_TenantId_Code",
                schema: "operations",
                table: "Requests",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Requests_TenantId_Status",
                schema: "operations",
                table: "Requests",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RequestStatusHistory_RequestId",
                schema: "operations",
                table: "RequestStatusHistory",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestUpdates_RequestId",
                schema: "operations",
                table: "RequestUpdates",
                column: "RequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OperationSequence",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "RequestAttachments",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "RequestParticipants",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "RequestStatusHistory",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "RequestUpdates",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "Requests",
                schema: "operations");
        }
    }
}

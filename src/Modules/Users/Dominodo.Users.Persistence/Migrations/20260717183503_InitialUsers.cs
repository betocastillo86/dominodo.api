using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Dominodo.Users.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "users");

            migrationBuilder.CreateTable(
                name: "Permissions",
                schema: "users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Group = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                schema: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RevokedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReplacedByTokenId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedByIp = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                schema: "users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    Scope = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DocumentNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PasswordHash = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PhoneVerifiedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EmailVerifiedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PreferredLanguage = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    AvatarUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Profile = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VerificationCodes",
                schema: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CodeHash = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ConsumedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Attempts = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerificationCodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlatformRoleAssignments",
                schema: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformRoleAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlatformRoleAssignments_Roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "users",
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                schema: "users",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => new { x.RoleId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalSchema: "users",
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "users",
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "users",
                table: "Permissions",
                columns: new[] { "Id", "Code", "Description", "Group" },
                values: new object[,]
                {
                    { 1, "users.manage", "Gestionar usuarios.", "Usuarios" },
                    { 2, "roles.manage", "Gestionar roles y permisos.", "Usuarios" },
                    { 3, "requests.create", "Crear solicitudes (PQRS).", "Solicitudes" },
                    { 4, "requests.manage", "Gestionar solicitudes (PQRS).", "Solicitudes" },
                    { 5, "deliveries.register", "Registrar paquetería.", "Paquetería" },
                    { 6, "deliveries.manage", "Gestionar paquetería.", "Paquetería" },
                    { 7, "visits.register", "Registrar visitas.", "Visitas" },
                    { 8, "announcements.manage", "Gestionar boletines.", "Comunicaciones" },
                    { 9, "settings.manage", "Gestionar configuración.", "Administración" },
                    { 10, "tenants.create", "Crear conjuntos residenciales.", "Plataforma" },
                    { 11, "tenants.manage", "Gestionar conjuntos residenciales.", "Plataforma" }
                });

            migrationBuilder.InsertData(
                schema: "users",
                table: "Roles",
                columns: new[] { "Id", "Description", "IsSystem", "Name", "Scope" },
                values: new object[,]
                {
                    { 1, "Acceso total a la plataforma (cross-tenant).", true, "SuperAdmin", 0 },
                    { 2, "Administra un conjunto residencial.", true, "Administrador", 1 },
                    { 3, "Asiste en la administración del conjunto.", true, "AsistenteAdministracion", 1 },
                    { 4, "Personal de portería y seguridad.", true, "Vigilante", 1 },
                    { 5, "Residente de un apartamento.", true, "Residente", 1 }
                });

            migrationBuilder.InsertData(
                schema: "users",
                table: "Users",
                columns: new[] { "Id", "AvatarUrl", "CreatedAtUtc", "DocumentNumber", "DocumentType", "Email", "EmailVerifiedAtUtc", "FirstName", "LastName", "PasswordHash", "Phone", "PhoneVerifiedAtUtc", "PreferredLanguage", "Profile", "Status", "UpdatedAtUtc" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, "superadmin@dominodo.local", null, "Super", "Admin", "$2b$11$3qiA6Ogz7cU0k/slUmOy5uiFJOvKStCMp0VjaMPqiw7ry8PxOm71i", "+1111111", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "es", null, "Active", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.InsertData(
                schema: "users",
                table: "PlatformRoleAssignments",
                columns: new[] { "Id", "RoleId", "UserId" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000101"), 1, new Guid("00000000-0000-0000-0000-000000000001") });

            migrationBuilder.InsertData(
                schema: "users",
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { 1, 1 },
                    { 2, 1 },
                    { 3, 1 },
                    { 4, 1 },
                    { 5, 1 },
                    { 6, 1 },
                    { 7, 1 },
                    { 8, 1 },
                    { 9, 1 },
                    { 10, 1 },
                    { 11, 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Code",
                schema: "users",
                table: "Permissions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlatformRoleAssignments_RoleId",
                schema: "users",
                table: "PlatformRoleAssignments",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformRoleAssignments_UserId",
                schema: "users",
                table: "PlatformRoleAssignments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformRoleAssignments_UserId_RoleId",
                schema: "users",
                table: "PlatformRoleAssignments",
                columns: new[] { "UserId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                schema: "users",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                schema: "users",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                schema: "users",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                schema: "users",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_DocumentNumber",
                schema: "users",
                table: "Users",
                column: "DocumentNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                schema: "users",
                table: "Users",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Phone",
                schema: "users",
                table: "Users",
                column: "Phone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VerificationCodes_Phone_Purpose",
                schema: "users",
                table: "VerificationCodes",
                columns: new[] { "Phone", "Purpose" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlatformRoleAssignments",
                schema: "users");

            migrationBuilder.DropTable(
                name: "RefreshTokens",
                schema: "users");

            migrationBuilder.DropTable(
                name: "RolePermissions",
                schema: "users");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "users");

            migrationBuilder.DropTable(
                name: "VerificationCodes",
                schema: "users");

            migrationBuilder.DropTable(
                name: "Permissions",
                schema: "users");

            migrationBuilder.DropTable(
                name: "Roles",
                schema: "users");
        }
    }
}

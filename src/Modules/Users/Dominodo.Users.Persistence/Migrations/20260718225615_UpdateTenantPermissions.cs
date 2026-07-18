using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dominodo.Users.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTenantPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "users",
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "Code", "Description" },
                values: new object[] { "tenants.view", "Ver conjuntos residenciales." });

            migrationBuilder.InsertData(
                schema: "users",
                table: "Permissions",
                columns: new[] { "Id", "Code", "Description", "Group" },
                values: new object[] { 12, "tenants.edit", "Editar conjuntos residenciales.", "Plataforma" });

            migrationBuilder.InsertData(
                schema: "users",
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[] { 12, 1 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 12, 1 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.UpdateData(
                schema: "users",
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "Code", "Description" },
                values: new object[] { "tenants.manage", "Gestionar conjuntos residenciales." });
        }
    }
}

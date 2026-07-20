using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Dominodo.Users.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddApartmentPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "users",
                table: "Permissions",
                columns: new[] { "Id", "Code", "Description", "Group" },
                values: new object[,]
                {
                    { 14, "apartments.create", "Crear apartamentos.", "Apartamentos" },
                    { 15, "apartments.view", "Ver apartamentos.", "Apartamentos" },
                    { 16, "apartments.edit", "Editar apartamentos.", "Apartamentos" }
                });

            migrationBuilder.InsertData(
                schema: "users",
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { 14, 1 },
                    { 15, 1 },
                    { 16, 1 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 14, 1 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 15, 1 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 16, 1 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                schema: "users",
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                schema: "users",
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 16);
        }
    }
}

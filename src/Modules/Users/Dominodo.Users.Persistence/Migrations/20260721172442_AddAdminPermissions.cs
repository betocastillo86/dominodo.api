using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Dominodo.Users.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "users",
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "Code", "Description" },
                values: new object[] { "settings.view", "Ver configuración." });

            migrationBuilder.InsertData(
                schema: "users",
                table: "Permissions",
                columns: new[] { "Id", "Code", "Description", "Group" },
                values: new object[,]
                {
                    { 17, "settings.create", "Crear configuración.", "Administración" },
                    { 18, "settings.edit", "Editar configuración.", "Administración" },
                    { 19, "notifications.view", "Ver notificaciones.", "Notificaciones" },
                    { 20, "notifications.create", "Crear notificaciones.", "Notificaciones" },
                    { 21, "notifications.edit", "Editar notificaciones.", "Notificaciones" }
                });

            migrationBuilder.InsertData(
                schema: "users",
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { 9, 2 },
                    { 17, 1 },
                    { 18, 1 },
                    { 19, 1 },
                    { 20, 1 },
                    { 21, 1 },
                    { 17, 2 },
                    { 18, 2 },
                    { 19, 2 },
                    { 20, 2 },
                    { 21, 2 },
                    { 19, 3 },
                    { 20, 3 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 1 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 18, 1 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 19, 1 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 20, 1 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 21, 1 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 9, 2 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 2 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 18, 2 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 19, 2 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 20, 2 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 21, 2 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 19, 3 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 20, 3 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                schema: "users",
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                schema: "users",
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                schema: "users",
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                schema: "users",
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.UpdateData(
                schema: "users",
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "Code", "Description" },
                values: new object[] { "settings.manage", "Gestionar configuración." });
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Dominodo.Users.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class GranularOperationsPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 3, 1 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 4, 1 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 5, 1 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 6, 1 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 7, 1 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 8, 1 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                schema: "users",
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                schema: "users",
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                schema: "users",
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                schema: "users",
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                schema: "users",
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.InsertData(
                schema: "users",
                table: "Permissions",
                columns: new[] { "Id", "Code", "Description", "Group" },
                values: new object[,]
                {
                    { 23, "requests.view", "Ver solicitudes (PQRS).", "Solicitudes" },
                    { 24, "requests.edit", "Editar solicitudes (PQRS).", "Solicitudes" },
                    { 25, "requests.manage", "Gestionar el ciclo de vida de solicitudes (PQRS).", "Solicitudes" },
                    { 26, "requests.delete", "Eliminar o cancelar solicitudes (PQRS).", "Solicitudes" },
                    { 27, "deliveries.view", "Ver paquetería.", "Paquetería" },
                    { 28, "deliveries.edit", "Editar y cambiar el estado de paquetería.", "Paquetería" },
                    { 29, "deliveries.create", "Registrar paquetería.", "Paquetería" },
                    { 30, "visits.view", "Ver visitas.", "Visitas" },
                    { 31, "visits.edit", "Editar y cambiar el estado de visitas.", "Visitas" },
                    { 32, "visits.create", "Registrar visitas.", "Visitas" },
                    { 33, "announcements.view", "Ver comunicados (incl. borradores).", "Comunicaciones" },
                    { 34, "announcements.edit", "Editar, publicar y archivar comunicados.", "Comunicaciones" },
                    { 35, "announcements.create", "Crear borradores de comunicados.", "Comunicaciones" }
                });

            migrationBuilder.InsertData(
                schema: "users",
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { 23, 1 },
                    { 24, 1 },
                    { 25, 1 },
                    { 26, 1 },
                    { 27, 1 },
                    { 28, 1 },
                    { 29, 1 },
                    { 30, 1 },
                    { 31, 1 },
                    { 32, 1 },
                    { 33, 1 },
                    { 34, 1 },
                    { 35, 1 },
                    { 23, 2 },
                    { 24, 2 },
                    { 25, 2 },
                    { 26, 2 },
                    { 27, 2 },
                    { 28, 2 },
                    { 29, 2 },
                    { 30, 2 },
                    { 31, 2 },
                    { 32, 2 },
                    { 33, 2 },
                    { 34, 2 },
                    { 35, 2 },
                    { 23, 3 },
                    { 24, 3 },
                    { 25, 3 },
                    { 27, 3 },
                    { 28, 3 },
                    { 29, 3 },
                    { 30, 3 },
                    { 31, 3 },
                    { 32, 3 },
                    { 33, 3 },
                    { 34, 3 },
                    { 35, 3 },
                    { 27, 4 },
                    { 28, 4 },
                    { 29, 4 },
                    { 30, 4 },
                    { 31, 4 },
                    { 32, 4 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 23, 1 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 24, 1 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 25, 1 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 26, 1 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 27, 1 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 28, 1 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 29, 1 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 30, 1 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 31, 1 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 32, 1 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 33, 1 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 34, 1 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 35, 1 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 23, 2 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 24, 2 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 25, 2 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 26, 2 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 27, 2 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 28, 2 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 29, 2 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 30, 2 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 31, 2 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 32, 2 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 33, 2 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 34, 2 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 35, 2 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 23, 3 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 24, 3 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 25, 3 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 27, 3 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 28, 3 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 29, 3 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 30, 3 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 31, 3 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 32, 3 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 33, 3 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 34, 3 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 35, 3 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 27, 4 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 28, 4 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 29, 4 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 30, 4 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 31, 4 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 32, 4 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                schema: "users",
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                schema: "users",
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                schema: "users",
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                schema: "users",
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 27);

            migrationBuilder.DeleteData(
                schema: "users",
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 28);

            migrationBuilder.DeleteData(
                schema: "users",
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 29);

            migrationBuilder.DeleteData(
                schema: "users",
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 30);

            migrationBuilder.DeleteData(
                schema: "users",
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 31);

            migrationBuilder.DeleteData(
                schema: "users",
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 32);

            migrationBuilder.DeleteData(
                schema: "users",
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 33);

            migrationBuilder.DeleteData(
                schema: "users",
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 34);

            migrationBuilder.DeleteData(
                schema: "users",
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 35);

            migrationBuilder.InsertData(
                schema: "users",
                table: "Permissions",
                columns: new[] { "Id", "Code", "Description", "Group" },
                values: new object[,]
                {
                    { 3, "requests.create", "Crear solicitudes (PQRS).", "Solicitudes" },
                    { 4, "requests.manage", "Gestionar solicitudes (PQRS).", "Solicitudes" },
                    { 5, "deliveries.register", "Registrar paquetería.", "Paquetería" },
                    { 6, "deliveries.manage", "Gestionar paquetería.", "Paquetería" },
                    { 7, "visits.register", "Registrar visitas.", "Visitas" },
                    { 8, "announcements.manage", "Gestionar boletines.", "Comunicaciones" }
                });

            migrationBuilder.InsertData(
                schema: "users",
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { 3, 1 },
                    { 4, 1 },
                    { 5, 1 },
                    { 6, 1 },
                    { 7, 1 },
                    { 8, 1 }
                });
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Dominodo.Users.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMembershipsManagePermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "users",
                table: "Permissions",
                columns: new[] { "Id", "Code", "Description", "Group" },
                values: new object[] { 13, "memberships.manage", "Gestionar membresías de un conjunto.", "Membresías" });

            migrationBuilder.InsertData(
                schema: "users",
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { 13, 1 },
                    { 13, 2 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 13, 1 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 13, 2 });

            migrationBuilder.DeleteData(
                schema: "users",
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 13);
        }
    }
}

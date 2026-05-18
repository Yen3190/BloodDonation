using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VLU.BloodDonation.Api.Migrations
{
    /// <inheritdoc />
    public partial class SeedAdminUserNew : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "BloodType", "DateCreated", "Email", "FullName", "IsApproved", "PasswordHash", "RhFactor", "Role", "StudentId", "TotalPoints" },
                values: new object[] { 999999, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "admin.vlu@vanlanguni.vn", "Quản Trị Viên Hệ Thống", true, "admin_secure_password_hash", null, "Admin", "ADMIN-GOC", 0 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 999999);
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VLU.BloodDonation.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDescriptionToBloodEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "BloodEvents",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "BloodEvents");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FloodRescue.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddCitizenEmailToRescueRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CitizenEmail",
                table: "RescueRequests",
                type: "nvarchar(300)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CitizenEmail",
                table: "RescueRequests");
        }
    }
}

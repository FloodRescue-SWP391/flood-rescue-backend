using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FloodRescue.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddRescueRequestImagesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RescueRequestImages",
                columns: table => new
                {
                    ImageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    RescueRequestID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RescueTeamID = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RescueRequestImages", x => x.ImageId);
                    table.ForeignKey(
                        name: "FK_RescueRequestImages_RescueRequests_RescueRequestID",
                        column: x => x.RescueRequestID,
                        principalTable: "RescueRequests",
                        principalColumn: "RescueRequestID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RescueRequestImages_RescueTeams_RescueTeamID",
                        column: x => x.RescueTeamID,
                        principalTable: "RescueTeams",
                        principalColumn: "RescueTeamID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_RescueRequestImages_RescueRequestID",
                table: "RescueRequestImages",
                column: "RescueRequestID");

            migrationBuilder.CreateIndex(
                name: "IX_RescueRequestImages_RescueTeamID",
                table: "RescueRequestImages",
                column: "RescueTeamID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RescueRequestImages");
        }
    }
}

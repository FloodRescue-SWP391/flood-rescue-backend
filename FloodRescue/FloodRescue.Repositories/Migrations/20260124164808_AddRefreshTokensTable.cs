using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FloodRescue.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokensTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RescueRequestImages_RescueRequests_RescueRequestID",
                table: "RescueRequestImages");

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    RefresTokenID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Token = table.Column<string>(type: "varchar(500)", nullable: false),
                    JwtID = table.Column<string>(type: "varchar(100)", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserID = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.RefresTokenID);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserID",
                table: "RefreshTokens",
                column: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_RescueRequestImages_RescueRequests_RescueRequestID",
                table: "RescueRequestImages",
                column: "RescueRequestID",
                principalTable: "RescueRequests",
                principalColumn: "RescueRequestID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RescueRequestImages_RescueRequests_RescueRequestID",
                table: "RescueRequestImages");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.AddForeignKey(
                name: "FK_RescueRequestImages_RescueRequests_RescueRequestID",
                table: "RescueRequestImages",
                column: "RescueRequestID",
                principalTable: "RescueRequests",
                principalColumn: "RescueRequestID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

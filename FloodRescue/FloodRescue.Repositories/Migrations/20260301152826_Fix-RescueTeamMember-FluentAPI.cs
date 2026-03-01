using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FloodRescue.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class FixRescueTeamMemberFluentAPI : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RescueTeamMemberUserID",
                table: "Users",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_RescueTeamMemberUserID",
                table: "Users",
                column: "RescueTeamMemberUserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_RescueTeamMembers_RescueTeamMemberUserID",
                table: "Users",
                column: "RescueTeamMemberUserID",
                principalTable: "RescueTeamMembers",
                principalColumn: "UserID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_RescueTeamMembers_RescueTeamMemberUserID",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_RescueTeamMemberUserID",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RescueTeamMemberUserID",
                table: "Users");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FloodRescue.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class MoveCoordinatorIDToRescueMission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RescueRequests_Users_CoordinatorID",
                table: "RescueRequests");

            migrationBuilder.DropIndex(
                name: "IX_RescueRequests_CoordinatorID",
                table: "RescueRequests");

            migrationBuilder.DropColumn(
                name: "CoordinatorID",
                table: "RescueRequests");

            migrationBuilder.AddColumn<Guid>(
                name: "CoordinatorID",
                table: "RescueMissions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RescueMissions_CoordinatorID",
                table: "RescueMissions",
                column: "CoordinatorID");

            migrationBuilder.AddForeignKey(
                name: "FK_RescueMissions_Users_CoordinatorID",
                table: "RescueMissions",
                column: "CoordinatorID",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RescueMissions_Users_CoordinatorID",
                table: "RescueMissions");

            migrationBuilder.DropIndex(
                name: "IX_RescueMissions_CoordinatorID",
                table: "RescueMissions");

            migrationBuilder.DropColumn(
                name: "CoordinatorID",
                table: "RescueMissions");

            migrationBuilder.AddColumn<Guid>(
                name: "CoordinatorID",
                table: "RescueRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RescueRequests_CoordinatorID",
                table: "RescueRequests",
                column: "CoordinatorID");

            migrationBuilder.AddForeignKey(
                name: "FK_RescueRequests_Users_CoordinatorID",
                table: "RescueRequests",
                column: "CoordinatorID",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

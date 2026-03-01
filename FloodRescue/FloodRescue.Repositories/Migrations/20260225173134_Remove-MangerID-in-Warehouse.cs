using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FloodRescue.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class RemoveMangerIDinWarehouse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Warehouses_Users_ManagerID",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_Warehouses_ManagerID",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "ManagerID",
                table: "Warehouses");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ManagerID",
                table: "Warehouses",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_ManagerID",
                table: "Warehouses",
                column: "ManagerID");

            migrationBuilder.AddForeignKey(
                name: "FK_Warehouses_Users_ManagerID",
                table: "Warehouses",
                column: "ManagerID",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FloodRescue.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class RemoveWarehouseInReliefOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReliefOrders_Warehouses_WarehouseID",
                table: "ReliefOrders");

            migrationBuilder.DropIndex(
                name: "IX_ReliefOrders_WarehouseID",
                table: "ReliefOrders");

            migrationBuilder.DropColumn(
                name: "WarehouseID",
                table: "ReliefOrders");

            migrationBuilder.AlterColumn<Guid>(
                name: "ManagerID",
                table: "ReliefOrders",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "ManagerID",
                table: "ReliefOrders",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WarehouseID",
                table: "ReliefOrders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ReliefOrders_WarehouseID",
                table: "ReliefOrders",
                column: "WarehouseID");

            migrationBuilder.AddForeignKey(
                name: "FK_ReliefOrders_Warehouses_WarehouseID",
                table: "ReliefOrders",
                column: "WarehouseID",
                principalTable: "Warehouses",
                principalColumn: "WarehouseID",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

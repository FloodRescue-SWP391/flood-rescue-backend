using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FloodRescue.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddPickedUpTimeInReliefOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PickedUpTime",
                table: "ReliefOrders",
                type: "datetime2(7)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PickedUpTime",
                table: "ReliefOrders");
        }
    }
}

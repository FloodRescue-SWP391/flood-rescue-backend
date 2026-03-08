using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FloodRescue.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class ExtractUnitToSeparateTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Unit",
                table: "ReliefItems");

            migrationBuilder.AddColumn<int>(
                name: "UnitID",
                table: "ReliefItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Units",
                columns: table => new
                {
                    UnitID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UnitName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsDeleted = table.Column<bool>(type: "BIT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Units", x => x.UnitID);
                });

            migrationBuilder.InsertData(
                table: "Units",
                columns: new[] { "UnitID", "IsDeleted", "UnitName" },
                values: new object[,]
                {
                    { 1, false, "Thùng" },
                    { 2, false, "Hộp" },
                    { 3, false, "Chai" },
                    { 4, false, "Gói" },
                    { 5, false, "Bịch" },
                    { 6, false, "Cái" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReliefItems_UnitID",
                table: "ReliefItems",
                column: "UnitID");

            migrationBuilder.AddForeignKey(
                name: "FK_ReliefItems_Units_UnitID",
                table: "ReliefItems",
                column: "UnitID",
                principalTable: "Units",
                principalColumn: "UnitID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReliefItems_Units_UnitID",
                table: "ReliefItems");

            migrationBuilder.DropTable(
                name: "Units");

            migrationBuilder.DropIndex(
                name: "IX_ReliefItems_UnitID",
                table: "ReliefItems");

            migrationBuilder.DropColumn(
                name: "UnitID",
                table: "ReliefItems");

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "ReliefItems",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }
    }
}

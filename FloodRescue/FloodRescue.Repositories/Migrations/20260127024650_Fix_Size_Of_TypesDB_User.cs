using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FloodRescue.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class Fix_Size_Of_TypesDB_User : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "Salt",
                table: "Users",
                type: "varbinary(128)",
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "varbinary(64)");

            migrationBuilder.AlterColumn<byte[]>(
                name: "Password",
                table: "Users",
                type: "varbinary(64)",
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "varbinary(50)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "Salt",
                table: "Users",
                type: "varbinary(64)",
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "varbinary(128)");

            migrationBuilder.AlterColumn<byte[]>(
                name: "Password",
                table: "Users",
                type: "varbinary(50)",
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "varbinary(64)");
        }
    }
}

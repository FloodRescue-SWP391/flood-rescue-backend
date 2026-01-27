using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FloodRescue.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class Fix_TypesName_User : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Xóa columns cũ
            migrationBuilder.DropColumn(name: "Password", table: "Users");
            migrationBuilder.DropColumn(name: "Salt", table: "Users");

            // 2. Thêm columns mới với đúng type
            migrationBuilder.AddColumn<byte[]>(
                name: "Password",
                table: "Users",
                type: "varbinary(64)",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "Salt",
                table: "Users",
                type: "varbinary(128)",
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Password", table: "Users");
            migrationBuilder.DropColumn(name: "Salt", table: "Users");

            migrationBuilder.AddColumn<string>(
                name: "Password",
                table: "Users",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Salt",
                table: "Users",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");
        }
    }
}

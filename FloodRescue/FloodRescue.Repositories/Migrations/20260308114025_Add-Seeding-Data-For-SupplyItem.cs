using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FloodRescue.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddSeedingDataForSupplyItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Insert Units TRƯỚC TIÊN (Không bị lỗi khóa ngoại)
            migrationBuilder.InsertData(
                table: "Units",
                columns: new[] { "UnitID", "UnitName", "IsDeleted" },
                values: new object[,]
                {
            { 1, "Thùng", false },
            { 2, "Hộp", false },
            { 3, "Chai", false },
            { 4, "Gói", false },
            { 5, "Bịch", false },
            { 6, "Cái", false }
                });

            // 2. Insert Categories (Danh mục)
            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "CategoryID", "CategoryName", "IsDeleted" },
                values: new object[,]
                {
            { 1, "Nước uống", false },
            { 2, "Đồ ăn", false },
            { 3, "Thuốc", false }
                });

            // 3. Insert ReliefItems (Hàng hóa) SAU CÙNG
            migrationBuilder.InsertData(
                table: "ReliefItems",
                columns: new[] { "ReliefItemID", "CategoryID", "IsDeleted", "ReliefItemName", "UnitID" },
                values: new object[,]
                {
            { 1, 1, false, "Aquafina", 1 },
            { 2, 1, false, "Lavie", 1 },
            { 3, 1, false, "Ionlife", 1 },
            { 4, 2, false, "Lương Khô", 4 },
            { 5, 2, false, "Bánh Mì", 6 },
            { 6, 2, false, "Mì Tôm", 1 }
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Khi Rollback (Down) thì phải xóa ngược lại từ dưới lên: Hàng hóa -> Danh mục -> Đơn vị
            migrationBuilder.DeleteData(table: "ReliefItems", keyColumn: "ReliefItemID", keyValue: 1);
            migrationBuilder.DeleteData(table: "ReliefItems", keyColumn: "ReliefItemID", keyValue: 2);
            migrationBuilder.DeleteData(table: "ReliefItems", keyColumn: "ReliefItemID", keyValue: 3);
            migrationBuilder.DeleteData(table: "ReliefItems", keyColumn: "ReliefItemID", keyValue: 4);
            migrationBuilder.DeleteData(table: "ReliefItems", keyColumn: "ReliefItemID", keyValue: 5);
            migrationBuilder.DeleteData(table: "ReliefItems", keyColumn: "ReliefItemID", keyValue: 6);

            migrationBuilder.DeleteData(table: "Categories", keyColumn: "CategoryID", keyValue: 1);
            migrationBuilder.DeleteData(table: "Categories", keyColumn: "CategoryID", keyValue: 2);
            migrationBuilder.DeleteData(table: "Categories", keyColumn: "CategoryID", keyValue: 3);

            // Tui bù thêm lệnh xóa bảng Units cho ông luôn nè
            migrationBuilder.DeleteData(table: "Units", keyColumn: "UnitID", keyValue: 1);
            migrationBuilder.DeleteData(table: "Units", keyColumn: "UnitID", keyValue: 2);
            migrationBuilder.DeleteData(table: "Units", keyColumn: "UnitID", keyValue: 3);
            migrationBuilder.DeleteData(table: "Units", keyColumn: "UnitID", keyValue: 4);
            migrationBuilder.DeleteData(table: "Units", keyColumn: "UnitID", keyValue: 5);
            migrationBuilder.DeleteData(table: "Units", keyColumn: "UnitID", keyValue: 6);
        }
    }
}

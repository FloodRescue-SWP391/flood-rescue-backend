using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FloodRescue.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class InitialDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    CategoryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsDeleted = table.Column<bool>(type: "BIT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.CategoryID);
                });

            migrationBuilder.CreateTable(
                name: "RescueTeams",
                columns: table => new
                {
                    RescueTeamID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeamName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    City = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CurrentStatus = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    CurrentLatitude = table.Column<double>(type: "float", nullable: false),
                    CurrentLongitude = table.Column<double>(type: "float", nullable: false),
                    IsDeleted = table.Column<bool>(type: "BIT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RescueTeams", x => x.RescueTeamID);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    RoleID = table.Column<string>(type: "char(2)", maxLength: 2, nullable: false),
                    RoleName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    IsDeleted = table.Column<bool>(type: "BIT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.RoleID);
                });

            migrationBuilder.CreateTable(
                name: "ReliefItems",
                columns: table => new
                {
                    ReliefItemID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReliefItemName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CategoryID = table.Column<int>(type: "int", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsDeleted = table.Column<bool>(type: "BIT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReliefItems", x => x.ReliefItemID);
                    table.ForeignKey(
                        name: "FK_ReliefItems_Categories_CategoryID",
                        column: x => x.CategoryID,
                        principalTable: "Categories",
                        principalColumn: "CategoryID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Username = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Password = table.Column<string>(type: "varchar(255)", maxLength: 25, nullable: false),
                    Phone = table.Column<string>(type: "varchar(15)", maxLength: 15, nullable: false),
                    FullName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    RoleID = table.Column<string>(type: "char(2)", maxLength: 2, nullable: false),
                    IsDeleted = table.Column<bool>(type: "BIT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserID);
                    table.ForeignKey(
                        name: "FK_Users_Roles_RoleID",
                        column: x => x.RoleID,
                        principalTable: "Roles",
                        principalColumn: "RoleID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    NotificationID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReferenceID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRead = table.Column<bool>(type: "BIT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.NotificationID);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RescueRequests",
                columns: table => new
                {
                    RescueRequestID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CitizenName = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    CitizenPhone = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(225)", nullable: true),
                    LocationLatitude = table.Column<double>(type: "float", nullable: false),
                    LocationLongitude = table.Column<double>(type: "float", nullable: false),
                    PeopleCount = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestType = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    ShortCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    RejectedNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    CoordinatorID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "BIT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RescueRequests", x => x.RescueRequestID);
                    table.ForeignKey(
                        name: "FK_RescueRequests_Users_CoordinatorID",
                        column: x => x.CoordinatorID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RescueTeamMembers",
                columns: table => new
                {
                    UserID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RescueTeamID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsLeader = table.Column<bool>(type: "BIT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "BIT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RescueTeamMembers", x => x.UserID);
                    table.ForeignKey(
                        name: "FK_RescueTeamMembers_RescueTeams_RescueTeamID",
                        column: x => x.RescueTeamID,
                        principalTable: "RescueTeams",
                        principalColumn: "RescueTeamID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RescueTeamMembers_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Warehouses",
                columns: table => new
                {
                    WarehouseID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ManagerID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    LocationLong = table.Column<double>(type: "float", nullable: false),
                    LocationLat = table.Column<double>(type: "float", nullable: false),
                    IsDeleted = table.Column<bool>(type: "BIT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warehouses", x => x.WarehouseID);
                    table.ForeignKey(
                        name: "FK_Warehouses_Users_ManagerID",
                        column: x => x.ManagerID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CitizenNotification",
                columns: table => new
                {
                    NotificationID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RescueRequestID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CitizenNotification", x => x.NotificationID);
                    table.ForeignKey(
                        name: "FK_CitizenNotification_RescueRequests_RescueRequestID",
                        column: x => x.RescueRequestID,
                        principalTable: "RescueRequests",
                        principalColumn: "RescueRequestID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RescueMissions",
                columns: table => new
                {
                    RescueMissionID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RescueTeamID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RescueRequestID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    EndTime = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "BIT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RescueMissions", x => x.RescueMissionID);
                    table.ForeignKey(
                        name: "FK_RescueMissions_RescueRequests_RescueRequestID",
                        column: x => x.RescueRequestID,
                        principalTable: "RescueRequests",
                        principalColumn: "RescueRequestID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RescueMissions_RescueTeams_RescueTeamID",
                        column: x => x.RescueTeamID,
                        principalTable: "RescueTeams",
                        principalColumn: "RescueTeamID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Inventories",
                columns: table => new
                {
                    InventoryID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReliefItemID = table.Column<int>(type: "int", nullable: false),
                    WarehouseID = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inventories", x => x.InventoryID);
                    table.ForeignKey(
                        name: "FK_Inventories_ReliefItems_ReliefItemID",
                        column: x => x.ReliefItemID,
                        principalTable: "ReliefItems",
                        principalColumn: "ReliefItemID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Inventories_Warehouses_WarehouseID",
                        column: x => x.WarehouseID,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReliefOrders",
                columns: table => new
                {
                    ReliefOrderID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RescueRequestID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ManagerID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseID = table.Column<int>(type: "int", nullable: false),
                    RescueTeamID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    PreparedTime = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsDeleted = table.Column<bool>(type: "BIT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReliefOrders", x => x.ReliefOrderID);
                    table.ForeignKey(
                        name: "FK_ReliefOrders_RescueRequests_RescueRequestID",
                        column: x => x.RescueRequestID,
                        principalTable: "RescueRequests",
                        principalColumn: "RescueRequestID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReliefOrders_RescueTeams_RescueTeamID",
                        column: x => x.RescueTeamID,
                        principalTable: "RescueTeams",
                        principalColumn: "RescueTeamID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReliefOrders_Users_ManagerID",
                        column: x => x.ManagerID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReliefOrders_Warehouses_WarehouseID",
                        column: x => x.WarehouseID,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IncidentReports",
                columns: table => new
                {
                    IncidentReportID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RescueMissionID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportedID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResolvedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResolvedTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    Latitiude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CoordinatorNote = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentReports", x => x.IncidentReportID);
                    table.ForeignKey(
                        name: "FK_IncidentReports_RescueMissions_RescueMissionID",
                        column: x => x.RescueMissionID,
                        principalTable: "RescueMissions",
                        principalColumn: "RescueMissionID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IncidentReports_Users_ReportedID",
                        column: x => x.ReportedID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IncidentReports_Users_ResolvedBy",
                        column: x => x.ResolvedBy,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReliefOrderDetails",
                columns: table => new
                {
                    ReliefOrderDetailID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReliefOrderID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReliefItemID = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReliefOrderDetails", x => x.ReliefOrderDetailID);
                    table.ForeignKey(
                        name: "FK_ReliefOrderDetails_ReliefItems_ReliefItemID",
                        column: x => x.ReliefItemID,
                        principalTable: "ReliefItems",
                        principalColumn: "ReliefItemID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReliefOrderDetails_ReliefOrders_ReliefOrderID",
                        column: x => x.ReliefOrderID,
                        principalTable: "ReliefOrders",
                        principalColumn: "ReliefOrderID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "RoleID", "IsDeleted", "RoleName" },
                values: new object[,]
                {
                    { "AD", false, "Admin" },
                    { "IM", false, "Inventory Manager" },
                    { "RC", false, "Rescue Coordinator" },
                    { "RT", false, "Rescue Team Member" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CitizenNotification_RescueRequestID",
                table: "CitizenNotification",
                column: "RescueRequestID");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentReports_ReportedID",
                table: "IncidentReports",
                column: "ReportedID");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentReports_RescueMissionID",
                table: "IncidentReports",
                column: "RescueMissionID");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentReports_ResolvedBy",
                table: "IncidentReports",
                column: "ResolvedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_ReliefItemID",
                table: "Inventories",
                column: "ReliefItemID");

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_WarehouseID",
                table: "Inventories",
                column: "WarehouseID");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserID",
                table: "Notifications",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_ReliefItems_CategoryID",
                table: "ReliefItems",
                column: "CategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_ReliefOrderDetails_ReliefItemID",
                table: "ReliefOrderDetails",
                column: "ReliefItemID");

            migrationBuilder.CreateIndex(
                name: "IX_ReliefOrderDetails_ReliefOrderID",
                table: "ReliefOrderDetails",
                column: "ReliefOrderID");

            migrationBuilder.CreateIndex(
                name: "IX_ReliefOrders_ManagerID",
                table: "ReliefOrders",
                column: "ManagerID");

            migrationBuilder.CreateIndex(
                name: "IX_ReliefOrders_RescueRequestID",
                table: "ReliefOrders",
                column: "RescueRequestID");

            migrationBuilder.CreateIndex(
                name: "IX_ReliefOrders_RescueTeamID",
                table: "ReliefOrders",
                column: "RescueTeamID");

            migrationBuilder.CreateIndex(
                name: "IX_ReliefOrders_WarehouseID",
                table: "ReliefOrders",
                column: "WarehouseID");

            migrationBuilder.CreateIndex(
                name: "IX_RescueMissions_RescueRequestID",
                table: "RescueMissions",
                column: "RescueRequestID");

            migrationBuilder.CreateIndex(
                name: "IX_RescueMissions_RescueTeamID",
                table: "RescueMissions",
                column: "RescueTeamID");

            migrationBuilder.CreateIndex(
                name: "IX_RescueRequests_CoordinatorID",
                table: "RescueRequests",
                column: "CoordinatorID");

            migrationBuilder.CreateIndex(
                name: "IX_RescueRequests_ShortCode",
                table: "RescueRequests",
                column: "ShortCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RescueTeamMembers_RescueTeamID",
                table: "RescueTeamMembers",
                column: "RescueTeamID");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Phone",
                table: "Users",
                column: "Phone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleID",
                table: "Users",
                column: "RoleID");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_ManagerID",
                table: "Warehouses",
                column: "ManagerID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CitizenNotification");

            migrationBuilder.DropTable(
                name: "IncidentReports");

            migrationBuilder.DropTable(
                name: "Inventories");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "ReliefOrderDetails");

            migrationBuilder.DropTable(
                name: "RescueTeamMembers");

            migrationBuilder.DropTable(
                name: "RescueMissions");

            migrationBuilder.DropTable(
                name: "ReliefItems");

            migrationBuilder.DropTable(
                name: "ReliefOrders");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "RescueRequests");

            migrationBuilder.DropTable(
                name: "RescueTeams");

            migrationBuilder.DropTable(
                name: "Warehouses");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}

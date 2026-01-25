using AutoMapper;
using FloodRescue.Repositories.Context;
using FloodRescue.Repositories.Entites;
using FloodRescue.Repositories.Implements;
using FloodRescue.Repositories.Interface;
using FloodRescue.Services.DTO.Request.Warehouse;
using FloodRescue.Services.DTO.Request.WarehouseRequest;
using FloodRescue.Services.DTO.Response.Warehouse;
using FloodRescue.Services.Implements;
using FloodRescue.Services.Interface;
using FloodRescue.Services.Mapper;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace FloodRescue.Tests.Services
{
    /// <summary>
    /// Unit Tests cho WarehouseService
    /// Test các ch?c n?ng CRUD: Create, Read, Update, Delete Warehouse
    /// </summary>
    [TestFixture]
    public class WarehouseServiceTests
    {
        // ===== KHAI BÁO BI?N =====
        private FloodRescueDbContext _context = null!;
        private IUnitOfWork _unitOfWork = null!;
        private IMapper _mapper = null!;
        private IWarehouseService _warehouseService = null!;

        // Test data
        private User _testManager = null!;
        private Role _testRole = null!;

        [SetUp]
        public void Setup()
        {
            // B??C 1: T?o InMemory Database
            var options = new DbContextOptionsBuilder<FloodRescueDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new FloodRescueDbContext(options);

            // B??C 2: Kh?i t?o UnitOfWork
            _unitOfWork = new UnitOfWork(_context);

            // B??C 3: C?u hình AutoMapper
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            });
            _mapper = config.CreateMapper();

            // B??C 4: Kh?i t?o WarehouseService
            _warehouseService = new WarehouseService(_unitOfWork, _mapper);

            // B??C 5: T?o test data
            _testRole = new Role
            {
                RoleID = "IM",
                RoleName = "Inventory Manager",
                IsDeleted = false
            };

            _testManager = new User
            {
                UserID = Guid.NewGuid(),
                Username = "manager1",
                Password = "Manager123",
                Phone = "0901234567",
                FullName = "Test Manager",
                RoleID = "IM",
                Role = _testRole,
                IsDeleted = false
            };

            // Thêm vào context
            _context.Roles.Add(_testRole);
            _context.Users.Add(_testManager);
            _context.SaveChanges();
        }

        [TearDown]
        public void TearDown()
        {
            _unitOfWork?.Dispose();
            _context?.Dispose();
        }

        #region ===== TEST CreateWarehouseAsync =====

        [Test]
        public async Task CreateWarehouseAsync_WhenValidRequest_ShouldCreateWarehouse()
        {
            // ARRANGE
            var request = new CreateWarehouseRequestDTO
            {
                ManagerID = _testManager.UserID,
                Name = "Test Warehouse",
                Address = "123 Test Street",
                LocationLong = 106.660172,
                LocationLat = 10.762622
            };

            // ACT
            var result = await _warehouseService.CreateWarehouseAsync(request);

            // ASSERT
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Name, Is.EqualTo(request.Name));
                Assert.That(result.Address, Is.EqualTo(request.Address));
            });

            // Verify database
            var warehouseInDb = await _unitOfWork.Warehouses.GetAsync(w => w.Name == "Test Warehouse");
            Assert.That(warehouseInDb, Is.Not.Null);
        }

        [Test]
        public async Task CreateWarehouseAsync_ShouldSaveToDatabase()
        {
            // ARRANGE
            var request = new CreateWarehouseRequestDTO
            {
                ManagerID = _testManager.UserID,
                Name = "Warehouse DB Test",
                Address = "456 DB Street",
                LocationLong = 106.5,
                LocationLat = 10.5
            };

            // ACT
            var result = await _warehouseService.CreateWarehouseAsync(request);

            // ASSERT - Check database
            var warehouseCount = (await _unitOfWork.Warehouses.GetAllAsync()).Count;
            Assert.That(warehouseCount, Is.EqualTo(1));
        }

        #endregion

        #region ===== TEST SearchWarehouseAsync =====

        [Test]
        public async Task SearchWarehouseAsync_WhenWarehouseExists_ShouldReturnWarehouse()
        {
            // ARRANGE - T?o warehouse tr??c
            var warehouse = new Warehouse
            {
                WarehouseID = 1,
                ManagerID = _testManager.UserID,
                Name = "Search Test Warehouse",
                Address = "Search Street",
                LocationLong = 106.0,
                LocationLat = 10.0,
                IsDeleted = false
            };
            await _unitOfWork.Warehouses.AddAsync(warehouse);
            await _unitOfWork.SaveChangesAsync();

            // ACT
            var result = await _warehouseService.SearchWarehouseAsync(1);

            // ASSERT
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result!.Name, Is.EqualTo("Search Test Warehouse"));
                Assert.That(result.Address, Is.EqualTo("Search Street"));
            });
        }

        [Test]
        public async Task SearchWarehouseAsync_WhenWarehouseNotExists_ShouldReturnNull()
        {
            // ACT
            var result = await _warehouseService.SearchWarehouseAsync(999);

            // ASSERT
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task SearchWarehouseAsync_WhenWarehouseIsDeleted_ShouldReturnNull()
        {
            // ARRANGE - T?o warehouse ?ã xóa
            var warehouse = new Warehouse
            {
                WarehouseID = 2,
                ManagerID = _testManager.UserID,
                Name = "Deleted Warehouse",
                Address = "Deleted Street",
                LocationLong = 106.0,
                LocationLat = 10.0,
                IsDeleted = true  // ?Ã XÓA
            };
            await _unitOfWork.Warehouses.AddAsync(warehouse);
            await _unitOfWork.SaveChangesAsync();

            // ACT
            var result = await _warehouseService.SearchWarehouseAsync(2);

            // ASSERT - Không tìm th?y vì ?ã b? xóa
            Assert.That(result, Is.Null);
        }

        #endregion

        #region ===== TEST GetAllWarehousesAsync =====

        [Test]
        public async Task GetAllWarehousesAsync_WhenNoWarehouses_ShouldReturnEmptyList()
        {
            // ACT
            var result = await _warehouseService.GetAllWarehousesAsync();

            // ASSERT
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task GetAllWarehousesAsync_WhenHasWarehouses_ShouldReturnAll()
        {
            // ARRANGE - T?o 3 warehouses
            var warehouses = new List<Warehouse>
            {
                new Warehouse
                {
                    ManagerID = _testManager.UserID,
                    Name = "Warehouse 1",
                    Address = "Address 1",
                    LocationLong = 106.0,
                    LocationLat = 10.0,
                    IsDeleted = false
                },
                new Warehouse
                {
                    ManagerID = _testManager.UserID,
                    Name = "Warehouse 2",
                    Address = "Address 2",
                    LocationLong = 106.1,
                    LocationLat = 10.1,
                    IsDeleted = false
                },
                new Warehouse
                {
                    ManagerID = _testManager.UserID,
                    Name = "Warehouse 3",
                    Address = "Address 3",
                    LocationLong = 106.2,
                    LocationLat = 10.2,
                    IsDeleted = false
                }
            };

            foreach (var wh in warehouses)
            {
                await _unitOfWork.Warehouses.AddAsync(wh);
            }
            await _unitOfWork.SaveChangesAsync();

            // ACT
            var result = await _warehouseService.GetAllWarehousesAsync();

            // ASSERT
            Assert.That(result.Count, Is.EqualTo(3));
        }

        [Test]
        public async Task GetAllWarehousesAsync_ShouldNotReturnDeletedWarehouses()
        {
            // ARRANGE - T?o 2 warehouse active, 1 deleted
            var warehouses = new List<Warehouse>
            {
                new Warehouse
                {
                    ManagerID = _testManager.UserID,
                    Name = "Active 1",
                    Address = "Address 1",
                    LocationLong = 106.0,
                    LocationLat = 10.0,
                    IsDeleted = false
                },
                new Warehouse
                {
                    ManagerID = _testManager.UserID,
                    Name = "Active 2",
                    Address = "Address 2",
                    LocationLong = 106.1,
                    LocationLat = 10.1,
                    IsDeleted = false
                },
                new Warehouse
                {
                    ManagerID = _testManager.UserID,
                    Name = "Deleted",
                    Address = "Address 3",
                    LocationLong = 106.2,
                    LocationLat = 10.2,
                    IsDeleted = true  // DELETED
                }
            };

            foreach (var wh in warehouses)
            {
                await _unitOfWork.Warehouses.AddAsync(wh);
            }
            await _unitOfWork.SaveChangesAsync();

            // ACT
            var result = await _warehouseService.GetAllWarehousesAsync();

            // ASSERT - Ch? tr? v? 2 active warehouses
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.All(w => w.Name != "Deleted"), Is.True);
        }

        #endregion

        #region ===== TEST UpdateWarehouseAsync =====

        [Test]
        public async Task UpdateWarehouseAsync_WhenWarehouseExists_ShouldUpdate()
        {
            // ARRANGE - T?o warehouse
            var warehouse = new Warehouse
            {
                WarehouseID = 10,
                ManagerID = _testManager.UserID,
                Name = "Old Name",
                Address = "Old Address",
                LocationLong = 100.0,
                LocationLat = 20.0,
                IsDeleted = false
            };
            await _unitOfWork.Warehouses.AddAsync(warehouse);
            await _unitOfWork.SaveChangesAsync();

            var updateRequest = new UpdateWarehouseRequestDTO
            {
                Name = "New Name",
                Address = "New Address",
                LocationLong = 105.0,
                LocationLat = 25.0,
                IsDeleted = false
            };

            // ACT
            var result = await _warehouseService.UpdateWarehouseAsync(10, updateRequest);

            // ASSERT
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result!.Name, Is.EqualTo("New Name"));
                Assert.That(result.Address, Is.EqualTo("New Address"));
            });

            // Verify in database
            var updatedInDb = await _unitOfWork.Warehouses.GetAsync(w => w.WarehouseID == 10);
            Assert.That(updatedInDb!.Name, Is.EqualTo("New Name"));
        }

        [Test]
        public async Task UpdateWarehouseAsync_WhenWarehouseNotExists_ShouldReturnNull()
        {
            // ARRANGE
            var updateRequest = new UpdateWarehouseRequestDTO
            {
                Name = "New Name",
                Address = "New Address",
                LocationLong = 105.0,
                LocationLat = 25.0,
                IsDeleted = false
            };

            // ACT
            var result = await _warehouseService.UpdateWarehouseAsync(999, updateRequest);

            // ASSERT
            Assert.That(result, Is.Null);
        }

        #endregion

        #region ===== TEST DeleteWarehouseAsync =====

        [Test]
        public async Task DeleteWarehouseAsync_WhenWarehouseExists_ShouldMarkAsDeleted()
        {
            // ARRANGE
            var warehouse = new Warehouse
            {
                WarehouseID = 20,
                ManagerID = _testManager.UserID,
                Name = "To Delete",
                Address = "Delete Address",
                LocationLong = 106.0,
                LocationLat = 10.0,
                IsDeleted = false
            };
            await _unitOfWork.Warehouses.AddAsync(warehouse);
            await _unitOfWork.SaveChangesAsync();

            // ACT
            var result = await _warehouseService.DeleteWarehouseAsync(20);

            // ASSERT
            Assert.That(result, Is.True);

            // Verify trong database
            var deletedInDb = await _unitOfWork.Warehouses.GetAsync(w => w.WarehouseID == 20);
            Assert.That(deletedInDb!.IsDeleted, Is.True);
        }

        [Test]
        public async Task DeleteWarehouseAsync_WhenWarehouseNotExists_ShouldReturnFalse()
        {
            // ACT
            var result = await _warehouseService.DeleteWarehouseAsync(999);

            // ASSERT
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task DeleteWarehouseAsync_WhenAlreadyDeleted_ShouldReturnFalse()
        {
            // ARRANGE - Warehouse ?ã xóa r?i
            var warehouse = new Warehouse
            {
                WarehouseID = 21,
                ManagerID = _testManager.UserID,
                Name = "Already Deleted",
                Address = "Deleted Address",
                LocationLong = 106.0,
                LocationLat = 10.0,
                IsDeleted = true  // ?Ã XÓA R?I
            };
            await _unitOfWork.Warehouses.AddAsync(warehouse);
            await _unitOfWork.SaveChangesAsync();

            // ACT - C? xóa l?i
            var result = await _warehouseService.DeleteWarehouseAsync(21);

            // ASSERT - Không xóa ???c n?a
            Assert.That(result, Is.False);
        }

        #endregion
    }
}

# ?? UNIT TESTS SUMMARY - FloodRescue Project

## ? **?Ã T?O**

?ã t?o **4 Unit Test files** m?i:

| File | Location | Tests | Status |
|------|----------|-------|--------|
| `WarehouseServiceTests.cs` | `FloodRescue.Tests\Services\` | 15 tests | ?? C?n fix DTOs |
| `RegisterServiceTests.cs` | `FloodRescue.Tests\Services\` | 13 tests | ? Ready |
| `WarehousesControllerTests.cs` | `FloodRescue.Tests\Controllers\` | 10 tests | ?? C?n restore packages |
| `RegisterControllerTests.cs` | `FloodRescue.Tests\Controllers\` | 8 tests | ?? C?n restore packages |

**T?ng c?ng: 46 Unit Tests**

---

## ?? **PACKAGES ?Ã THÊM**

?ã update `FloodRescue.Tests.csproj`:

```xml
<!-- Packages for Controller testing -->
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.0" />
<PackageReference Include="Moq" Version="4.20.70" />
<!-- For AutoMapper testing -->
<PackageReference Include="AutoMapper" Version="12.0.1" />
<!-- For BCrypt password hashing testing -->
<PackageReference Include="BCrypt.Net-Next" Version="4.0.3" />
```

---

## ?? **C?N S?A**

### **1. Restore NuGet Packages**

```sh
cd FloodRescue.Tests
dotnet restore
```

### **2. Fix DTOs Properties**

**V?n ??:** DTOs thi?u properties c?n thi?t cho testing

**C?n s?a file:** `FloodRescue.Services\DTO\Response\WarehouseResponse\ShowWareHouseResponseDTO.cs`

```csharp
public class ShowWareHouseResponseDTO
{
    public int WarehouseID { get; set; }  // ? THÊM
    public Guid ManagerID { get; set; }  // ? THÊM
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public double LocationLong { get; set; }
    public double LocationLat { get; set; }
    public string ManagedBy { get; set; } = string.Empty;
}
```

**T??ng t? cho:**
- `CreateWarehouseResponseDTO.cs` - Thêm `WarehouseID`, `ManagerID`
- `UpdateWarehouseResponseDTO.cs` - Thêm `WarehouseID`

### **3. Fix ApiResponse Property Names**

Trong test files, thay:
- `apiResponse.IsSuccess` ? `apiResponse.Success`
- Ho?c thêm property `IsSuccess` vào `ApiResponse<T>`:

```csharp
public bool IsSuccess => Success;  // Alias for backward compatibility
public List<string> Errors => Success ? new List<string>() : new List<string> { Message };
```

---

## ?? **TEST COVERAGE**

### **WarehouseService - 15 Tests**

| Category | Tests | Description |
|----------|-------|-------------|
| Create | 2 | T?o warehouse, validate save to DB |
| Read | 4 | Search by ID, get all, filter deleted |
| Update | 2 | Update existing, handle not found |
| Delete | 3 | Soft delete, already deleted, not found |

### **RegisterService - 13 Tests**

| Category | Tests | Description |
|----------|-------|-------------|
| Success | 4 | Valid registration, password hashing, different roles |
| Validation | 5 | Username exists, phone exists, invalid role, admin blocked |
| Edge Cases | 2 | Deleted user reuse username/phone |

### **WarehousesController - 10 Tests**

| Category | Tests | Description |
|----------|-------|-------------|
| GET All | 2 | Return list, empty list |
| GET By ID | 2 | Found, not found |
| POST Create | 1 | Create warehouse |
| PUT Update | 2 | Update success, not found |
| DELETE | 2 | Delete success, not found |
| Integration | 1 | Full CRUD workflow |

### **RegisterController - 8 Tests**

| Category | Tests | Description |
|----------|-------|-------------|
| Success | 2 | Valid register, different roles |
| Errors | 4 | Username/phone exists, invalid role, admin blocked |
| Format | 2 | ApiResponse format validation |

---

## ?? **CÁCH CH?Y TESTS**

### **Sau khi restore packages và fix DTOs:**

```sh
# Restore packages
cd FloodRescue.Tests
dotnet restore

# Ch?y t?t c? tests
dotnet test

# Ch?y tests c?a 1 class
dotnet test --filter "FullyQualifiedName~WarehouseServiceTests"
dotnet test --filter "FullyQualifiedName~RegisterServiceTests"
dotnet test --filter "FullyQualifiedName~WarehousesControllerTests"
dotnet test --filter "FullyQualifiedName~RegisterControllerTests"

# Ch?y 1 test c? th?
dotnet test --filter "FullyQualifiedName~WarehouseServiceTests.CreateWarehouseAsync_WhenValidRequest_ShouldCreateWarehouse"
```

### **Trong Visual Studio:**

1. M? **Test Explorer** (Test ? Test Explorer)
2. Click **Run All Tests**
3. Xem k?t qu? green/red

---

## ?? **TESTING STRATEGY**

### **Service Tests (Integration v?i Database)**
- ? Dùng InMemory Database
- ? Test business logic th?c t?
- ? Verify database operations
- ? Test edge cases và validation

### **Controller Tests (Unit v?i Mocking)**
- ? Dùng Moq ?? mock Services
- ? Test API responses (200, 400, 404)
- ? Verify service calls
- ? Test ApiResponse format

---

## ?? **EXAMPLE TEST CASES**

### **WarehouseServiceTests:**
```csharp
[Test]
public async Task CreateWarehouseAsync_WhenValidRequest_ShouldCreateWarehouse()
{
    // T?o warehouse m?i v?i d? li?u h?p l?
    // Verify response DTO ?úng
    // Verify warehouse ???c save vào database
}

[Test]
public async Task DeleteWarehouseAsync_WhenWarehouseExists_ShouldMarkAsDeleted()
{
    // Soft delete warehouse
    // Verify IsDeleted = true trong database
}
```

### **RegisterServiceTests:**
```csharp
[Test]
public async Task RegisterAsync_ShouldHashPassword()
{
    // ??ng ký user m?i
    // Verify password ???c hash b?ng BCrypt
    // Verify password trong DB != plain text
}

[Test]
public async Task RegisterAsync_WhenUsernameExists_ShouldReturnError()
{
    // C? ??ng ký v?i username ?ã t?n t?i
    // Verify tr? v? error message
}
```

### **WarehousesControllerTests:**
```csharp
[Test]
public async Task GetWarehouses_WhenHasWarehouses_ShouldReturnOkWithList()
{
    // Mock service tr? v? list warehouses
    // Verify controller tr? v? ApiResponse v?i Success = true
    // Verify service.GetAllWarehousesAsync() ???c g?i ?úng 1 l?n
}
```

### **RegisterControllerTests:**
```csharp
[Test]
public async Task Register_WhenValidRequest_ShouldReturnOkWith201()
{
    // Mock service tr? v? success
    // Verify ApiResponse: Success = true, StatusCode = 201
    // Verify Message = "Register successfully"
}
```

---

## ?? **NEXT STEPS**

1. **Restore packages:** `dotnet restore`
2. **Fix DTOs:** Thêm missing properties
3. **Fix ApiResponse:** Thêm `IsSuccess` property ho?c s?a tests
4. **Run tests:** `dotnet test`
5. **Review coverage:** Aim for 80%+ coverage

---

## ? **BUILD STATUS**

- ?? **Current:** Build failed (missing packages & DTO properties)
- ?? **Target:** All tests green after fixes
- ?? **Coverage:** 46 tests covering Services & Controllers

---

## ?? **REFERENCES**

- NUnit Documentation: https://docs.nunit.org/
- Moq Documentation: https://github.com/moq/moq4
- .NET Testing Best Practices: https://learn.microsoft.com/en-us/dotnet/core/testing/


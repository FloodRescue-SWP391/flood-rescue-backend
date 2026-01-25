# ?? SUMMARY: MIGRATION FROM GetByIdAsync TO GetAsync WITH INCLUDES

## ? **?Ã THAY ??I**

### **1. IBaseRepository.cs**
- **Thêm 2 overload methods m?i:**
  - `GetAsync(filter, params includes[])` - L?y 1 entity v?i navigation properties
  - `GetAllAsync(filter, params includes[])` - L?y danh sách v?i navigation properties

### **2. BaseRepository.cs**
- **Fix bug:** Thêm `await` cho `query.FirstOrDefaultAsync()` 
- **Remove:** `using Microsoft.VisualBasic` không c?n thi?t
- **Implement:** 2 overload methods v?i Include support

### **3. TokenService.cs**
- **TR??C:**
  ```csharp
  var user = await _unitOfWork.Users.GetByIdAsync(userID);
  // user.Role = null ? CRASH!
  ```
- **SAU:**
  ```csharp
  var user = await _unitOfWork.Users.GetAsync(
      u => u.UserID == userID && !u.IsDeleted,
      u => u.Role!  // Include Role
  );
  // user.Role ?ã ???c load ?
  ```

### **4. WarehouseService.cs**
- **DeleteWarehouseAsync():** 
  - `GetByIdAsync(id)` ? `GetAsync(w => w.WarehouseID == id)`
  
- **SearchWarehouseAsync():**
  - `GetByIdAsync(id)` ? `GetAsync(w => w.WarehouseID == id && !w.IsDeleted, w => w.Manager!)`
  
- **GetAllWarehousesAsync():**
  - `GetAllAsync()` ? `GetAllAsync(w => !w.IsDeleted, w => w.Manager!)`
  
- **UpdateWarehouseAsync():**
  - `GetByIdAsync(id)` ? `GetAsync(w => w.WarehouseID == id)`

---

## ?? **L?I ÍCH**

| Tr??c | Sau |
|-------|-----|
| ? Navigation Properties = null | ? Navigation Properties ???c load |
| ? Ph?i check null nhi?u | ? Ít l?i NullReferenceException |
| ? N+1 query problem | ? Eager Loading - 1 query duy nh?t |
| ? Crash khi production | ? Stable, d? li?u ??y ?? |

---

## ?? **CÁCH DÙNG M?I**

### **L?y entity v?i 1 navigation property:**
```csharp
var user = await _unitOfWork.Users.GetAsync(
    u => u.UserID == userId,
    u => u.Role!
);
```

### **L?y entity v?i NHI?U navigation properties:**
```csharp
var warehouse = await _unitOfWork.Warehouses.GetAsync(
    w => w.WarehouseID == id,
    w => w.Manager!,
    w => w.Inventories!
);
```

### **L?y danh sách v?i navigation property:**
```csharp
var warehouses = await _unitOfWork.Warehouses.GetAllAsync(
    w => !w.IsDeleted,
    w => w.Manager!
);
```

---

## ?? **L?U Ý**

1. **`GetByIdAsync()` V?N T?N T?I** - dùng khi không c?n navigation properties
2. **Luôn check `!IsDeleted`** trong filter ?? tránh l?y data ?ã xóa
3. **Dùng `!` (null-forgiving operator)** khi ch?c ch?n navigation property không null

---

## ?? **NEXT STEPS**

N?u có thêm Services khác c?n s?a:
1. Tìm t?t c? `GetByIdAsync()` 
2. Xác ??nh entity có navigation property nào
3. Thay b?ng `GetAsync(filter, includes...)`

---

## ? **BUILD STATUS:** SUCCESS

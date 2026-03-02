using Azure.Core;
using FloodRescue.Repositories.Interface;
using FloodRescue.Services.DTO.Request.InventoryRequest;
using FloodRescue.Services.DTO.Response.InventoryResponse;
using FloodRescue.Services.Interface.Inventory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InventoryEntity = FloodRescue.Repositories.Entites.Inventory;
using ReliefItemEntity = FloodRescue.Repositories.Entites.ReliefItem;
using WarehouseEntity = FloodRescue.Repositories.Entites.Warehouse;

namespace FloodRescue.Services.Implements.Inventory
{
    public class InventoryService : IInventoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<InventoryService> _logger;

        public InventoryService(IUnitOfWork unitOfWork, ILogger<InventoryService> logger) 
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<(List<InventoryItemResponseDTO>? Data, string? ErrorMessage)> GetInventoryByWarehouseAsync(int warehouseId)
        {
            _logger.LogInformation("[InventoryService] GetInventoryByWarehouse called. WarehouseID: {WarehouseID}", warehouseId);
            // 1. Kiểm tra Warehouse có tồn tại không
            WarehouseEntity? warehouse = await _unitOfWork.Warehouses.GetAsync(w => w.WarehouseID == warehouseId && !w.IsDeleted);

            if (warehouse == null)
            {
                _logger.LogWarning("[InventoryService - Sql Server] Warehouse {WarehouseID} not found or deleted.", warehouseId);
                return (null, "Warehouse does not exist or has been deleted.");
            }

            // 2. Lấy danh sách inventory với Include ReliefItem
            List<InventoryEntity> inventories = await _unitOfWork.Inventories.GetAllAsync(
               filter: inv => inv.WarehouseID == warehouseId,
               includes: inv => inv.ReliefItem!  // Include để lấy ReliefItemName và Unit
           );
            // 3. Lọc bỏ những item đã bị xóa (IsDeleted)
            var validInventories = inventories
                .Where(inv => inv.ReliefItem != null && !inv.ReliefItem.IsDeleted)
                .ToList();

            if (!validInventories.Any())
            {
                _logger.LogInformation("[InventoryService] No inventory found for WarehouseID: {WarehouseID}", warehouseId);
                return (new List<InventoryItemResponseDTO>(), null);
            }

            // 4. 
            List<InventoryItemResponseDTO> result = validInventories.Select(inv => new InventoryItemResponseDTO
            {
                InventoryID = inv.InventoryID,
                ReliefItemID = inv.ReliefItemID,
                ReliefItemName = inv.ReliefItem?.ReliefItemName ?? string.Empty,
                Unit = inv.ReliefItem?.Unit ?? string.Empty,
                Quantity = inv.Quantity,
                LastUpdated = inv.LastUpdated
            }).ToList();

            _logger.LogInformation("[InventoryService] Found {Count} inventory items for WarehouseID: {WarehouseID}",
                result.Count, warehouseId);

            return (result, null);
        }

        public async Task<(ReceiveInventoryResponseDTO? Data, string? ErrorMessage)> ReceiveInventoryAsync(ReceiveInventoryRequestDTO request)
        {
            _logger.LogInformation("[InventoryService] ReceiveInventory called. WarehouseID: {WarehouseID}, ItemCount: {Count}",
                request.WarehouseID, request.Items.Count);
            // 1. Kiểm tra Warehouse có tồn tại không
            WarehouseEntity? warehouse = await _unitOfWork.Warehouses.GetAsync(w => w.WarehouseID == request.WarehouseID && !w.IsDeleted);

            if (warehouse == null)
            {
                _logger.LogWarning("[InventoryService - Sql Server] Warehouse {WarehouseID} not found or deleted.", request.WarehouseID);
                return (null, "Warehouse does not exist or has been deleted.");
            }
            // 2. Kiểm tra danh sách items
            if (request.Items == null || !request.Items.Any())
            {
                _logger.LogWarning("[InventoryService] Items list is empty.");
                return (null, "The list of Items must not be left blank.");
            }

            // Kiểm tra quantity > 0
            var invalidItems = request.Items.Where(i => i.Quantity <= 0).ToList();
            if (invalidItems.Any())
            {
                _logger.LogWarning("[InventoryService] Invalid quantity found. ReliefItemIDs: {Ids}",
                    string.Join(", ", invalidItems.Select(i => i.ReliefItemID)));
                return (null, "The quantity must be greater than 0.");
            }

            // 3. Kiểm tra ReliefItems tồn tại
            var requestedItemIds = request.Items.Select(i => i.ReliefItemID).Distinct().ToList();

            // Query tất cả ReliefItems trong 1 lần
            List<ReliefItemEntity> existingReliefItems = await _unitOfWork.ReliefItems.GetAllAsync(
                r => requestedItemIds.Contains(r.ReliefItemID) && !r.IsDeleted
            );

            var existingItemIds = existingReliefItems.Select(r => r.ReliefItemID).ToList();
            var notFoundItemIds = requestedItemIds.Except(existingItemIds).ToList();

            if (notFoundItemIds.Any())
            {
                _logger.LogWarning("[InventoryService - Sql Server] ReliefItems not found: {Ids}",
                    string.Join(", ", notFoundItemIds));
                return (null, $"The items do not exist: {string.Join(", ", notFoundItemIds)}");
            }

            // Tạo dictionary để lookup tên item
            var reliefItemDict = existingReliefItems.ToDictionary(r => r.ReliefItemID, r => r.ReliefItemName);

            // 4. Bắt đầu xử lý 
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                List<InventoryItemResultDTO> itemResults = new List<InventoryItemResultDTO>();
                DateTime processedAt = DateTime.UtcNow;

                // Query tất cả inventory hiện có của warehouse này (tối ưu - 1 query)
                List<InventoryEntity> existingInventories = await _unitOfWork.Inventories.GetAllAsync(
                    inv => inv.WarehouseID == request.WarehouseID && requestedItemIds.Contains(inv.ReliefItemID)
                );

                // Tạo dictionary để lookup nhanh
                var inventoryDict = existingInventories.ToDictionary(
                    inv => inv.ReliefItemID,
                    inv => inv
                );
                // 5. LOOP - Xử lý từng item

                foreach (var item in request.Items)
                {
                    bool isNewRecord = false;
                    int newTotalQuantity = 0;

                    if (inventoryDict.TryGetValue(item.ReliefItemID, out InventoryEntity? existingInventory))
                    {
                        // ═══ CASE 1: Inventory ĐÃ TỒN TẠI → Cộng dồn ═══
                        existingInventory.Quantity += item.Quantity;
                        existingInventory.LastUpdated = processedAt;
                        _unitOfWork.Inventories.Update(existingInventory);

                        newTotalQuantity = existingInventory.Quantity;
                        isNewRecord = false;

                        _logger.LogInformation(
                            "[InventoryService] Updated inventory. WarehouseID: {WH}, ReliefItemID: {Item}, Added: {Added}, NewTotal: {Total}",
                            request.WarehouseID, item.ReliefItemID, item.Quantity, newTotalQuantity);
                    }
                    else
                    {
                        // ═══ CASE 2: Inventory CHƯA TỒN TẠI → Tạo mới ═══
                        InventoryEntity newInventory = new InventoryEntity
                        {
                            InventoryID = Guid.NewGuid(),
                            WarehouseID = request.WarehouseID,
                            ReliefItemID = item.ReliefItemID,
                            Quantity = item.Quantity,
                            LastUpdated = processedAt
                        };

                        await _unitOfWork.Inventories.AddAsync(newInventory);

                        newTotalQuantity = item.Quantity;
                        isNewRecord = true;

                        _logger.LogInformation(
                            "[InventoryService] Created new inventory. WarehouseID: {WH}, ReliefItemID: {Item}, Quantity: {Qty}",
                            request.WarehouseID, item.ReliefItemID, item.Quantity);
                    }

                    // Thêm vào kết quả
                    itemResults.Add(new InventoryItemResultDTO
                    {
                        ReliefItemID = item.ReliefItemID,
                        ReliefItemName = reliefItemDict.GetValueOrDefault(item.ReliefItemID, "Unknown"),
                        QuantityAdded = item.Quantity,
                        NewTotalQuantity = newTotalQuantity,
                        IsNewRecord = isNewRecord
                    });
                }

                // 6. SAVE & COMMIT
                int saveResult = await _unitOfWork.SaveChangesAsync();

                if (saveResult <= 0)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    _logger.LogError("[InventoryService - Error] SaveChanges returned 0 rows. WarehouseID: {WH}", request.WarehouseID);
                    return (null, "Lưu dữ liệu thất bại");
                }

                await _unitOfWork.CommitTransactionAsync();


                // 7. TẠO RESPONSE
                ReceiveInventoryResponseDTO response = new ReceiveInventoryResponseDTO
                {
                    WarehouseID = warehouse.WarehouseID,
                    WarehouseName = warehouse.Name,
                    TotalItemsProcessed = itemResults.Count,
                    ProcessedAt = processedAt,
                    ItemResults = itemResults,
                    Message = $"Nhập kho thành công {itemResults.Count} loại vật tư vào kho {warehouse.Name}"
                };

                _logger.LogInformation("[InventoryService] ReceiveInventory completed. WarehouseID: {WH}, ItemsProcessed: {Count}",
                    request.WarehouseID, itemResults.Count);

                return (response, null);
            }
            catch (Exception ex) 
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "[InventoryService - Error] ReceiveInventory failed. Transaction rolled back. WarehouseID: {WH}",
                    request.WarehouseID);
                throw;
            }
        }
    }
}

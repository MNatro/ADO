using ADO.Data.Models;

namespace ADO.Data.Repositories
{
    public interface IOrderRepository
    {
        // CRUD Operations
        Task<Order> CreateAsync(Order order);
        Task<Order?> GetByIdAsync(int id);
        Task<Order> UpdateAsync(Order order);
        Task<bool> DeleteAsync(int id);
        
        // Filter operations with stored procedure
        Task<IEnumerable<Order>> GetFilteredOrdersAsync(int? month = null, int? year = null, OrderStatus? status = null, int? productId = null);
        
        // Bulk delete with transaction
        Task<int> BulkDeleteOrdersAsync(int? month = null, int? year = null, OrderStatus? status = null, int? productId = null);
    }
}

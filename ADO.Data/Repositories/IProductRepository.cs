using ADO.Data.Models;

namespace ADO.Data.Repositories
{
    public interface IProductRepository
    {
        // CRUD Operations
        Task<Product> CreateAsync(Product product);
        Task<Product?> GetByIdAsync(int id);
        Task<Product> UpdateAsync(Product product);
        Task<bool> DeleteAsync(int id);
        
        // Fetch all products
        Task<IEnumerable<Product>> GetAllAsync();
    }
}

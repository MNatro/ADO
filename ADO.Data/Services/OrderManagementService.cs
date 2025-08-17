using ADO.Data.Models;
using ADO.Data.Repositories;

namespace ADO.Data.Services
{
    public interface IOrderManagementService
    {
        // Product operations
        Task<Product> CreateProductAsync(Product product);
        Task<Product?> GetProductByIdAsync(int id);
        Task<Product> UpdateProductAsync(Product product);
        Task<bool> DeleteProductAsync(int id);
        Task<IEnumerable<Product>> GetAllProductsAsync();

        // Order operations
        Task<Order> CreateOrderAsync(Order order);
        Task<Order?> GetOrderByIdAsync(int id);
        Task<Order> UpdateOrderAsync(Order order);
        Task<bool> DeleteOrderAsync(int id);
        Task<IEnumerable<Order>> GetFilteredOrdersAsync(int? month = null, int? year = null, OrderStatus? status = null, int? productId = null);
        Task<int> BulkDeleteOrdersAsync(int? month = null, int? year = null, OrderStatus? status = null, int? productId = null);
    }

    public class OrderManagementService : IOrderManagementService
    {
        private readonly IProductRepository _productRepository;
        private readonly IOrderRepository _orderRepository;

        public OrderManagementService(IProductRepository productRepository, IOrderRepository orderRepository)
        {
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        }

        // Product operations
        public Task<Product> CreateProductAsync(Product product) => _productRepository.CreateAsync(product);
        public Task<Product?> GetProductByIdAsync(int id) => _productRepository.GetByIdAsync(id);
        public Task<Product> UpdateProductAsync(Product product) => _productRepository.UpdateAsync(product);
        public Task<bool> DeleteProductAsync(int id) => _productRepository.DeleteAsync(id);
        public Task<IEnumerable<Product>> GetAllProductsAsync() => _productRepository.GetAllAsync();

        // Order operations
        public Task<Order> CreateOrderAsync(Order order) => _orderRepository.CreateAsync(order);
        public Task<Order?> GetOrderByIdAsync(int id) => _orderRepository.GetByIdAsync(id);
        public Task<Order> UpdateOrderAsync(Order order) => _orderRepository.UpdateAsync(order);
        public Task<bool> DeleteOrderAsync(int id) => _orderRepository.DeleteAsync(id);
        public Task<IEnumerable<Order>> GetFilteredOrdersAsync(int? month = null, int? year = null, OrderStatus? status = null, int? productId = null)
            => _orderRepository.GetFilteredOrdersAsync(month, year, status, productId);
        public Task<int> BulkDeleteOrdersAsync(int? month = null, int? year = null, OrderStatus? status = null, int? productId = null)
            => _orderRepository.BulkDeleteOrdersAsync(month, year, status, productId);
    }
}

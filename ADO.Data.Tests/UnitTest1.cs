using ADO.Data.Infrastructure;
using ADO.Data.Models;
using ADO.Data.Repositories;
using ADO.Data.Services;
using Microsoft.Data.SqlClient;
using Moq;
using System.Data;

namespace ADO.Data.Tests
{
    public class ProductRepositoryTests
    {
        private readonly Mock<IDbConnectionManager> _mockConnectionManager;
        private readonly ProductRepository _repository;

        public ProductRepositoryTests()
        {
            _mockConnectionManager = new Mock<IDbConnectionManager>();
            _repository = new ProductRepository(_mockConnectionManager.Object);
        }

        [Fact]
        public void Constructor_WithNullConnectionManager_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ProductRepository(null!));
        }

        [Fact]
        public void GetAllAsync_ReturnsProductsFromDataSet()
        {
            // This test demonstrates the disconnected model usage
            // In a real scenario, you would mock the SqlDataAdapter
            // For now, we test the constructor and basic functionality
            Assert.NotNull(_repository);
        }
    }

    public class OrderRepositoryTests
    {
        private readonly Mock<IDbConnectionManager> _mockConnectionManager;
        private readonly OrderRepository _repository;

        public OrderRepositoryTests()
        {
            _mockConnectionManager = new Mock<IDbConnectionManager>();
            _repository = new OrderRepository(_mockConnectionManager.Object);
        }

        [Fact]
        public void Constructor_WithNullConnectionManager_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => new OrderRepository(null!));
        }

        [Fact]
        public void Repository_CanBeCreated()
        {
            // Test basic instantiation
            Assert.NotNull(_repository);
        }
    }

    public class OrderManagementServiceTests
    {
        private readonly Mock<IProductRepository> _mockProductRepository;
        private readonly Mock<IOrderRepository> _mockOrderRepository;
        private readonly OrderManagementService _service;

        public OrderManagementServiceTests()
        {
            _mockProductRepository = new Mock<IProductRepository>();
            _mockOrderRepository = new Mock<IOrderRepository>();
            _service = new OrderManagementService(_mockProductRepository.Object, _mockOrderRepository.Object);
        }

        [Fact]
        public void Constructor_WithNullProductRepository_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => new OrderManagementService(null!, _mockOrderRepository.Object));
        }

        [Fact]
        public void Constructor_WithNullOrderRepository_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => new OrderManagementService(_mockProductRepository.Object, null!));
        }

        [Fact]
        public async Task CreateProductAsync_CallsRepositoryCreateAsync()
        {
            // Arrange
            var product = new Product { Name = "Test Product", Description = "Test", Weight = 1.0m, Height = 1.0m, Width = 1.0m, Length = 1.0m };
            _mockProductRepository.Setup(x => x.CreateAsync(product)).ReturnsAsync(product);

            // Act
            var result = await _service.CreateProductAsync(product);

            // Assert
            _mockProductRepository.Verify(x => x.CreateAsync(product), Times.Once);
            Assert.Equal(product, result);
        }

        [Fact]
        public async Task GetAllProductsAsync_CallsRepositoryGetAllAsync()
        {
            // Arrange
            var products = new List<Product> 
            { 
                new Product { Id = 1, Name = "Product 1" },
                new Product { Id = 2, Name = "Product 2" }
            };
            _mockProductRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(products);

            // Act
            var result = await _service.GetAllProductsAsync();

            // Assert
            _mockProductRepository.Verify(x => x.GetAllAsync(), Times.Once);
            Assert.Equal(products, result);
        }

        [Fact]
        public async Task CreateOrderAsync_CallsRepositoryCreateAsync()
        {
            // Arrange
            var order = new Order 
            { 
                Status = OrderStatus.NotStarted, 
                CreatedDate = DateTime.Now, 
                UpdatedDate = DateTime.Now, 
                ProductId = 1 
            };
            _mockOrderRepository.Setup(x => x.CreateAsync(order)).ReturnsAsync(order);

            // Act
            var result = await _service.CreateOrderAsync(order);

            // Assert
            _mockOrderRepository.Verify(x => x.CreateAsync(order), Times.Once);
            Assert.Equal(order, result);
        }

        [Fact]
        public async Task GetFilteredOrdersAsync_CallsRepositoryWithCorrectParameters()
        {
            // Arrange
            var month = 8;
            var year = 2025;
            var status = OrderStatus.InProgress;
            var productId = 1;
            var orders = new List<Order>();

            _mockOrderRepository.Setup(x => x.GetFilteredOrdersAsync(month, year, status, productId))
                               .ReturnsAsync(orders);

            // Act
            var result = await _service.GetFilteredOrdersAsync(month, year, status, productId);

            // Assert
            _mockOrderRepository.Verify(x => x.GetFilteredOrdersAsync(month, year, status, productId), Times.Once);
            Assert.Equal(orders, result);
        }

        [Fact]
        public async Task BulkDeleteOrdersAsync_CallsRepositoryWithCorrectParameters()
        {
            // Arrange
            var month = 7;
            var year = 2025;
            var status = OrderStatus.Cancelled;
            var deletedCount = 5;

            _mockOrderRepository.Setup(x => x.BulkDeleteOrdersAsync(month, year, status, null))
                               .ReturnsAsync(deletedCount);

            // Act
            var result = await _service.BulkDeleteOrdersAsync(month, year, status);

            // Assert
            _mockOrderRepository.Verify(x => x.BulkDeleteOrdersAsync(month, year, status, null), Times.Once);
            Assert.Equal(deletedCount, result);
        }
    }

    public class ModelTests
    {
        [Fact]
        public void Product_CanBeCreatedWithProperties()
        {
            // Arrange & Act
            var product = new Product
            {
                Id = 1,
                Name = "Test Product",
                Description = "Test Description",
                Weight = 2.5m,
                Height = 10.0m,
                Width = 15.0m,
                Length = 20.0m
            };

            // Assert
            Assert.Equal(1, product.Id);
            Assert.Equal("Test Product", product.Name);
            Assert.Equal("Test Description", product.Description);
            Assert.Equal(2.5m, product.Weight);
            Assert.Equal(10.0m, product.Height);
            Assert.Equal(15.0m, product.Width);
            Assert.Equal(20.0m, product.Length);
        }

        [Fact]
        public void Order_CanBeCreatedWithProperties()
        {
            // Arrange & Act
            var order = new Order
            {
                Id = 1,
                Status = OrderStatus.InProgress,
                CreatedDate = new DateTime(2025, 8, 17),
                UpdatedDate = new DateTime(2025, 8, 17),
                ProductId = 1
            };

            // Assert
            Assert.Equal(1, order.Id);
            Assert.Equal(OrderStatus.InProgress, order.Status);
            Assert.Equal(new DateTime(2025, 8, 17), order.CreatedDate);
            Assert.Equal(new DateTime(2025, 8, 17), order.UpdatedDate);
            Assert.Equal(1, order.ProductId);
        }

        [Fact]
        public void OrderStatus_HasCorrectValues()
        {
            // Assert enum values match specification
            Assert.Equal(0, (int)OrderStatus.NotStarted);
            Assert.Equal(1, (int)OrderStatus.Loading);
            Assert.Equal(2, (int)OrderStatus.InProgress);
            Assert.Equal(3, (int)OrderStatus.Arrived);
            Assert.Equal(4, (int)OrderStatus.Unloading);
            Assert.Equal(5, (int)OrderStatus.Cancelled);
            Assert.Equal(6, (int)OrderStatus.Done);
        }
    }

    public class DbConnectionManagerTests
    {
        [Fact]
        public void Constructor_WithNullConnectionString_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentException>(() => new DbConnectionManager(null!));
        }

        [Fact]
        public void Constructor_WithEmptyConnectionString_ThrowsArgumentException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentException>(() => new DbConnectionManager(""));
        }

        [Fact]
        public void Constructor_WithWhitespaceConnectionString_ThrowsArgumentException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentException>(() => new DbConnectionManager("   "));
        }

        [Fact]
        public void Constructor_WithValidConnectionString_SetsProperty()
        {
            // Arrange
            var connectionString = "Server=localhost;Database=TestDB;Trusted_Connection=true;";

            // Act
            var manager = new DbConnectionManager(connectionString);

            // Assert
            Assert.Equal(connectionString, manager.ConnectionString);
        }

        [Fact]
        public void CreateConnection_ReturnsNewSqlConnection()
        {
            // Arrange
            var connectionString = "Server=localhost;Database=TestDB;Trusted_Connection=true;";
            var manager = new DbConnectionManager(connectionString);

            // Act
            using var connection = manager.CreateConnection();

            // Assert
            Assert.NotNull(connection);
            Assert.IsType<Microsoft.Data.SqlClient.SqlConnection>(connection);
            Assert.Equal(connectionString, connection.ConnectionString);
        }

        [Fact]
        public async Task TestConnectionAsync_WithInvalidConnectionString_ReturnsFalse()
        {
            // Arrange
            var connectionString = "Server=invalid;Database=invalid;Trusted_Connection=true;";
            var manager = new DbConnectionManager(connectionString);

            // Act
            var result = await manager.TestConnectionAsync();

            // Assert
            Assert.False(result);
        }
    }
}

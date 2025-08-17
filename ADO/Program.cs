using ADO.Data.Configuration;
using ADO.Data.Infrastructure;
using ADO.Data.Models;
using ADO.Data.Repositories;
using ADO.Data.Services;

namespace ADO
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("ADO.NET Order Management System Demo");
            Console.WriteLine("=====================================");

            try
            {
                // Setup configuration and dependencies
                var config = new ConnectionConfiguration();
                var connectionManager = new DbConnectionManager(config.GetConnectionString());
                
                // Test database connection
                Console.WriteLine("Testing database connection...");
                var connectionTest = await connectionManager.TestConnectionAsync();
                if (!connectionTest)
                {
                    Console.WriteLine("❌ Database connection failed!");
                    Console.WriteLine("Please ensure the database is created and accessible.");
                    return;
                }
                Console.WriteLine("✅ Database connection successful!");

                var productRepository = new ProductRepository(connectionManager);
                var orderRepository = new OrderRepository(connectionManager);
                var orderManagementService = new OrderManagementService(productRepository, orderRepository);

                await DemonstrateProductOperations(orderManagementService);
                await DemonstrateOrderOperations(orderManagementService);
                await DemonstrateFilteringAndBulkOperations(orderManagementService);
                
                Console.WriteLine("\n🎉 Demo completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.WriteLine("\n📋 Troubleshooting steps:");
                Console.WriteLine("1. Execute the SQL script in ADO.Data/Scripts/DatabaseSetup.sql");
                Console.WriteLine("2. Ensure SQL Server LocalDB is installed and running");
                Console.WriteLine("3. Check that the connection string is correct");
                Console.WriteLine("4. Verify database permissions");
                
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"\n🔍 Inner exception: {ex.InnerException.Message}");
                }
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        static async Task DemonstrateProductOperations(IOrderManagementService service)
        {
            Console.WriteLine("\n=== Product CRUD Operations Demo ===");

            // Create a new product
            var newProduct = new Product
            {
                Name = "Gaming Chair",
                Description = "Ergonomic gaming chair with RGB lighting",
                Weight = 15.5m,
                Height = 120.0m,
                Width = 65.0m,
                Length = 70.0m
            };

            Console.WriteLine("Creating new product...");
            var createdProduct = await service.CreateProductAsync(newProduct);
            Console.WriteLine($"Created product with ID: {createdProduct.Id}");

            // Read the product
            Console.WriteLine("\nRetrieving product...");
            var retrievedProduct = await service.GetProductByIdAsync(createdProduct.Id);
            if (retrievedProduct != null)
            {
                Console.WriteLine($"Retrieved: {retrievedProduct.Name} - {retrievedProduct.Description}");
            }

            // Update the product
            Console.WriteLine("\nUpdating product...");
            retrievedProduct!.Description = "Updated: Premium ergonomic gaming chair with RGB lighting";
            await service.UpdateProductAsync(retrievedProduct);
            Console.WriteLine("Product updated successfully!");

            // Get all products (demonstrates disconnected model)
            Console.WriteLine("\nFetching all products (Disconnected Model)...");
            var allProducts = await service.GetAllProductsAsync();
            Console.WriteLine($"Total products: {allProducts.Count()}");
            foreach (var product in allProducts.Take(3)) // Show first 3
            {
                Console.WriteLine($"- {product.Name} (ID: {product.Id})");
            }
        }

        static async Task DemonstrateOrderOperations(IOrderManagementService service)
        {
            Console.WriteLine("\n=== Order CRUD Operations Demo ===");

            // Create a new order
            var newOrder = new Order
            {
                Status = OrderStatus.NotStarted,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now,
                ProductId = 1 // Assuming product with ID 1 exists
            };

            Console.WriteLine("Creating new order...");
            var createdOrder = await service.CreateOrderAsync(newOrder);
            Console.WriteLine($"Created order with ID: {createdOrder.Id}");

            // Read the order with product information
            Console.WriteLine("\nRetrieving order with product details...");
            var retrievedOrder = await service.GetOrderByIdAsync(createdOrder.Id);
            if (retrievedOrder != null)
            {
                Console.WriteLine($"Order ID: {retrievedOrder.Id}, Status: {retrievedOrder.Status}");
                if (retrievedOrder.Product != null)
                {
                    Console.WriteLine($"Product: {retrievedOrder.Product.Name}");
                }
            }

            // Update the order status
            Console.WriteLine("\nUpdating order status...");
            retrievedOrder!.Status = OrderStatus.Loading;
            retrievedOrder.UpdatedDate = DateTime.Now;
            await service.UpdateOrderAsync(retrievedOrder);
            Console.WriteLine("Order status updated to Loading!");
        }

        static async Task DemonstrateFilteringAndBulkOperations(IOrderManagementService service)
        {
            Console.WriteLine("\n=== Filtering and Bulk Operations Demo ===");

            // Demonstrate stored procedure filtering
            Console.WriteLine("\nFiltering orders by month (August 2025)...");
            var augustOrders = await service.GetFilteredOrdersAsync(month: 8, year: 2025);
            Console.WriteLine($"Found {augustOrders.Count()} orders in August 2025");

            Console.WriteLine("\nFiltering orders by status (NotStarted)...");
            var notStartedOrders = await service.GetFilteredOrdersAsync(status: OrderStatus.NotStarted);
            Console.WriteLine($"Found {notStartedOrders.Count()} orders with NotStarted status");

            foreach (var order in notStartedOrders.Take(3))
            {
                Console.WriteLine($"- Order {order.Id}: {order.Status} (Created: {order.CreatedDate:yyyy-MM-dd})");
            }

            // Demonstrate bulk delete with transaction
            Console.WriteLine("\nBulk delete demo - Deleting cancelled orders from July 2025...");
            var deletedCount = await service.BulkDeleteOrdersAsync(
                month: 7, 
                year: 2025, 
                status: OrderStatus.Cancelled
            );
            Console.WriteLine($"Bulk deleted {deletedCount} cancelled orders from July 2025");

            // Show remaining orders
            Console.WriteLine("\nRemaining orders after bulk delete...");
            var remainingOrders = await service.GetFilteredOrdersAsync(year: 2025);
            Console.WriteLine($"Total remaining orders in 2025: {remainingOrders.Count()}");
        }
    }
}

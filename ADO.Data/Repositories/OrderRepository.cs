using ADO.Data.Infrastructure;
using ADO.Data.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ADO.Data.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly IDbConnectionManager _connectionManager;

        public OrderRepository(IDbConnectionManager connectionManager)
        {
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        }

        // Connected Model - Create with validation
        public async Task<Order> CreateAsync(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            ValidateOrder(order);

            const string sql = @"
                INSERT INTO [Order] (Status, CreatedDate, UpdatedDate, ProductId)
                OUTPUT INSERTED.Id
                VALUES (@Status, @CreatedDate, @UpdatedDate, @ProductId)";

            try
            {
                using var connection = _connectionManager.CreateConnection();
                await connection.OpenAsync();

                using var command = new SqlCommand(sql, connection);
                command.Parameters.Add(new SqlParameter("@Status", SqlDbType.Int) { Value = (int)order.Status });
                command.Parameters.Add(new SqlParameter("@CreatedDate", SqlDbType.DateTime2) { Value = order.CreatedDate });
                command.Parameters.Add(new SqlParameter("@UpdatedDate", SqlDbType.DateTime2) { Value = order.UpdatedDate });
                command.Parameters.Add(new SqlParameter("@ProductId", SqlDbType.Int) { Value = order.ProductId });

                var result = await command.ExecuteScalarAsync();
                if (result == null)
                    throw new InvalidOperationException("Failed to create order: no ID returned");

                order.Id = (int)result;
                return order;
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException($"Database error while creating order: {ex.Message}", ex);
            }
        }

        // Connected Model - Read with JOIN
        public async Task<Order?> GetByIdAsync(int id)
        {
            const string sql = @"
                SELECT o.Id, o.Status, o.CreatedDate, o.UpdatedDate, o.ProductId,
                       p.Name, p.Description, p.Weight, p.Height, p.Width, p.Length
                FROM [Order] o
                INNER JOIN Product p ON o.ProductId = p.Id
                WHERE o.Id = @Id";

            using var connection = _connectionManager.CreateConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapOrderFromReader(reader);
            }

            return null;
        }

        // Connected Model - Update
        public async Task<Order> UpdateAsync(Order order)
        {
            const string sql = @"
                UPDATE [Order] 
                SET Status = @Status, UpdatedDate = @UpdatedDate, ProductId = @ProductId
                WHERE Id = @Id";

            using var connection = _connectionManager.CreateConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", order.Id);
            command.Parameters.AddWithValue("@Status", (int)order.Status);
            command.Parameters.AddWithValue("@UpdatedDate", order.UpdatedDate);
            command.Parameters.AddWithValue("@ProductId", order.ProductId);

            await command.ExecuteNonQueryAsync();
            return order;
        }

        // Connected Model - Delete
        public async Task<bool> DeleteAsync(int id)
        {
            const string sql = "DELETE FROM [Order] WHERE Id = @Id";

            using var connection = _connectionManager.CreateConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        // Stored Procedure - Filtered Orders
        public async Task<IEnumerable<Order>> GetFilteredOrdersAsync(int? month = null, int? year = null, OrderStatus? status = null, int? productId = null)
        {
            var orders = new List<Order>();

            using var connection = _connectionManager.CreateConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand("sp_GetFilteredOrders", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            // Add parameters for stored procedure
            command.Parameters.Add(new SqlParameter("@Month", SqlDbType.Int) { Value = month ?? (object)DBNull.Value });
            command.Parameters.Add(new SqlParameter("@Year", SqlDbType.Int) { Value = year ?? (object)DBNull.Value });
            command.Parameters.Add(new SqlParameter("@Status", SqlDbType.Int) { Value = status.HasValue ? (int)status.Value : (object)DBNull.Value });
            command.Parameters.Add(new SqlParameter("@ProductId", SqlDbType.Int) { Value = productId ?? (object)DBNull.Value });

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                orders.Add(MapOrderFromReader(reader));
            }

            return orders;
        }

        // Transaction - Bulk Delete Orders
        public async Task<int> BulkDeleteOrdersAsync(int? month = null, int? year = null, OrderStatus? status = null, int? productId = null)
        {
            using var connection = _connectionManager.CreateConnection();
            await connection.OpenAsync();

            // Begin transaction for bulk operation
            using var transaction = connection.BeginTransaction();
            try
            {
                using var command = new SqlCommand("sp_BulkDeleteOrders", connection, transaction)
                {
                    CommandType = CommandType.StoredProcedure
                };

                // Add parameters for stored procedure
                command.Parameters.Add(new SqlParameter("@Month", SqlDbType.Int) { Value = month ?? (object)DBNull.Value });
                command.Parameters.Add(new SqlParameter("@Year", SqlDbType.Int) { Value = year ?? (object)DBNull.Value });
                command.Parameters.Add(new SqlParameter("@Status", SqlDbType.Int) { Value = status.HasValue ? (int)status.Value : (object)DBNull.Value });
                command.Parameters.Add(new SqlParameter("@ProductId", SqlDbType.Int) { Value = productId ?? (object)DBNull.Value });

                // Output parameter for deleted count
                var deletedCountParam = new SqlParameter("@DeletedCount", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };
                command.Parameters.Add(deletedCountParam);

                await command.ExecuteNonQueryAsync();

                // Commit transaction
                transaction.Commit();

                return (int)deletedCountParam.Value;
            }
            catch
            {
                // Rollback transaction on error
                transaction.Rollback();
                throw;
            }
        }

        private static Order MapOrderFromReader(SqlDataReader reader)
        {
            var order = new Order
            {
                Id = reader.GetInt32("Id"),
                Status = (OrderStatus)reader.GetInt32("Status"),
                CreatedDate = reader.GetDateTime("CreatedDate"),
                UpdatedDate = reader.GetDateTime("UpdatedDate"),
                ProductId = reader.GetInt32("ProductId")
            };

            // Check if product data is available (for joined queries)
            try
            {
                if (reader.GetOrdinal("Name") >= 0)
                {
                    order.Product = new Product
                    {
                        Id = order.ProductId,
                        Name = reader.GetString("Name"),
                        Description = reader.IsDBNull("Description") ? string.Empty : reader.GetString("Description"),
                        Weight = reader.GetDecimal("Weight"),
                        Height = reader.GetDecimal("Height"),
                        Width = reader.GetDecimal("Width"),
                        Length = reader.GetDecimal("Length")
                    };
                }
            }
            catch (IndexOutOfRangeException)
            {
                // Product columns not available in this query
            }

            return order;
        }

        private static void ValidateOrder(Order order)
        {
            if (order.ProductId <= 0)
                throw new ArgumentException("Order must have a valid Product ID.", nameof(order.ProductId));

            if (order.CreatedDate == default)
                throw new ArgumentException("Order must have a valid created date.", nameof(order.CreatedDate));

            if (order.UpdatedDate == default)
                throw new ArgumentException("Order must have a valid updated date.", nameof(order.UpdatedDate));

            if (!Enum.IsDefined(typeof(OrderStatus), order.Status))
                throw new ArgumentException("Order must have a valid status.", nameof(order.Status));
        }
    }
}

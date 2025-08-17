using ADO.Data.Infrastructure;
using ADO.Data.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ADO.Data.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly IDbConnectionManager _connectionManager;

        public ProductRepository(IDbConnectionManager connectionManager)
        {
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        }

        // Connected Model - Create with improved parameter handling
        public async Task<Product> CreateAsync(Product product)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            ValidateProduct(product);

            const string sql = @"
                INSERT INTO Product (Name, Description, Weight, Height, Width, Length)
                OUTPUT INSERTED.Id
                VALUES (@Name, @Description, @Weight, @Height, @Width, @Length)";

            try
            {
                using var connection = _connectionManager.CreateConnection();
                await connection.OpenAsync();

                using var command = new SqlCommand(sql, connection);
                
                // Use typed parameters instead of AddWithValue
                command.Parameters.Add(new SqlParameter("@Name", SqlDbType.NVarChar, 255) { Value = product.Name });
                command.Parameters.Add(new SqlParameter("@Description", SqlDbType.NVarChar) { Value = product.Description ?? (object)DBNull.Value });
                command.Parameters.Add(new SqlParameter("@Weight", SqlDbType.Decimal) { Value = product.Weight });
                command.Parameters.Add(new SqlParameter("@Height", SqlDbType.Decimal) { Value = product.Height });
                command.Parameters.Add(new SqlParameter("@Width", SqlDbType.Decimal) { Value = product.Width });
                command.Parameters.Add(new SqlParameter("@Length", SqlDbType.Decimal) { Value = product.Length });

                var result = await command.ExecuteScalarAsync();
                if (result == null)
                    throw new InvalidOperationException("Failed to create product: no ID returned");

                product.Id = (int)result;
                return product;
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException($"Database error while creating product: {ex.Message}", ex);
            }
        }

        // Connected Model - Read with better null handling
        public async Task<Product?> GetByIdAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Product ID must be greater than zero.", nameof(id));

            const string sql = @"
                SELECT Id, Name, Description, Weight, Height, Width, Length
                FROM Product
                WHERE Id = @Id";

            try
            {
                using var connection = _connectionManager.CreateConnection();
                await connection.OpenAsync();

                using var command = new SqlCommand(sql, connection);
                command.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int) { Value = id });

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return MapProductFromReader(reader);
                }

                return null;
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException($"Database error while retrieving product with ID {id}: {ex.Message}", ex);
            }
        }

        // Connected Model - Update with validation
        public async Task<Product> UpdateAsync(Product product)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            if (product.Id <= 0)
                throw new ArgumentException("Product ID must be greater than zero.", nameof(product));

            ValidateProduct(product);

            const string sql = @"
                UPDATE Product 
                SET Name = @Name, Description = @Description, Weight = @Weight, 
                    Height = @Height, Width = @Width, Length = @Length
                WHERE Id = @Id";

            try
            {
                using var connection = _connectionManager.CreateConnection();
                await connection.OpenAsync();

                using var command = new SqlCommand(sql, connection);
                
                command.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int) { Value = product.Id });
                command.Parameters.Add(new SqlParameter("@Name", SqlDbType.NVarChar, 255) { Value = product.Name });
                command.Parameters.Add(new SqlParameter("@Description", SqlDbType.NVarChar) { Value = product.Description ?? (object)DBNull.Value });
                command.Parameters.Add(new SqlParameter("@Weight", SqlDbType.Decimal) { Value = product.Weight });
                command.Parameters.Add(new SqlParameter("@Height", SqlDbType.Decimal) { Value = product.Height });
                command.Parameters.Add(new SqlParameter("@Width", SqlDbType.Decimal) { Value = product.Width });
                command.Parameters.Add(new SqlParameter("@Length", SqlDbType.Decimal) { Value = product.Length });

                var rowsAffected = await command.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                    throw new InvalidOperationException($"Product with ID {product.Id} was not found or could not be updated.");

                return product;
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException($"Database error while updating product with ID {product.Id}: {ex.Message}", ex);
            }
        }

        // Connected Model - Delete with validation
        public async Task<bool> DeleteAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Product ID must be greater than zero.", nameof(id));

            const string sql = "DELETE FROM Product WHERE Id = @Id";

            try
            {
                using var connection = _connectionManager.CreateConnection();
                await connection.OpenAsync();

                using var command = new SqlCommand(sql, connection);
                command.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int) { Value = id });

                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException($"Database error while deleting product with ID {id}: {ex.Message}", ex);
            }
        }

        // Disconnected Model - Get All Products using DataAdapter (async wrapper)
        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            const string sql = @"
                SELECT Id, Name, Description, Weight, Height, Width, Length
                FROM Product
                ORDER BY Name";

            try
            {
                return await Task.Run(() =>
                {
                    var products = new List<Product>();

                    using var connection = _connectionManager.CreateConnection();
                    using var adapter = new SqlDataAdapter(sql, connection);
                    var dataSet = new DataSet();
                    
                    // Fill DataSet (disconnected operation)
                    adapter.Fill(dataSet, "Products");

                    // Work with disconnected data
                    foreach (DataRow row in dataSet.Tables["Products"]!.Rows)
                    {
                        products.Add(new Product
                        {
                            Id = Convert.ToInt32(row["Id"]),
                            Name = row["Name"]?.ToString() ?? string.Empty,
                            Description = row["Description"]?.ToString() ?? string.Empty,
                            Weight = Convert.ToDecimal(row["Weight"]),
                            Height = Convert.ToDecimal(row["Height"]),
                            Width = Convert.ToDecimal(row["Width"]),
                            Length = Convert.ToDecimal(row["Length"])
                        });
                    }

                    return products;
                });
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException($"Database error while retrieving all products: {ex.Message}", ex);
            }
        }

        private static Product MapProductFromReader(SqlDataReader reader)
        {
            return new Product
            {
                Id = reader.GetInt32("Id"),
                Name = reader.GetString("Name"),
                Description = reader.IsDBNull("Description") ? string.Empty : reader.GetString("Description"),
                Weight = reader.GetDecimal("Weight"),
                Height = reader.GetDecimal("Height"),
                Width = reader.GetDecimal("Width"),
                Length = reader.GetDecimal("Length")
            };
        }

        private static void ValidateProduct(Product product)
        {
            if (string.IsNullOrWhiteSpace(product.Name))
                throw new ArgumentException("Product name cannot be null or empty.", nameof(product.Name));

            if (product.Weight < 0)
                throw new ArgumentException("Product weight cannot be negative.", nameof(product.Weight));

            if (product.Height < 0)
                throw new ArgumentException("Product height cannot be negative.", nameof(product.Height));

            if (product.Width < 0)
                throw new ArgumentException("Product width cannot be negative.", nameof(product.Width));

            if (product.Length < 0)
                throw new ArgumentException("Product length cannot be negative.", nameof(product.Length));
        }
    }
}

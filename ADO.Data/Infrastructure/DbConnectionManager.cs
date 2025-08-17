using Microsoft.Data.SqlClient;
using System.Data;

namespace ADO.Data.Infrastructure
{
    public interface IDbConnectionManager
    {
        SqlConnection CreateConnection();
        string ConnectionString { get; }
        Task<bool> TestConnectionAsync();
    }

    public class DbConnectionManager : IDbConnectionManager
    {
        public string ConnectionString { get; }

        public DbConnectionManager(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));
            
            ConnectionString = connectionString;
        }

        public SqlConnection CreateConnection()
        {
            return new SqlConnection(ConnectionString);
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();
                return connection.State == ConnectionState.Open;
            }
            catch
            {
                return false;
            }
        }
    }
}

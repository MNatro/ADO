namespace ADO.Data.Configuration
{
    public interface IConnectionConfiguration
    {
        string GetConnectionString(string name = "DefaultConnection");
        void SetConnectionString(string name, string connectionString);
    }

    public class ConnectionConfiguration : IConnectionConfiguration
    {
        private readonly Dictionary<string, string> _connectionStrings = new();

        public ConnectionConfiguration()
        {
            // Default connection string for development
            _connectionStrings["DefaultConnection"] = @"Server=(localdb)\MSSQLLocalDB;Database=OrderManagementDB;Trusted_Connection=true;TrustServerCertificate=true;";
        }

        public string GetConnectionString(string name = "DefaultConnection")
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Connection name cannot be null or empty.", nameof(name));

            if (!_connectionStrings.ContainsKey(name))
                throw new ArgumentException($"Connection string '{name}' not found.", nameof(name));

            return _connectionStrings[name];
        }

        public void SetConnectionString(string name, string connectionString)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Connection name cannot be null or empty.", nameof(name));

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));

            _connectionStrings[name] = connectionString;
        }
    }
}

using Microsoft.Data.SqlClient;
namespace ChecklistApp.Data
{
    public class SqlConnectionFactory : ISqlConnectionFactory
    {
        private readonly string _connectionString;
        public SqlConnectionFactory(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("ChecklistConnection")
                ?? throw new InvalidOperationException("Checklist Connection is missing.");

        }
        public SqlConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}

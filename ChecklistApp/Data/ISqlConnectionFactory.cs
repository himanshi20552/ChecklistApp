using Microsoft.Data.SqlClient;

namespace ChecklistApp.Data
{
    public interface ISqlConnectionFactory
    {
        SqlConnection CreateConnection();
    }
}

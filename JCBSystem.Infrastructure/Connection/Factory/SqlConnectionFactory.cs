using System.Data;
using System.Data.SqlClient;
using JCBSystem.Infrastructure.Connection.Interface;

namespace JCBSystem.Infrastructure.Connection.Factory
{
    public class SqlConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        public SqlConnectionFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IDbConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}

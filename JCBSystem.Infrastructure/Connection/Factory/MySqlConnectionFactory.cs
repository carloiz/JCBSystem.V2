using System.Data;
using MySqlConnector;
using JCBSystem.Infrastructure.Connection.Interface;

namespace JCBSystem.Infrastructure.Connection.Factory
{
    public class MySqlConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        public MySqlConnectionFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IDbConnection CreateConnection()
        {
            return new MySqlConnection(_connectionString);
        }
    }
}

using Npgsql;
using System.Data;
using JCBSystem.Infrastructure.Connection.Interface;

namespace JCBSystem.Infrastructure.Connection.Factory
{
    public class NpgsqlConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        public NpgsqlConnectionFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IDbConnection CreateConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }
    }

}

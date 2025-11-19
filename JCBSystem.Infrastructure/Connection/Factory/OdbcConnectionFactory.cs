using System.Data;
using System.Data.Odbc;
using JCBSystem.Infrastructure.Connection.Interface;

namespace JCBSystem.Infrastructure.Connection.Factory
{
    public class OdbcConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        public OdbcConnectionFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IDbConnection CreateConnection()
        {
            return new OdbcConnection(_connectionString);
        }
    }
}

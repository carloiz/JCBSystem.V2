using MySqlConnector;
using System.Data.Odbc;
using System.Data.SqlClient;
using System.Data;
using Npgsql;
using JCBSystem.Infrastructure.Connection.Factory;
using JCBSystem.Infrastructure.Connection.Interface;
using System.Threading.Tasks;
using System;
using System.Configuration;

namespace JCBSystem.Infrastructure.Connection
{
    public class ConnectionFactorySelector : IConnectionFactorySelector
    {
        private readonly string _connName = ConfigurationManager.AppSettings["ConnectionName"];

        private readonly string connectionString = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
        public Task<IDbConnectionFactory> GetFactory()
        {
            switch (_connName)
            {
                case "sql":
                    return Task.FromResult<IDbConnectionFactory>(new SqlConnectionFactory(connectionString));
                case "mysql":
                    return Task.FromResult<IDbConnectionFactory>(new MySqlConnectionFactory(connectionString));
                case "odbc":
                    return Task.FromResult<IDbConnectionFactory>(new OdbcConnectionFactory(connectionString));                
                case "npgsql":
                    return Task.FromResult<IDbConnectionFactory>(new NpgsqlConnectionFactory(connectionString));
                default:
                    throw new NotSupportedException($"Provider '{_connName}' is not supported.");
            }
        }

        public async Task OpenConnectionAsync(IDbConnection connection)
        {
            // IDbConnection does not have OpenAsync, so cast only if supported
            if (connection is SqlConnection sqlConn)
                await sqlConn.OpenAsync();
            else if (connection is MySqlConnection mySqlConn)
                await mySqlConn.OpenAsync();
            else if (connection is NpgsqlConnection pgSqlConn)
                await pgSqlConn.OpenAsync();
            else
                connection.Open(); // fallback for ODBC or others
        }

        public Task<IDataAdapter> CreateDataAdapter(IDbCommand command)
        {
            if (command is SqlCommand sqlCmd)
                return Task.FromResult<IDataAdapter>(new SqlDataAdapter(sqlCmd));

            if (command is OdbcCommand odbcCmd)
                return Task.FromResult<IDataAdapter>(new OdbcDataAdapter(odbcCmd));

            if (command is MySqlCommand mysqlCmd)
                return Task.FromResult<IDataAdapter>(new MySqlDataAdapter(mysqlCmd));           
            
            if (command is NpgsqlCommand npgSqlCmd)
                return Task.FromResult<IDataAdapter>(new NpgsqlDataAdapter(npgSqlCmd));
            // Add more if needed (e.g., MySqlCommand)

            throw new NotSupportedException($"Unsupported command type: {command.GetType().Name}");
        }
    }
}

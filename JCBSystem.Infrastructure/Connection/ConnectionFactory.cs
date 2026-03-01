using JCBSystem.Infrastructure.Connection.Factory;
using JCBSystem.Infrastructure.Connection.Interface;
using MySqlConnector;
using Npgsql;
using System;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace JCBSystem.Infrastructure.Connection
{
    public class ConnectionFactory : IConnectionFactory
    {
        private readonly string _providerName =
            ConfigurationManager.ConnectionStrings["ConnectionString"].ProviderName;

        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;

        public Task<IDbConnectionFactory> GetFactory()
        {
            switch (_providerName)
            {
                case "System.Data.SqlClient":
                    return Task.FromResult<IDbConnectionFactory>(
                        new SqlConnectionFactory(_connectionString));

                case "MySql.Data.MySqlClient":
                    return Task.FromResult<IDbConnectionFactory>(
                        new MySqlConnectionFactory(_connectionString));

                case "System.Data.Odbc":
                    return Task.FromResult<IDbConnectionFactory>(
                        new OdbcConnectionFactory(_connectionString));

                case "Npgsql":
                    return Task.FromResult<IDbConnectionFactory>(
                        new NpgsqlConnectionFactory(_connectionString));

                default:
                    throw new NotSupportedException(
                        $"Provider '{_providerName}' is not supported.");
            }
        }

        public async Task OpenConnectionAsync(IDbConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));
            if (connection.State == ConnectionState.Open)
                return;
            if (string.IsNullOrWhiteSpace(connection.ConnectionString))
                throw new InvalidOperationException("ConnectionString is not set.");

            try
            {
                Console.WriteLine($"Attempting to connect with: {connection.ConnectionString}");

                if (connection is DbConnection dbConn)
                {
                    Console.WriteLine("Before Open: " + connection.State);
                    await dbConn.OpenAsync();
                    Console.WriteLine("After Open: " + connection.State);
                }
                else
                {
                    connection.Open();
                }

                Console.WriteLine("Connection successful!");
            }
            catch (SqlException sqlEx)
            {
                Console.WriteLine();
                Console.WriteLine("═══════════════════════════════════════════════════");
                Console.WriteLine("❌ SQL SERVER ERROR");
                Console.WriteLine("═══════════════════════════════════════════════════");
                Console.WriteLine($"Error Number: {sqlEx.Number}");
                Console.WriteLine($"Error Message: {sqlEx.Message}");
                Console.WriteLine($"SQL Server: {sqlEx.Server}");
                Console.WriteLine($"Error Source: {sqlEx.Source}");
                Console.WriteLine("═══════════════════════════════════════════════════");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("═══════════════════════════════════════════════════");
                Console.WriteLine("❌ GENERAL CONNECTION ERROR");
                Console.WriteLine("═══════════════════════════════════════════════════");
                Console.WriteLine($"Error Type: {ex.GetType().Name}");
                Console.WriteLine($"Error Message: {ex.Message}");
                Console.WriteLine("═══════════════════════════════════════════════════");
                throw;
            }
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

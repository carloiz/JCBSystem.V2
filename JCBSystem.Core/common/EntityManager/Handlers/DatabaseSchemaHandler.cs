using JCBSystem.Infrastructure.Connection.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JCBSystem.Core.common.EntityManager.Handlers
{
    public class DatabaseSchemaHandler
    {
        private readonly IDbConnectionFactory dbConnectionFactory;
        private readonly IConnectionFactory connectionFactory;

        public DatabaseSchemaHandler(IDbConnectionFactory dbConnectionFactory, IConnectionFactory connectionFactory)
        {
            this.dbConnectionFactory = dbConnectionFactory;
            this.connectionFactory = connectionFactory;
        }
        /// <summary>
        /// CREATE DATABASE kung hindi pa existing
        /// </summary>
        public async Task<bool> HandleAsync(
             IDbConnection connection,
             string databaseName)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            if (string.IsNullOrWhiteSpace(databaseName) || !Regex.IsMatch(databaseName, @"^[a-zA-Z0-9_]+$"))
            {
                Console.WriteLine("Invalid database name.", nameof(databaseName));
                throw new ArgumentException("Invalid database name.", nameof(databaseName));
            }
            try
            {
                // Determine database type
                string connectionTypeName = connection.GetType().Name;
                bool isNpgSql = connectionTypeName.Contains("Npgsql") || connectionTypeName.Contains("Postgres");
                bool isOdbc = connectionTypeName.Contains("Odbc");

                // Get current connection string
                string originalConnectionString = connection.ConnectionString;

                // Parse and modify connection string to point to master/postgres
                var builder = new DbConnectionStringBuilder { ConnectionString = originalConnectionString };
                string masterDb = isNpgSql ? "postgres" : "master";

                if (builder.ContainsKey("Database"))
                    builder["Database"] = masterDb;
                else if (builder.ContainsKey("Initial Catalog"))
                    builder["Initial Catalog"] = masterDb;

                // Create new connection to master/postgres database
                IDbConnection masterConnection = dbConnectionFactory.CreateConnection();

                try
                {
                    await connectionFactory.OpenConnectionAsync(masterConnection);

                    // Check if database exists
                    bool exists = await DatabaseExistsAsync(masterConnection, databaseName, isNpgSql, isOdbc);

                    if (!exists)
                    {
                        string createDbQuery = isNpgSql
                            ? $"CREATE DATABASE {databaseName}"
                            : isOdbc
                            ? $"CREATE DATABASE `{databaseName}`"
                            : $"CREATE DATABASE [{databaseName}]";

                        using (var command = masterConnection.CreateCommand())
                        {
                            command.CommandText = createDbQuery;

                            if (command is DbCommand dbCmd)
                                await dbCmd.ExecuteNonQueryAsync();
                            else
                                command.ExecuteNonQuery();
                        }

                        return true;
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error creating database '{databaseName}': {ex.Message}");
                    return false;
                    //throw new Exception($"Error creating database '{databaseName}': {ex.Message}", ex);
                }
                finally
                {
                    if (masterConnection.State == ConnectionState.Open)
                    { 
                        Console.WriteLine("✔ Database updated successfully");
                        masterConnection.Close();
                    }
                    masterConnection.Dispose();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating database '{databaseName}': {ex.Message}");
                return false;
                //throw new Exception($"Error creating database '{databaseName}': {ex.Message}", ex);
            }
        }


        private async Task<bool> DatabaseExistsAsync(IDbConnection connection, string databaseName, bool isNpgSql, bool isOdbc)
        {
            try
            {
                string query;

                if (isNpgSql)
                {
                    query = "SELECT 1 FROM pg_database WHERE datname = @dbName";
                }
                else if (isOdbc) // MySQL
                {
                    query = "SELECT SCHEMA_NAME FROM information_schema.schemata WHERE SCHEMA_NAME = @dbName";
                }
                else // SQL Server
                {
                    query = "SELECT name FROM sys.databases WHERE name = @dbName";
                }


                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;

                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "@dbName";
                    parameter.Value = databaseName;
                    command.Parameters.Add(parameter);

                    object result;
                    if (command is DbCommand dbCmd)
                        result = await dbCmd.ExecuteScalarAsync();
                    else
                        result = command.ExecuteScalar();

                    return result != null;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating database '{databaseName}': {ex.Message}");
                return true;
                //throw new Exception($"Error creating database '{databaseName}': {ex.Message}", ex);
            }
        }
    }
}

using JCBSystem.Core.common.FormCustomization;
using JCBSystem.Core.common.Helpers;
using JCBSystem.Core.common.Interfaces;
using JCBSystem.Domain.DTO.Users;
using JCBSystem.Infrastructure.Connection.Interface;
using System;
using System.Configuration;
using System.Data;
using System.Threading.Tasks;

namespace JCBSystem.Services.Data.Seeders
{
    public class DatabaseBootstrapper
    {
        private readonly IDataManager dataManager;
        private readonly IConnectionFactory connectionFactory;
        private readonly IDbConnectionFactory dbConnectionFactory;

        public DatabaseBootstrapper(
            IDataManager dataManager,
            IConnectionFactory connectionFactory,
            IDbConnectionFactory dbConnectionFactory)
        {
            this.dataManager = dataManager;
            this.connectionFactory = connectionFactory;
            this.dbConnectionFactory = dbConnectionFactory;
        }

        public async Task RunAsync()
        {
            try
            {
                string databaseName = ConfigurationManager.AppSettings["DatabaseName"];
                string baseConnStr = ConfigurationManager
                    .ConnectionStrings["ConnectionString"]
                    .ConnectionString;

                Console.WriteLine("═══════════════════════════════════════════════════");
                Console.WriteLine("🚀 DATABASE BOOTSTRAP STARTED");
                Console.WriteLine("═══════════════════════════════════════════════════");
                Console.WriteLine($"📦 Target Database: {databaseName}");
                Console.WriteLine($"🔗 Original Connection: {baseConnStr}");
                Console.WriteLine();

                // 1️⃣ MASTER connection (connect to master database)
                string masterConnStr = ManipulateDatabaseInConnectionString(
                    baseConnStr,
                    null,  // ← Connect to master database
                    removeDatabase: true);

                Console.WriteLine("Original Connection String: {0}", baseConnStr);
                Console.WriteLine("Master Connection String: {0}", masterConnStr);


                using (var masterConnection = dbConnectionFactory.CreateConnection())
                {
                    masterConnection.ConnectionString = masterConnStr;

                    await connectionFactory.OpenConnectionAsync(masterConnection);

                    await dataManager.CreateDatabaseIfNotExistsAsync(
                        masterConnection,
                        databaseName);

                    Console.WriteLine($"✅ Database '{databaseName}' checked/created successfully");
                }

                Console.WriteLine();

                // 2️⃣ REAL database connection (add/replace database)
                string realConnStr = ManipulateDatabaseInConnectionString(
                    baseConnStr,
                    databaseName,
                    removeDatabase: false);

                using (var dbConnection = dbConnectionFactory.CreateConnection())
                {
                    dbConnection.ConnectionString = realConnStr;

                    await connectionFactory.OpenConnectionAsync(dbConnection);

                    using (var transaction = dbConnection.BeginTransaction())
                    {
                        await ProcessCreate(dbConnection, transaction);
                    }
                }

                Console.WriteLine();
                Console.WriteLine("═══════════════════════════════════════════════════");
                Console.WriteLine("✅ DATABASE BOOTSTRAP COMPLETED SUCCESSFULLY!");
                Console.WriteLine("═══════════════════════════════════════════════════");
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("═══════════════════════════════════════════════════");
                Console.WriteLine("🔥 DATABASE BOOTSTRAP ERROR");
                Console.WriteLine("═══════════════════════════════════════════════════");
                Console.WriteLine(ex.ToString());
                Console.WriteLine("═══════════════════════════════════════════════════");
                throw;
            }
        }

        /// <summary>
        /// Generic method to manipulate database in connection string.
        /// Supports both SQL Server (Initial Catalog) and MySQL/ODBC (database).
        /// </summary>
        private string ManipulateDatabaseInConnectionString(
            string connStr,
            string databaseName,
            bool removeDatabase = false)
        {
            if (string.IsNullOrWhiteSpace(connStr))
                throw new ArgumentException("Connection string cannot be null or empty", nameof(connStr));

            // Detect if SQL Server or MySQL/ODBC
            bool isSqlServer = connStr.IndexOf("Initial Catalog", StringComparison.OrdinalIgnoreCase) >= 0
                || connStr.IndexOf("Data Source", StringComparison.OrdinalIgnoreCase) >= 0
                || connStr.IndexOf("Server", StringComparison.OrdinalIgnoreCase) >= 0 && connStr.IndexOf("Driver", StringComparison.OrdinalIgnoreCase) < 0;

            string dbKey = isSqlServer ? "Initial Catalog" : "database";
            string pattern = isSqlServer
                ? @"Initial\s+Catalog\s*=\s*[^;]+"
                : @"database\s*=\s*[^;]+";

            string result = connStr;

            if (removeDatabase)
            {
                // Remove database from connection string
                result = System.Text.RegularExpressions.Regex.Replace(
                    result,
                    pattern + ";?",
                    "",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                // Clean up double semicolons and trailing semicolons
                result = result.Replace(";;", ";").TrimEnd(';');
            }
            else
            {
                if (string.IsNullOrWhiteSpace(databaseName))
                    throw new ArgumentException("Database name cannot be null or empty when adding database", nameof(databaseName));

                // Check if database key exists
                bool hasDatabase = System.Text.RegularExpressions.Regex.IsMatch(
                    result,
                    pattern,
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (hasDatabase)
                {
                    // Replace existing database
                    result = System.Text.RegularExpressions.Regex.Replace(
                        result,
                        pattern,
                        $"{dbKey}={databaseName}",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                }
                else
                {
                    // Append database
                    result = result.TrimEnd(';') + $";{dbKey}={databaseName}";
                }
            }

            return result;
        }

        private async Task ProcessCreate(IDbConnection connection, IDbTransaction transaction)
        {
            string username = "admin";
            string password = "admin";

            string hashPassword = PasswordHelper.HashPassword(password);
            DateTime dateToday = SystemDate.GetPhilippineTime();

            var userCreateDto = new UsersDto
            {
                UserNumber = "U000001",
                Username = username,
                Password = hashPassword,
                UserLevel = "Admin",
                Status = true,
                IsSessionActive = false,
                CurrentToken = null,
                RecordDate = dateToday
            };

            // Ensure TABLES
            await dataManager.CreateAlterTableAsync<UsersDto>(
                "Users",
                connection,
                transaction);

            await dataManager.InsertAsync(userCreateDto, "Users", connection, transaction, "UserNumber");

            transaction.Commit();

            Console.WriteLine("✅ Default User Created.");
        }
    }
}
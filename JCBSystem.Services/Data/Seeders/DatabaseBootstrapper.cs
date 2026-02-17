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
        private readonly IDataManager _dataManager;
        private readonly IConnectionFactory _connectionFactory;

        public DatabaseBootstrapper(
            IDataManager dataManager,
            IConnectionFactory connectionFactory)
        {
            _dataManager = dataManager;
            _connectionFactory = connectionFactory;
        }

        public async Task RunAsync()
        {
            try
            {
                string databaseName = ConfigurationManager.AppSettings["DatabaseName"];

                // Get connection string
                string baseConnStr = ConfigurationManager
                    .ConnectionStrings["ConnectionString"]
                    .ConnectionString;

                // 1️⃣ MASTER connection (no database)
                var factory = await _connectionFactory.GetFactory();
                using (var masterConnection = factory.CreateConnection())
                {
                    masterConnection.ConnectionString = baseConnStr;

                    // Open connection
                    await _connectionFactory.OpenConnectionAsync(masterConnection);

                    // Create database if not exists
                    await _dataManager.CreateDatabaseIfNotExistsAsync(
                        masterConnection,
                        databaseName);
                }

                // 3️⃣ ENSURE database part (manual & safe)
                string realConnStr;

                if (baseConnStr.IndexOf("database=", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    // replace existing database
                    realConnStr = System.Text.RegularExpressions.Regex.Replace(
                        baseConnStr,
                        @"database\s*=\s*[^;]+",
                        $"Database={databaseName}",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                }
                else
                {
                    // append database
                    realConnStr = baseConnStr.TrimEnd(';') + $";Database={databaseName}";
                }

                // 4️⃣ REAL database connection
                var realFactory = await _connectionFactory.GetFactory();
                using (var dbConnection = realFactory.CreateConnection())
                {
                    dbConnection.ConnectionString = realConnStr;

                    await _connectionFactory.OpenConnectionAsync(dbConnection);

                    using (var transaction = dbConnection.BeginTransaction())
                    {

                        await _dataManager.CommitAndRollbackMethod(async (conn, trans) =>
                        {
                            await ProcessCreate(dbConnection, transaction);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("🔥 DATABASE BOOTSTRAP ERROR");
                Console.WriteLine(ex.ToString());
                throw;
            }
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

            // 5️⃣ Ensure TABLES
            await _dataManager.CreateAlterTableAsync<UsersDto>(
                "Users",
                connection,
                transaction);

            await _dataManager.InsertAsync(userCreateDto, "Users", connection, transaction, "UserNumber");

            transaction.Commit();

            Console.WriteLine("Default User Created.");
        }
    }
}

using JCBSystem.Core.common.FormCustomization;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JCBSystem.Core.common.EntityManager.Handlers
{
    public class DeleteCommandHandler
    {

        /// <summary>
        /// DELETE 
        /// </summary>
        /// <param name="filterValues"></param>
        /// <param name="tableName"></param>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <param name="whereConditions"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="Exception"></exception>
        public async Task<int> HandleAsync(
            List<object> filterValues,
            string tableName,
            IDbConnection connection,
            IDbTransaction transaction,
            string whereConditions = null)
        {
            if (string.IsNullOrWhiteSpace(tableName) || !Regex.IsMatch(tableName, @"^[a-zA-Z0-9_]+$"))
                throw new ArgumentException("Invalid table name.", nameof(tableName));

            if (filterValues.Count > 0 && string.IsNullOrWhiteSpace(whereConditions))
                throw new ArgumentException("WHERE conditions are required when filters are provided.");

            var isOdbc = connection is OdbcConnection;

            var isNpgSql = connection is NpgsqlConnection;

            string sqlTableName = isNpgSql ? tableName : $"[{tableName}]";

            string finalQuery = Modules.ReplaceSharpWithParams(whereConditions, isOdbc);

            if (isOdbc)
                tableName.ToLower();

            // 🔧 Build the DELETE query
            string query = string.IsNullOrWhiteSpace(whereConditions)
                ? (isOdbc ? $"DELETE FROM `{tableName}`" : $"DELETE FROM {sqlTableName}")
                : (isOdbc ? $"DELETE FROM `{tableName}` WHERE {finalQuery}" : $"DELETE FROM {sqlTableName} WHERE {finalQuery}");

            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = query;

                    // 🧷 Bind parameters (ODBC uses `?`, SQL Server uses `@paramX`)
                    for (int i = 0; i < filterValues.Count; i++)
                    {
                        var parameter = command.CreateParameter();
                        parameter.ParameterName = isOdbc ? null : $"@param{i}";
                        parameter.Value = filterValues[i] ?? DBNull.Value;
                        command.Parameters.Add(parameter);
                    }

                    // ✅ Execute async if supported
                    if (command is DbCommand dbCommand)
                        return await dbCommand.ExecuteNonQueryAsync();
                    else
                        return command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while deleting data: " + ex.Message, ex);
            }
        }
    }
}

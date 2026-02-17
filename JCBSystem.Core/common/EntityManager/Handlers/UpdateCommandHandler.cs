using JCBSystem.Core.common.Attributes;
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
    public class UpdateCommandHandler
    {

        /// <summary>
        /// UPDATE
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <param name="tableName"></param>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <param name="primaryKey"></param>
        /// <param name="whereCondition"></param>
        /// <param name="additionalParameters"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="Exception"></exception>
        public async Task<int> HandleAsync<T>(
           T entity,
           string tableName,
           IDbConnection connection,
           IDbTransaction transaction,
           string primaryKey = null,
           string whereCondition = null,
           List<object> additionalParameters = null)
        {
            if (string.IsNullOrWhiteSpace(tableName) || !Regex.IsMatch(tableName, @"^[a-zA-Z0-9_]+$"))
                throw new ArgumentException("Invalid table name.", nameof(tableName));

            try
            {
                var isOdbc = connection is OdbcConnection;

                bool isNpgSql = connection is NpgsqlConnection;

                if (isOdbc)
                    tableName.ToLower();

                var properties = typeof(T)
                    .GetProperties()
                    .Where(p =>
                    {
                        if (!p.CanRead) return false;

                        // never update PK
                        if (!string.IsNullOrEmpty(primaryKey) &&
                            string.Equals(p.Name, primaryKey, StringComparison.OrdinalIgnoreCase))
                            return false;

                        var value = p.GetValue(entity);

                        // NULL value
                        if (value == null)
                        {
                            // include ONLY if explicitly allowed
                            return Attribute.IsDefined(p, typeof(AllowNullUpdateAttribute));
                        }

                        // has value → update
                        return true;
                    })
                    .ToArray();



                var setProperties = string.IsNullOrEmpty(primaryKey)
                    ? properties
                    : properties.Where(p => !string.Equals(p.Name, primaryKey, StringComparison.OrdinalIgnoreCase)).ToArray();

                // SET clause
                var setClause = string.Join(", ", setProperties.Select(p =>
                    isNpgSql ? $"{p.Name} = @{p.Name}" : isOdbc ? $"`{p.Name}` = ?" : $"[{p.Name}] = @{p.Name}"
                ));

                string query;

                if (!string.IsNullOrEmpty(primaryKey))
                {
                    query = isNpgSql ? $"UPDATE {tableName} SET {setClause} WHERE {primaryKey} = @{primaryKey}" :
                        isOdbc
                        ? $"UPDATE `{tableName}` SET {setClause} WHERE `{primaryKey}` = ?"
                        : $"UPDATE [{tableName}] SET {setClause} WHERE [{primaryKey}] = @{primaryKey}";
                }
                else if (!string.IsNullOrEmpty(whereCondition))
                {
                    string finalQuery = Modules.ReplaceSharpWithParams(whereCondition, isOdbc);

                    query = isNpgSql ? $"UPDATE {tableName} SET {setClause} WHERE {finalQuery}" :
                        isOdbc
                        ? $"UPDATE `{tableName}` SET {setClause} WHERE {finalQuery}"
                        : $"UPDATE [{tableName}] SET {setClause} WHERE {finalQuery}";
                }
                else
                {
                    query = isNpgSql ? $"UPDATE {tableName} SET {setClause}" :
                        isOdbc
                        ? $"UPDATE `{tableName}` SET {setClause}"
                        : $"UPDATE [{tableName}] SET {setClause}";
                }

                using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = query;

                    // Parameter index tracking for ODBC (uses '?')
                    int paramIndex = 0;

                    // SET parameters
                    foreach (var prop in setProperties)
                    {
                        var param = command.CreateParameter();
                        param.ParameterName = isOdbc ? null : "@" + prop.Name;
                        param.Value = prop.GetValue(entity) ?? DBNull.Value;
                        command.Parameters.Add(param);
                        paramIndex++;
                    }

                    // Primary key parameter
                    if (!string.IsNullOrEmpty(primaryKey))
                    {
                        var pkProp = typeof(T).GetProperty(primaryKey);
                        if (pkProp != null)
                        {
                            var pkParam = command.CreateParameter();
                            pkParam.ParameterName = isOdbc ? null : "@" + primaryKey;
                            pkParam.Value = pkProp.GetValue(entity) ?? DBNull.Value;
                            command.Parameters.Add(pkParam);
                            paramIndex++;
                        }
                    }

                    // Additional parameters (for WHERE clause)
                    if (additionalParameters != null)
                    {
                        foreach (var val in additionalParameters)
                        {
                            var param = command.CreateParameter();
                            param.ParameterName = isOdbc ? null : $"@param{paramIndex}";
                            param.Value = val ?? DBNull.Value;
                            command.Parameters.Add(param);
                            paramIndex++;
                        }
                    }

                    // Execute async if supported
                    if (command is DbCommand dbCommand)
                        return await dbCommand.ExecuteNonQueryAsync();
                    else
                        return command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while updating data: " + ex.Message, ex);
            }
        }
    }
}

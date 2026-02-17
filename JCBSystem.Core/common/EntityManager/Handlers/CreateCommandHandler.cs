using JCBSystem.Core.common.Attributes;
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
    public class CreateCommandHandler
    {
        /// <summary>
        /// CREATE / ADD / INSERT
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <param name="tableName"></param>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <param name="primaryKeyColumn"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="Exception"></exception>
        public async Task<object> HandleAsync<T>(
           T entity,
           string tableName,
           IDbConnection connection,
           IDbTransaction transaction,
           string primaryKeyColumn = "id") // ➔ Added optional parameter
        {
            if (string.IsNullOrWhiteSpace(tableName) || !Regex.IsMatch(tableName, @"^[a-zA-Z0-9_]+$"))
                throw new ArgumentException("Invalid table name.", nameof(tableName));

            if (string.IsNullOrWhiteSpace(primaryKeyColumn))
                throw new ArgumentException("Primary key column name must be provided.", nameof(primaryKeyColumn));

            try
            {
                var properties = typeof(T)
                       .GetProperties()
                       .Where(p =>
                       {
                           if (!p.CanRead) return false;

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

                if (!properties.Any())
                    throw new ArgumentException("Entity has no readable properties with values.");

                bool isOdbc = connection is OdbcConnection;
                bool isNpgSql = connection is NpgsqlConnection;

                if (isOdbc)
                    tableName = tableName.ToLower();

                string columnList = string.Join(", ", properties.Select(p =>
                    (isOdbc || isNpgSql) ? p.Name : $"[{p.Name}]"
                ));

                string valueList = string.Join(", ", properties.Select(p =>
                    isOdbc ? "?" : "@" + p.Name
                ));

                string insertQuery = $"INSERT INTO {(isOdbc || isNpgSql ? tableName : $"[{tableName}]")} ({columnList}) VALUES ({valueList})";

                if (isNpgSql)
                {
                    insertQuery += $" RETURNING {primaryKeyColumn}"; // ➔ dynamic na ang primary key
                }

                using (var insertCommand = connection.CreateCommand())
                {
                    insertCommand.Transaction = transaction;
                    insertCommand.CommandText = insertQuery;

                    foreach (var prop in properties)
                    {
                        var param = insertCommand.CreateParameter();
                        param.ParameterName = isOdbc ? null : "@" + prop.Name;
                        param.Value = prop.GetValue(entity) ?? DBNull.Value;
                        insertCommand.Parameters.Add(param);
                    }

                    if (isNpgSql)
                    {
                        if (insertCommand is DbCommand insertDbCommand)
                            return await insertDbCommand.ExecuteScalarAsync();
                        else
                            return insertCommand.ExecuteScalar();
                    }
                    else
                    {
                        if (insertCommand is DbCommand insertDbCommand)
                            await insertDbCommand.ExecuteNonQueryAsync();
                        else
                            insertCommand.ExecuteNonQuery();

                        using (var identityCommand = connection.CreateCommand())
                        {
                            identityCommand.Transaction = transaction;

                            if (isOdbc)
                                identityCommand.CommandText = "SELECT LAST_INSERT_ID();";
                            else
                                identityCommand.CommandText = "SELECT SCOPE_IDENTITY();";

                            if (identityCommand is DbCommand identityDbCommand)
                                return await identityDbCommand.ExecuteScalarAsync();
                            else
                                return identityCommand.ExecuteScalar();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while inserting data: " + ex.Message, ex);
            }
        }
    }
}

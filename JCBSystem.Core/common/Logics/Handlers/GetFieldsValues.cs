using Npgsql;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Odbc;
using System.Threading.Tasks;
using JCBSystem.Core.common.FormCustomization;
using JCBSystem.Infrastructure.Connection.Interface;

namespace JCBSystem.Core.common.Logics.Handlers
{
    public class GetFieldsValues
    {
        private readonly IConnectionFactorySelector connectionFactorySelector;
        private readonly IDbConnectionFactory dbConnectionFactory;

        public GetFieldsValues(IDbConnectionFactory dbConnectionFactory, IConnectionFactorySelector connectionFactorySelector)
        {
            this.dbConnectionFactory = dbConnectionFactory;
            this.connectionFactorySelector = connectionFactorySelector;
        }

        public async Task<Dictionary<string, object>> HandleAsync(
           List<object> filter,
           string tableName,
           List<string> fieldNamesQuery,
           List<string> fieldNames,
           string whereCondition)
        {
            if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(whereCondition))
                throw new ArgumentException("Table name and where condition must not be null or empty.");

            if (fieldNamesQuery == null || fieldNamesQuery.Count == 0)
                throw new ArgumentException("Field names for the query must not be null or empty.");

            if (fieldNames == null || fieldNames.Count == 0)
                throw new ArgumentException("Field names must not be null or empty.");

            var resultDictionary = new Dictionary<string, object>();
            int index = 0;

            using (var connection = dbConnectionFactory.CreateConnection())
            {
                await connectionFactorySelector.OpenConnectionAsync(connection);

                var isOdbc = connection is OdbcConnection;
                var isNpgSql = connection is NpgsqlConnection;

                string finalQuery = Modules.ReplaceSharpWithParams(whereCondition, isOdbc);


                // Format SELECT query depending on connection type
                string fields = string.Join(", ", fieldNamesQuery);
                string query = string.Format(
                    (isOdbc || isNpgSql)
                        ? "SELECT {0} FROM {1} WHERE {2}"     // MySQL-style
                        : "SELECT {0} FROM [{1}] WHERE {2}",    // SQL Server-style
                    fields, tableName, finalQuery);

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;

                    if (filter?.Count > 0)
                    {
                        foreach (var param in filter)
                        {
                            string paramName = isOdbc ? "?" : "@param" + index;

                            var dbParam = command.CreateParameter();
                            dbParam.ParameterName = paramName;
                            dbParam.Value = param ?? DBNull.Value;
                            command.Parameters.Add(dbParam);

                            index++;
                        }
                    }
                    if (command is DbCommand dbCommand)
                    {
                        using (var reader = await dbCommand.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                foreach (var fieldName in fieldNames)
                                {
                                    resultDictionary[fieldName] = reader[fieldName] == DBNull.Value ? null : reader[fieldName];
                                }
                            }
                        }
                    }
                    else
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                foreach (var fieldName in fieldNames)
                                {
                                    resultDictionary[fieldName] = reader[fieldName] == DBNull.Value ? null : reader[fieldName];
                                }
                            }
                        }
                    }
                }
            }

            return resultDictionary;
        }


    }
}

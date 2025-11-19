using Npgsql;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Odbc;
using System.Threading.Tasks;
using JCBSystem.Core.common.FormCustomization;
using JCBSystem.Infrastructure.Connection.Interface;

namespace JCBSystem.Core.common.Logics
{
    public class CheckIfRecordExists
    {
        private readonly IConnectionFactorySelector connectionFactorySelector;
        private readonly IDbConnectionFactory dbConnectionFactory;

        public CheckIfRecordExists(IDbConnectionFactory dbConnectionFactory, IConnectionFactorySelector connectionFactorySelector)
        {
            this.dbConnectionFactory = dbConnectionFactory;
            this.connectionFactorySelector = connectionFactorySelector;
        }

        public async Task<bool> ExecuteAsync(List<object> filter, string tableName, string whereCondition)
        {
            if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(whereCondition))
                throw new ArgumentException("Table name and where condition must not be null or empty.");

            int index = 0;

            using (var connection = dbConnectionFactory.CreateConnection())
            {
                await connectionFactorySelector.OpenConnectionAsync(connection);

                bool isOdbc = connection is OdbcConnection;

                bool isNpgsql = connection is NpgsqlConnection;


                string finalQuery = Modules.ReplaceSharpWithParams(whereCondition, isOdbc);

                // Use appropriate format depending on connection type
                string queryFormat = (isOdbc || isNpgsql)
                    ? "SELECT COUNT(1) FROM {0} WHERE {1}" // MySQL-style backticks
                    : "SELECT COUNT(1) FROM [{0}] WHERE {1}"; // SQL Server-style brackets

                string query = string.Format(queryFormat, tableName, finalQuery);

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;

                    if (filter.Count > 0)
                    {
                        foreach (var param in filter)
                        {
                            string paramName = isOdbc ? "?" : "@param" + index;

                            var parameter = command.CreateParameter();
                            parameter.ParameterName = paramName;
                            parameter.Value = param;
                            command.Parameters.Add(parameter);

                            index++;
                        }
                    }

                    if (command is DbCommand dbCountCommand)
                    {
                        var result = await dbCountCommand.ExecuteScalarAsync();
                        int recordCount = Convert.ToInt32(result);
                        return recordCount > 0;
                    }
                    else
                    {
                        var result = command.ExecuteScalar();
                        int recordCount = Convert.ToInt32(result);
                        return recordCount > 0;
                    }
                }
            }
        }
    }
}

using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Odbc; 
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JCBSystem.Core.common.EntityManager.Handlers
{
    public class TableSchemaHandler
    {
        public async Task<bool> HandleAsync<T>(
            string tableName,
            IDbConnection connection,
            IDbTransaction transaction = null,
            string primaryKeyOverride = null,
            bool autoIncrement = false)
        {
            if (string.IsNullOrWhiteSpace(tableName) || !Regex.IsMatch(tableName, @"^[a-zA-Z0-9_]+$"))
                throw new ArgumentException("Invalid table name.", nameof(tableName));

            try
            {
                var isOdbc = connection is OdbcConnection;
                var isNpgSql = connection is NpgsqlConnection;

                if (isOdbc)
                    tableName = tableName.ToLower();

                // Check if table exists
                bool tableExists = await TableExistsAsync(tableName, connection, transaction, isNpgSql, isOdbc);

                if (tableExists)
                {
                    // Update table structure (add missing columns)
                    await UpdateTableStructureAsync<T>(tableName, connection, transaction, isNpgSql, isOdbc);
                    Console.WriteLine("Successfully Tables Updated.");
                    return false; // Table already existed, just updated
                }
                else
                {
                    // Create new table
                    await CreateTableAsync<T>(tableName, connection, transaction, primaryKeyOverride, autoIncrement, isNpgSql, isOdbc);
                    Console.WriteLine("Successfully Tables Created & Updated.");
                    return true; // New table created
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"An error occurred while creating/updating table '{tableName}': {ex.Message}", ex);
            }
        }

        private async Task<bool> TableExistsAsync(
            string tableName,
            IDbConnection connection,
            IDbTransaction transaction,
            bool isNpgSql,
            bool isOdbc)
        {
            string query;

            if (isNpgSql)
            {
                query = @"
                        SELECT 1
                        FROM information_schema.tables
                        WHERE table_name = @tableName
                        LIMIT 1";
            }
            else if (isOdbc) // MySQL
            {
                query = @"
                        SELECT 1
                        FROM information_schema.tables
                        WHERE table_schema = DATABASE()
                          AND table_name = ?
                        LIMIT 1";
            }
            else // SQL Server
            {
                query = @"
                        SELECT 1
                        FROM INFORMATION_SCHEMA.TABLES
                        WHERE TABLE_NAME = @tableName";
            }

            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = query;

                var param = command.CreateParameter();
                param.ParameterName = isOdbc ? null : "@tableName";
                param.Value = tableName;
                command.Parameters.Add(param);

                object result;
                if (command is DbCommand dbCommand)
                    result = await dbCommand.ExecuteScalarAsync();
                else
                    result = command.ExecuteScalar();

                return result != null;
            }
        }


        private async Task CreateTableAsync<T>(
            string tableName,
            IDbConnection connection,
            IDbTransaction transaction,
            string primaryKeyOverride,
            bool autoIncrement,
            bool isNpgSql,
            bool isOdbc)
        {
            var properties = typeof(T).GetProperties().Where(p => p.CanRead && p.CanWrite).ToArray();

            // Find primary key
            string primaryKey = primaryKeyOverride;
            if (string.IsNullOrEmpty(primaryKey))
            {
                var keyProperty = properties.FirstOrDefault(p =>
                    Attribute.IsDefined(p, typeof(System.ComponentModel.DataAnnotations.KeyAttribute)));

                if (keyProperty != null)
                    primaryKey = keyProperty.Name;
            }

            var columnDefinitions = new List<string>();

            foreach (var prop in properties)
            {
                bool isPrimaryKey = !string.IsNullOrEmpty(primaryKey) &&
                                   string.Equals(prop.Name, primaryKey, StringComparison.OrdinalIgnoreCase);

                string columnDef = BuildColumnDefinition(prop, isPrimaryKey, autoIncrement, isNpgSql, isOdbc);
                columnDefinitions.Add(columnDef);
            }

            string columnsClause = string.Join(", ", columnDefinitions);

            string createTableQuery;
            if (isNpgSql)
            {
                createTableQuery = $"CREATE TABLE {tableName} ({columnsClause})";
            }
            else if (isOdbc)
            {
                createTableQuery = $"CREATE TABLE `{tableName}` ({columnsClause})";
            }
            else
            {
                createTableQuery = $"CREATE TABLE [{tableName}] ({columnsClause})";
            }

            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = createTableQuery;

                if (command is DbCommand dbCommand)
                    await dbCommand.ExecuteNonQueryAsync();
                else
                    command.ExecuteNonQuery();
            }
        }

        private string BuildColumnDefinition(
            PropertyInfo property,
            bool isPrimaryKey,
            bool autoIncrement,
            bool isNpgSql,
            bool isOdbc)
        {
            string columnName = isOdbc ? $"`{property.Name}`" :
                                isNpgSql ? property.Name :
                                $"[{property.Name}]";

            string dataType = GetSqlDataType(property.PropertyType, isNpgSql, isOdbc);

            var parts = new List<string> { columnName, dataType };

            if (isPrimaryKey)
            {
                if (autoIncrement)
                {
                    if (isNpgSql)
                    {
                        // PostgreSQL uses SERIAL or IDENTITY
                        if (IsIntegerType(property.PropertyType))
                        {
                            parts[1] = property.PropertyType == typeof(long) || property.PropertyType == typeof(long?)
                                ? "BIGSERIAL"
                                : "SERIAL";
                            parts.Add("PRIMARY KEY");
                        }
                        else
                        {
                            parts.Add("PRIMARY KEY");
                        }
                    }
                    else if (isOdbc)
                    {
                        parts.Add("PRIMARY KEY");
                        if (IsIntegerType(property.PropertyType))
                            parts.Add("AUTO_INCREMENT");
                    }
                    else
                    {
                        // SQL Server
                        if (IsIntegerType(property.PropertyType))
                            parts.Add("IDENTITY(1,1)");
                        parts.Add("PRIMARY KEY");
                    }
                }
                else
                {
                    parts.Add("PRIMARY KEY");
                }
            }
            else
            {
                // Check for Required attribute
                if (Attribute.IsDefined(property, typeof(System.ComponentModel.DataAnnotations.RequiredAttribute)))
                {
                    parts.Add("NOT NULL");
                }
                else if (!IsNullableType(property.PropertyType))
                {
                    parts.Add("NOT NULL");
                }
                else
                {
                    parts.Add("NULL");
                }
            }

            return string.Join(" ", parts);
        }

        private string GetSqlDataType(Type type, bool isNpgSql, bool isOdbc)
        {
            Type underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            if (isNpgSql)
            {
                if (underlyingType == typeof(int)) return "INTEGER";
                if (underlyingType == typeof(long)) return "BIGINT";
                if (underlyingType == typeof(short)) return "SMALLINT";
                if (underlyingType == typeof(byte)) return "SMALLINT";
                if (underlyingType == typeof(bool)) return "BOOLEAN";
                if (underlyingType == typeof(DateTime)) return "TIMESTAMP";
                if (underlyingType == typeof(decimal)) return "NUMERIC(18,2)";
                if (underlyingType == typeof(double)) return "DOUBLE PRECISION";
                if (underlyingType == typeof(float)) return "REAL";
                if (underlyingType == typeof(Guid)) return "UUID";
                if (underlyingType == typeof(string)) return "TEXT";
                if (underlyingType == typeof(byte[])) return "BYTEA";
                return "TEXT";
            }
            else if (isOdbc)
            {
                if (underlyingType == typeof(int)) return "INTEGER";
                if (underlyingType == typeof(long)) return "BIGINT";
                if (underlyingType == typeof(short)) return "SMALLINT";
                if (underlyingType == typeof(byte)) return "TINYINT";
                if (underlyingType == typeof(bool)) return "BOOLEAN";
                if (underlyingType == typeof(DateTime)) return "DATETIME";
                if (underlyingType == typeof(decimal)) return "DECIMAL(18,2)";
                if (underlyingType == typeof(double)) return "DOUBLE";
                if (underlyingType == typeof(float)) return "FLOAT";
                if (underlyingType == typeof(Guid)) return "CHAR(36)";
                if (underlyingType == typeof(string)) return "VARCHAR(255)";
                if (underlyingType == typeof(byte[])) return "BLOB";
                return "TEXT";
            }
            else
            {
                // SQL Server
                if (underlyingType == typeof(int)) return "INT";
                if (underlyingType == typeof(long)) return "BIGINT";
                if (underlyingType == typeof(short)) return "SMALLINT";
                if (underlyingType == typeof(byte)) return "TINYINT";
                if (underlyingType == typeof(bool)) return "BIT";
                if (underlyingType == typeof(DateTime)) return "DATETIME2";
                if (underlyingType == typeof(decimal)) return "DECIMAL(18,2)";
                if (underlyingType == typeof(double)) return "FLOAT";
                if (underlyingType == typeof(float)) return "REAL";
                if (underlyingType == typeof(Guid)) return "UNIQUEIDENTIFIER";
                if (underlyingType == typeof(string)) return "NVARCHAR(255)";
                if (underlyingType == typeof(byte[])) return "VARBINARY(MAX)";
                return "NVARCHAR(MAX)";
            }
        }

        private async Task UpdateTableStructureAsync<T>(
           string tableName,
           IDbConnection connection,
           IDbTransaction transaction,
           bool isNpgSql,
           bool isOdbc)
        {
            // SAFETY CHECK
            bool exists = await TableExistsAsync(
                tableName, connection, transaction, isNpgSql, isOdbc);

            if (!exists)
                return; // huwag mag ALTER kung wala ang table

            var properties = typeof(T)
                .GetProperties()
                .Where(p => p.CanRead && p.CanWrite)
                .ToArray();

            var existingColumns = await GetExistingColumnsAsync(
                tableName, connection, transaction, isNpgSql, isOdbc);

            foreach (var prop in properties)
            {
                if (!existingColumns.Contains(prop.Name, StringComparer.OrdinalIgnoreCase))
                {
                    await AddColumnAsync(
                        tableName, prop, connection, transaction, isNpgSql, isOdbc);
                }
            }
        }


        private async Task<List<string>> GetExistingColumnsAsync(
            string tableName,
            IDbConnection connection,
            IDbTransaction transaction,
            bool isNpgSql,
            bool isOdbc)
        {
            var columns = new List<string>();

            string query;
            if (isNpgSql)
            {
                query = "SELECT column_name FROM information_schema.columns WHERE table_name = @tableName";
            }
            else if (isOdbc)
            {
                query = @"
                        SELECT column_name
                        FROM information_schema.columns
                        WHERE table_schema = DATABASE()
                            AND table_name = ?";
            }

            else
            {
                query = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @tableName";
            }

            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = query;

                var param = command.CreateParameter();
                param.ParameterName = isOdbc ? null : "@tableName";
                param.Value = tableName;
                command.Parameters.Add(param);

                if (command is DbCommand dbCommand)
                {
                    using (var reader = await dbCommand.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            columns.Add(reader.GetString(0));
                        }
                    }
                }
                else
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            columns.Add(reader.GetString(0));
                        }
                    }
                }
            }

            return columns;
        }

        private async Task AddColumnAsync(
            string tableName,
            PropertyInfo property,
            IDbConnection connection,
            IDbTransaction transaction,
            bool isNpgSql,
            bool isOdbc)
        {
            string columnName = isOdbc ? $"`{property.Name}`" :
                                isNpgSql ? property.Name :
                                $"[{property.Name}]";

            string dataType = GetSqlDataType(property.PropertyType, isNpgSql, isOdbc);

            string nullable = IsNullableType(property.PropertyType) ? "NULL" : "NOT NULL";

            string alterQuery;
            if (isNpgSql)
            {
                alterQuery = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {dataType} {nullable}";
            }
            else if (isOdbc)
            {
                alterQuery = $"ALTER TABLE `{tableName}` ADD `{property.Name}` {dataType} {nullable}";
            }
            else
            {
                alterQuery = $"ALTER TABLE [{tableName}] ADD {columnName} {dataType} {nullable}";
            }

            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = alterQuery;

                if (command is DbCommand dbCommand)
                    await dbCommand.ExecuteNonQueryAsync();
                else
                    command.ExecuteNonQuery();
            }
        }

        private bool IsIntegerType(Type type)
        {
            Type underlyingType = Nullable.GetUnderlyingType(type) ?? type;
            return underlyingType == typeof(int) ||
                   underlyingType == typeof(long) ||
                   underlyingType == typeof(short) ||
                   underlyingType == typeof(byte);
        }

        private bool IsNullableType(Type type)
        {
            return !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
        }
    }
}

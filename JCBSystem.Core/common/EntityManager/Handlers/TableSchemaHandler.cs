using Npgsql;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JCBSystem.Core.common.Attributes;

namespace JCBSystem.Core.common.EntityManager.Handlers
{
    public class TableSchemaHandler
    {
        // ─────────────────────────────────────────────────────────────────────
        // ENTRY POINT
        // ─────────────────────────────────────────────────────────────────────

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

                if (isOdbc) tableName = tableName.ToLower();

                bool tableExists = await TableExistsAsync(tableName, connection, transaction, isNpgSql, isOdbc);

                if (tableExists)
                {
                    await UpdateTableStructureAsync<T>(
                        tableName, connection, transaction,
                        primaryKeyOverride, autoIncrement,
                        isNpgSql, isOdbc);
                    Console.WriteLine($"[{tableName}] Successfully Updated.");
                    return false;
                }
                else
                {
                    await CreateTableAsync<T>(
                        tableName, connection, transaction,
                        primaryKeyOverride, autoIncrement,
                        isNpgSql, isOdbc);
                    Console.WriteLine($"[{tableName}] Successfully Created.");
                    return true;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"An error occurred while creating/updating table '{tableName}': {ex.Message}", ex);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // TABLE EXISTS
        // ─────────────────────────────────────────────────────────────────────

        private async Task<bool> TableExistsAsync(
            string tableName,
            IDbConnection connection,
            IDbTransaction transaction,
            bool isNpgSql,
            bool isOdbc)
        {
            string query;
            if (isNpgSql)
                query = "SELECT 1 FROM information_schema.tables WHERE table_name = @tableName LIMIT 1";
            else if (isOdbc)
                query = "SELECT 1 FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = ? LIMIT 1";
            else
                query = "SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @tableName";

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

        // ─────────────────────────────────────────────────────────────────────
        // CREATE TABLE
        // ─────────────────────────────────────────────────────────────────────

        private async Task CreateTableAsync<T>(
            string tableName,
            IDbConnection connection,
            IDbTransaction transaction,
            string primaryKeyOverride,
            bool autoIncrement,
            bool isNpgSql,
            bool isOdbc)
        {
            var properties = typeof(T).GetProperties()
                .Where(p => p.CanRead && p.CanWrite)
                .ToArray();

            string primaryKey = primaryKeyOverride;
            if (string.IsNullOrEmpty(primaryKey))
            {
                var keyProperty = properties.FirstOrDefault(
                    p => Attribute.IsDefined(p, typeof(System.ComponentModel.DataAnnotations.KeyAttribute)));
                if (keyProperty != null)
                    primaryKey = keyProperty.Name;
            }

            var columnDefinitions = new List<string>();
            foreach (var prop in properties)
            {
                bool isPrimaryKey = !string.IsNullOrEmpty(primaryKey) &&
                    string.Equals(prop.Name, primaryKey, StringComparison.OrdinalIgnoreCase);
                columnDefinitions.Add(BuildColumnDefinition(prop, isPrimaryKey, autoIncrement, isNpgSql, isOdbc));
            }

            string columnsClause = string.Join(", ", columnDefinitions);
            string createTableQuery = isNpgSql || isOdbc
                ? $"CREATE TABLE {tableName} ({columnsClause})"
                : $"CREATE TABLE [{tableName}] ({columnsClause})";

            await ExecuteNonQueryAsync(createTableQuery, connection, transaction);
        }

        // ─────────────────────────────────────────────────────────────────────
        // UPDATE TABLE STRUCTURE
        // Handles: add missing columns, alter changed column types, sync PK
        // ─────────────────────────────────────────────────────────────────────

        private async Task UpdateTableStructureAsync<T>(
            string tableName,
            IDbConnection connection,
            IDbTransaction transaction,
            string primaryKeyOverride,
            bool autoIncrement,
            bool isNpgSql,
            bool isOdbc)
        {
            bool exists = await TableExistsAsync(tableName, connection, transaction, isNpgSql, isOdbc);
            if (!exists) return;

            var properties = typeof(T).GetProperties()
                .Where(p => p.CanRead && p.CanWrite)
                .ToArray();

            // ── Resolve desired primary key from DTO ──
            string desiredPk = primaryKeyOverride;
            if (string.IsNullOrEmpty(desiredPk))
            {
                var keyProperty = properties.FirstOrDefault(
                    p => Attribute.IsDefined(p, typeof(System.ComponentModel.DataAnnotations.KeyAttribute)));
                if (keyProperty != null)
                    desiredPk = keyProperty.Name;
            }

            // ── Get current state from DB ──
            var existingColumns = await GetExistingColumnsWithTypesAsync(
                tableName, connection, transaction, isNpgSql, isOdbc);

            string currentPk = await GetCurrentPrimaryKeyAsync(
                tableName, connection, transaction, isNpgSql, isOdbc);

            // ── COLUMN SYNC ──
            foreach (var prop in properties)
            {
                if (!existingColumns.ContainsKey(prop.Name))
                {
                    // Column does not exist yet — ADD it
                    await AddColumnAsync(tableName, prop, connection, transaction, isNpgSql, isOdbc);
                    Console.WriteLine($"[{tableName}] Column ADDED: {prop.Name}");
                }
                else
                {
                    // Column exists — check if data type changed
                    string expectedType = GetSqlDataType(prop.PropertyType, isNpgSql, isOdbc);
                    string actualType = existingColumns[prop.Name];

                    if (!IsTypeCompatible(actualType, expectedType))
                    {
                        bool isPkColumn = string.Equals(currentPk, prop.Name, StringComparison.OrdinalIgnoreCase);

                        if (isPkColumn)
                        {
                            // Must drop PK constraint first, alter type, then re-add PK
                            await AlterPrimaryKeyColumnAsync(
                                tableName, prop, currentPk,
                                connection, transaction,
                                autoIncrement, isNpgSql, isOdbc);
                        }
                        else
                        {
                            await AlterColumnAsync(tableName, prop, connection, transaction, isNpgSql, isOdbc);
                        }

                        Console.WriteLine($"[{tableName}] Column ALTERED: {prop.Name}  ({actualType} -> {expectedType})");
                    }
                }
            }

            // ── PRIMARY KEY SYNC ──
            // Re-fetch currentPk in case it was already changed by AlterPrimaryKeyColumnAsync above
            currentPk = await GetCurrentPrimaryKeyAsync(
                tableName, connection, transaction, isNpgSql, isOdbc);

            bool dbHasPk = !string.IsNullOrEmpty(currentPk);
            bool dtoHasPk = !string.IsNullOrEmpty(desiredPk);

            if (dbHasPk && dtoHasPk)
            {
                // Both have PK — check if it moved to a different column
                if (!string.Equals(currentPk, desiredPk, StringComparison.OrdinalIgnoreCase))
                {
                    await ReassignPrimaryKeyAsync(
                        tableName, desiredPk, currentPk,
                        connection, transaction,
                        isNpgSql, isOdbc);

                    Console.WriteLine($"[{tableName}] PRIMARY KEY changed: {currentPk} -> {desiredPk}");
                }
                // else — same PK column, nothing to do
            }
            else if (dbHasPk && !dtoHasPk)
            {
                // DB has PK but [Key] was removed in DTO — drop the PK constraint
                await DropPrimaryKeyAsync(
                    tableName, currentPk,
                    connection, transaction,
                    isNpgSql, isOdbc);

                Console.WriteLine($"[{tableName}] PRIMARY KEY DROPPED: {currentPk}");
            }
            else if (!dbHasPk && dtoHasPk)
            {
                // DB has no PK but [Key] was added in DTO — add the PK constraint
                await AddPrimaryKeyAsync(
                    tableName, desiredPk,
                    connection, transaction,
                    isNpgSql, isOdbc);

                Console.WriteLine($"[{tableName}] PRIMARY KEY ADDED: {desiredPk}");
            }
            // else — no PK in DB and no [Key] in DTO — nothing to do
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET EXISTING COLUMNS WITH TYPES
        // ─────────────────────────────────────────────────────────────────────

        private async Task<Dictionary<string, string>> GetExistingColumnsWithTypesAsync(
            string tableName,
            IDbConnection connection,
            IDbTransaction transaction,
            bool isNpgSql,
            bool isOdbc)
        {
            var columns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            string query;
            if (isNpgSql)
                query = "SELECT column_name, data_type FROM information_schema.columns WHERE table_name = @tableName";
            else if (isOdbc)
                query = "SELECT column_name, data_type FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = ?";
            else
                query = "SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @tableName";

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
                    using(var reader = await dbCommand.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                            columns[reader.GetString(0)] = reader.GetString(1);
                    }
               
                }
                else
                {
                    using(var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                            columns[reader.GetString(0)] = reader.GetString(1);
                    }
 
                }
            }

            return columns;
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET CURRENT PRIMARY KEY COLUMN
        // ─────────────────────────────────────────────────────────────────────

        private async Task<string> GetCurrentPrimaryKeyAsync(
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
                    SELECT kcu.column_name
                    FROM information_schema.table_constraints tc
                    JOIN information_schema.key_column_usage kcu
                        ON tc.constraint_name = kcu.constraint_name
                        AND tc.table_name = kcu.table_name
                    WHERE tc.constraint_type = 'PRIMARY KEY'
                      AND tc.table_name = @tableName
                    LIMIT 1";
            }
            else if (isOdbc) // MySQL
            {
                query = @"
                    SELECT column_name
                    FROM information_schema.key_column_usage
                    WHERE table_schema = DATABASE()
                      AND table_name = ?
                      AND constraint_name = 'PRIMARY'
                    LIMIT 1";
            }
            else // SQL Server
            {
                query = @"
                    SELECT col.COLUMN_NAME
                    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                    JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE col
                        ON tc.CONSTRAINT_NAME = col.CONSTRAINT_NAME
                    WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
                      AND tc.TABLE_NAME = @tableName";
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

                return result?.ToString();
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // ADD COLUMN
        // ─────────────────────────────────────────────────────────────────────

        private async Task AddColumnAsync(
            string tableName,
            PropertyInfo property,
            IDbConnection connection,
            IDbTransaction transaction,
            bool isNpgSql,
            bool isOdbc)
        {
            string columnName = isNpgSql || isOdbc ? property.Name : $"[{property.Name}]";
            string dataType = GetSqlDataType(property.PropertyType, isNpgSql, isOdbc);
            string nullable = IsNullableType(property.PropertyType) ? "NULL" : "NOT NULL";

            string alterQuery;
            if (isNpgSql)
                alterQuery = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {dataType} {nullable}";
            else if (isOdbc)
                alterQuery = $"ALTER TABLE {tableName} ADD {property.Name} {dataType} {nullable}";
            else
                alterQuery = $"ALTER TABLE [{tableName}] ADD {columnName} {dataType} {nullable}";

            await ExecuteNonQueryAsync(alterQuery, connection, transaction);
        }

        // ─────────────────────────────────────────────────────────────────────
        // ALTER COLUMN  (non-PK)
        // ─────────────────────────────────────────────────────────────────────

        private async Task AlterColumnAsync(
            string tableName,
            PropertyInfo property,
            IDbConnection connection,
            IDbTransaction transaction,
            bool isNpgSql,
            bool isOdbc)
        {
            string dataType = GetSqlDataType(property.PropertyType, isNpgSql, isOdbc);
            string nullable = IsNullableType(property.PropertyType) ? "NULL" : "NOT NULL";

            string alterQuery;
            if (isNpgSql)
                alterQuery = $"ALTER TABLE {tableName} ALTER COLUMN {property.Name} TYPE {dataType} USING {property.Name}::{dataType}";
            else if (isOdbc)
                alterQuery = $"ALTER TABLE {tableName} MODIFY COLUMN {property.Name} {dataType} {nullable}";
            else
                alterQuery = $"ALTER TABLE [{tableName}] ALTER COLUMN [{property.Name}] {dataType} {nullable}";

            await ExecuteNonQueryAsync(alterQuery, connection, transaction);
        }

        // ─────────────────────────────────────────────────────────────────────
        // ALTER PRIMARY KEY COLUMN  (drop PK -> alter type -> re-add PK)
        // ─────────────────────────────────────────────────────────────────────

        private async Task AlterPrimaryKeyColumnAsync(
            string tableName,
            PropertyInfo property,
            string currentPkColumn,
            IDbConnection connection,
            IDbTransaction transaction,
            bool autoIncrement,
            bool isNpgSql,
            bool isOdbc)
        {
            string dataType = GetSqlDataType(property.PropertyType, isNpgSql, isOdbc);

            if (isNpgSql)
            {
                string constraintName = $"{tableName}_pkey";

                await ExecuteNonQueryAsync(
                    $"ALTER TABLE {tableName} DROP CONSTRAINT IF EXISTS {constraintName}",
                    connection, transaction);

                await ExecuteNonQueryAsync(
                    $"ALTER TABLE {tableName} ALTER COLUMN {property.Name} TYPE {dataType} USING {property.Name}::{dataType}",
                    connection, transaction);

                await ExecuteNonQueryAsync(
                    $"ALTER TABLE {tableName} ADD PRIMARY KEY ({property.Name})",
                    connection, transaction);
            }
            else if (isOdbc) // MySQL
            {
                await ExecuteNonQueryAsync(
                    $"ALTER TABLE {tableName} DROP PRIMARY KEY",
                    connection, transaction);

                string nullable = "NOT NULL";
                string autoInc = (autoIncrement && IsIntegerType(property.PropertyType)) ? " AUTO_INCREMENT" : "";

                await ExecuteNonQueryAsync(
                    $"ALTER TABLE {tableName} MODIFY COLUMN {property.Name} {dataType} {nullable}{autoInc}",
                    connection, transaction);

                await ExecuteNonQueryAsync(
                    $"ALTER TABLE {tableName} ADD PRIMARY KEY ({property.Name})",
                    connection, transaction);
            }
            else // SQL Server
            {
                string pkConstraintName = await GetSqlServerPkConstraintNameAsync(tableName, connection, transaction);

                if (!string.IsNullOrEmpty(pkConstraintName))
                {
                    await ExecuteNonQueryAsync(
                        $"ALTER TABLE [{tableName}] DROP CONSTRAINT [{pkConstraintName}]",
                        connection, transaction);
                }

                await ExecuteNonQueryAsync(
                    $"ALTER TABLE [{tableName}] ALTER COLUMN [{property.Name}] {dataType} NOT NULL",
                    connection, transaction);

                await ExecuteNonQueryAsync(
                    $"ALTER TABLE [{tableName}] ADD PRIMARY KEY ([{property.Name}])",
                    connection, transaction);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // REASSIGN PRIMARY KEY  (move PK from old column to new column)
        // ─────────────────────────────────────────────────────────────────────

        private async Task ReassignPrimaryKeyAsync(
            string tableName,
            string newPkColumn,
            string oldPkColumn,
            IDbConnection connection,
            IDbTransaction transaction,
            bool isNpgSql,
            bool isOdbc)
        {
            if (isNpgSql)
            {
                string constraintName = $"{tableName}_pkey";
                await ExecuteNonQueryAsync(
                    $"ALTER TABLE {tableName} DROP CONSTRAINT IF EXISTS {constraintName}",
                    connection, transaction);
                await ExecuteNonQueryAsync(
                    $"ALTER TABLE {tableName} ADD PRIMARY KEY ({newPkColumn})",
                    connection, transaction);
            }
            else if (isOdbc) // MySQL
            {
                await ExecuteNonQueryAsync(
                    $"ALTER TABLE {tableName} DROP PRIMARY KEY",
                    connection, transaction);
                await ExecuteNonQueryAsync(
                    $"ALTER TABLE {tableName} ADD PRIMARY KEY ({newPkColumn})",
                    connection, transaction);
            }
            else // SQL Server
            {
                string pkConstraintName = await GetSqlServerPkConstraintNameAsync(tableName, connection, transaction);

                if (!string.IsNullOrEmpty(pkConstraintName))
                {
                    await ExecuteNonQueryAsync(
                        $"ALTER TABLE [{tableName}] DROP CONSTRAINT [{pkConstraintName}]",
                        connection, transaction);
                }

                await ExecuteNonQueryAsync(
                    $"ALTER TABLE [{tableName}] ADD PRIMARY KEY ([{newPkColumn}])",
                    connection, transaction);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // DROP PRIMARY KEY
        // ─────────────────────────────────────────────────────────────────────

        private async Task DropPrimaryKeyAsync(
            string tableName,
            string currentPkColumn,
            IDbConnection connection,
            IDbTransaction transaction,
            bool isNpgSql,
            bool isOdbc)
        {
            if (isNpgSql)
            {
                string constraintName = $"{tableName}_pkey";
                await ExecuteNonQueryAsync(
                    $"ALTER TABLE {tableName} DROP CONSTRAINT IF EXISTS {constraintName}",
                    connection, transaction);
            }
            else if (isOdbc) // MySQL
            {
                await ExecuteNonQueryAsync(
                    $"ALTER TABLE {tableName} DROP PRIMARY KEY",
                    connection, transaction);
            }
            else // SQL Server
            {
                string pkConstraintName = await GetSqlServerPkConstraintNameAsync(tableName, connection, transaction);

                if (!string.IsNullOrEmpty(pkConstraintName))
                {
                    await ExecuteNonQueryAsync(
                        $"ALTER TABLE [{tableName}] DROP CONSTRAINT [{pkConstraintName}]",
                        connection, transaction);
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // ADD PRIMARY KEY
        // ─────────────────────────────────────────────────────────────────────

        private async Task AddPrimaryKeyAsync(
            string tableName,
            string pkColumn,
            IDbConnection connection,
            IDbTransaction transaction,
            bool isNpgSql,
            bool isOdbc)
        {
            string sql = isNpgSql
                ? $"ALTER TABLE {tableName} ADD PRIMARY KEY ({pkColumn})"
                : isOdbc
                ? $"ALTER TABLE {tableName} ADD PRIMARY KEY ({pkColumn})"
                : $"ALTER TABLE [{tableName}] ADD PRIMARY KEY ([{pkColumn}])";

            await ExecuteNonQueryAsync(sql, connection, transaction);
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET SQL SERVER PK CONSTRAINT NAME  (shared helper)
        // ─────────────────────────────────────────────────────────────────────

        private async Task<string> GetSqlServerPkConstraintNameAsync(
            string tableName,
            IDbConnection connection,
            IDbTransaction transaction)
        {
            string query = $@"
                SELECT CONSTRAINT_NAME
                FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
                WHERE CONSTRAINT_TYPE = 'PRIMARY KEY'
                  AND TABLE_NAME = '{tableName}'";

            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = query;
                object result = command is DbCommand db
                    ? await db.ExecuteScalarAsync()
                    : command.ExecuteScalar();
                return result?.ToString();
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // BUILD COLUMN DEFINITION  (used during CREATE TABLE)
        // ─────────────────────────────────────────────────────────────────────

        private string BuildColumnDefinition(
            PropertyInfo property,
            bool isPrimaryKey,
            bool autoIncrement,
            bool isNpgSql,
            bool isOdbc)
        {
            string columnName = isNpgSql || isOdbc ? property.Name : $"[{property.Name}]";
            string dataType = GetSqlDataType(property.PropertyType, isNpgSql, isOdbc);
            var parts = new List<string> { columnName, dataType };

            if (isPrimaryKey)
            {
                if (autoIncrement)
                {
                    if (isNpgSql)
                    {
                        if (IsIntegerType(property.PropertyType))
                        {
                            parts[1] = property.PropertyType == typeof(long) ||
                                       property.PropertyType == typeof(long?)
                                       ? "BIGSERIAL" : "SERIAL";
                        }
                        parts.Add("PRIMARY KEY");
                    }
                    else if (isOdbc)
                    {
                        parts.Add("PRIMARY KEY");
                        if (IsIntegerType(property.PropertyType))
                            parts.Add("AUTO_INCREMENT");
                    }
                    else // SQL Server
                    {
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
                if (Attribute.IsDefined(property, typeof(System.ComponentModel.DataAnnotations.RequiredAttribute)))
                    parts.Add("NOT NULL");
                else if (!IsNullableType(property.PropertyType))
                    parts.Add("NOT NULL");
                else
                    parts.Add("NULL");
            }

            return string.Join(" ", parts);
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET SQL DATA TYPE
        // ─────────────────────────────────────────────────────────────────────

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
            else if (isOdbc) // MySQL
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
            else // SQL Server
            {
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

        // ─────────────────────────────────────────────────────────────────────
        // TYPE COMPATIBILITY CHECK
        // ─────────────────────────────────────────────────────────────────────

        private bool IsTypeCompatible(string actualType, string expectedType)
        {
            string Normalize(string t) => t.ToLower()
                .Replace("character varying", "text")
                .Replace("nvarchar", "text")
                .Replace("varchar", "text")
                .Replace("int4", "integer")
                .Replace("int8", "bigint")
                .Replace("int2", "smallint")
                .Trim();

            string actual = Normalize(actualType);
            string expected = Normalize(expectedType);

            return actual == expected ||
                   actual.Contains(expected) ||
                   expected.Contains(actual);
        }

        // ─────────────────────────────────────────────────────────────────────
        // EXECUTE NON QUERY  (shared helper)
        // ─────────────────────────────────────────────────────────────────────

        private async Task ExecuteNonQueryAsync(
            string sql,
            IDbConnection connection,
            IDbTransaction transaction)
        {
            using(var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = sql;
                if (command is DbCommand dbCommand)
                    await dbCommand.ExecuteNonQueryAsync();
                else
                    command.ExecuteNonQuery();
            }
   
        }

        // ─────────────────────────────────────────────────────────────────────
        // TYPE HELPERS
        // ─────────────────────────────────────────────────────────────────────

        private bool IsIntegerType(Type type)
        {
            Type u = Nullable.GetUnderlyingType(type) ?? type;
            return u == typeof(int) ||
                   u == typeof(long) ||
                   u == typeof(short) ||
                   u == typeof(byte);
        }

        private bool IsNullableType(Type type) =>
            !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
    }
}
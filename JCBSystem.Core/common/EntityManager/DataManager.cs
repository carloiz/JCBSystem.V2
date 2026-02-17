using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using JCBSystem.Core.common.Interfaces;
using JCBSystem.Infrastructure.Connection.Interface;
using JCBSystem.Core.common.EntityManager.Handlers;
using System.Data.Common;

namespace JCBSystem.Core.common.EntityManager
{
    public class DataManager : IDataManager
    {

        private readonly IConnectionFactory connectionFactory;
        private readonly IDbConnectionFactory dbConnectionFactory;

        public DataManager(IDbConnectionFactory dbConnectionFactory, IConnectionFactory connectionFactory)
        {
            this.dbConnectionFactory = dbConnectionFactory;
            this.connectionFactory = connectionFactory;
        }

        public Task CommitAndRollbackMethod(Func<IDbConnection, IDbTransaction, Task> action)
        {
            return new TransactionManagerHandler(dbConnectionFactory, connectionFactory).HandleAsync(action);
        }

        public Task<int> DeleteAsync(List<object> filterValues, string tableName, IDbConnection connection, IDbTransaction transaction, string whereConditions = null)
        {
            return new DeleteCommandHandler().HandleAsync(filterValues, tableName, connection, transaction, whereConditions);
        }

        public Task<object> InsertAsync<T>(T entity, string tableName, IDbConnection connection, IDbTransaction transaction, string primaryKeyColumn = "id")
        {
            return new CreateCommandHandler().HandleAsync(entity, tableName, connection, transaction, primaryKeyColumn);    
        }

        public Task<(string, int)> SearchWithPaginatedAsync<T>(List<object> filter, string countQuery, string dataQuery, DataGridView dataGrid, List<string> imageColumns, Dictionary<string, string> customColumnHeaders, int pageNumber = 1, int pageSize = 10) where T : new()
        {
            return new GetQueryHandler(dbConnectionFactory, connectionFactory).HandleAsync<T>(filter, countQuery, dataQuery, dataGrid, imageColumns, customColumnHeaders, pageNumber, pageSize);
        }

        public Task<(string, int)> SelectAllWithPaginatedAsync<T>(string countQuery, string dataQuery, DataGridView dataGrid, List<string> imageColumns, Dictionary<string, string> customColumnHeaders, int pageNumber = 1, int pageSize = 10) where T : new()
        {
            return new GetAllQueryHandler(dbConnectionFactory, connectionFactory).HandleAsync<T>(countQuery, dataQuery, dataGrid, imageColumns, customColumnHeaders, pageNumber, pageSize);
        }

        public Task<int> UpdateAsync<T>(T entity, string tableName, IDbConnection connection, IDbTransaction transaction, string primaryKey = null, string whereCondition = null, List<object> additionalParameters = null)
        {
            return new UpdateCommandHandler().HandleAsync(entity, tableName, connection, transaction, primaryKey, whereCondition, additionalParameters);
        }

        public Task<bool> CreateDatabaseIfNotExistsAsync(IDbConnection connection, string databaseName)
        {
            return new DatabaseSchemaHandler(dbConnectionFactory, connectionFactory).HandleAsync(connection, databaseName);
        }
        public Task CreateAlterTableAsync<T>(string tableName, IDbConnection connection, IDbTransaction transaction = null, string primaryKeyOverride = null, bool autoIncrement = true)
        {
            return new TableSchemaHandler().HandleAsync<T>(tableName, connection, transaction, primaryKeyOverride, autoIncrement);
        }

        public T GetRegistLocalSession<T>() where T : class, new()
        {
            return new RegistryKeysHandler().GetRegistLocalSession<T>();
        }
        public void CreateRegistLocalSession<T>(T regInfo) where T : class
        {
            new RegistryKeysHandler().CreateRegistLocalSession<T>(regInfo);  
        }
        public Task DeleteRegistLocalSession<T>() where T : class, new()
        {
            return new RegistryKeysHandler().DeleteRegistLocalSession<T>();
        }
    }
}

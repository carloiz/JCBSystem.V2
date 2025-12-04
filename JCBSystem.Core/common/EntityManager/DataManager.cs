using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using JCBSystem.Core.common.FormCustomization;
using JCBSystem.Core.common.Interfaces;
using JCBSystem.Infrastructure.Connection.Interface;
using Microsoft.IdentityModel.Tokens;
using JCBSystem.Core.common.EntityManager.Handlers;

namespace JCBSystem.Core.common.EntityManager
{
    public class DataManager : IDataManager
    {

        private readonly Color headerForeColor = Color.White;
        private readonly Color headerBackColor = Color.FromArgb(64, 64, 64);

        private readonly string dateFormat = "dddd, MMMM dd, yyyy hh:mm tt";

        private readonly IConnectionFactorySelector connectionFactorySelector;
        private readonly IDbConnectionFactory dbConnectionFactory;

        public DataManager(IDbConnectionFactory dbConnectionFactory, IConnectionFactorySelector connectionFactorySelector)
        {
            this.dbConnectionFactory = dbConnectionFactory;
            this.connectionFactorySelector = connectionFactorySelector;
        }

        public Task CommitAndRollbackMethod(Func<IDbConnection, IDbTransaction, Task> action)
        {
            return new TransactionManagerHandler(dbConnectionFactory, connectionFactorySelector).HandleAsync(action);
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
            return new GetQueryHandler(dbConnectionFactory, connectionFactorySelector).HandleAsync<T>(filter, countQuery, dataQuery, dataGrid, imageColumns, customColumnHeaders, pageNumber, pageSize);
        }

        public Task<(string, int)> SelectAllWithPaginatedAsync<T>(string countQuery, string dataQuery, DataGridView dataGrid, List<string> imageColumns, Dictionary<string, string> customColumnHeaders, int pageNumber = 1, int pageSize = 10) where T : new()
        {
            return new GetAllQueryHandler(dbConnectionFactory, connectionFactorySelector).HandleAsync<T>(countQuery, dataQuery, dataGrid, imageColumns, customColumnHeaders, pageNumber, pageSize);
        }

        public Task<int> UpdateAsync<T>(T entity, string tableName, IDbConnection connection, IDbTransaction transaction, string primaryKey = null, string whereCondition = null, List<object> additionalParameters = null)
        {
            return new UpdateCommandHandler().HandleAsync(entity, tableName, connection, transaction, primaryKey, whereCondition, additionalParameters);
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

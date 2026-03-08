using JCBSystem.Core.common.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JCBSystem.Core.common.Interfaces
{
    public interface IDataManager
    {
        Task<(string, int)>
          SearchWithPaginatedAsync<T>(QueryRequestWithParams queryRequest) where T : new();

        Task<(string, int)>
            SelectAllWithPaginatedAsync<T>(QueryRequestBase requestBase) where T : new();

        Task<object> InsertAsync<T>(
           T entity,
           string tableName,
           IDbConnection connection,
           IDbTransaction transaction,
           string primaryKeyColumn = "id");


        Task<int> UpdateAsync<T>(
           T entity,
           string tableName,
           IDbConnection connection,
           IDbTransaction transaction,
           string primaryKey = null,
           string whereCondition = null,
           List<object> additionalParameters = null);


        Task<int> DeleteAsync(
            List<object> filterValues,
            string tableName,
            IDbConnection connection,
            IDbTransaction transaction,
            string whereConditions = null);

        Task CommitAndRollbackMethod(Func<IDbConnection, IDbTransaction, Task> action);

        Task<bool> CreateDatabaseIfNotExistsAsync(
             IDbConnection connection,
             string databaseName);

        Task CreateAlterTableAsync<T>(
            string tableName,
            IDbConnection connection,
            IDbTransaction transaction = null);


        T GetRegistLocalSession<T>() where T : class, new();
        Task DeleteRegistLocalSession<T>() where T : class, new();
        void CreateRegistLocalSession<T>(T regInfo) where T : class;

    }
}

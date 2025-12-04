using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JCBSystem.Core.common.Interfaces
{
    public interface IDataManager
    {
        Task<(string, int)>
          SearchWithPaginatedAsync<T>(
              List<object> filter,
              string countQuery,
              string dataQuery,
              DataGridView dataGrid,
              List<string> imageColumns,
              Dictionary<string, string> customColumnHeaders,
              int pageNumber = 1,
              int pageSize = 10
          ) where T : new();

        Task<(string, int)>
            SelectAllWithPaginatedAsync<T>(
                string countQuery,
                string dataQuery,
                DataGridView dataGrid,
                List<string> imageColumns,
                Dictionary<string, string> customColumnHeaders,
                int pageNumber = 1,
                int pageSize = 10
            ) where T : new();

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


        T GetRegistLocalSession<T>() where T : class, new();
        Task DeleteRegistLocalSession<T>() where T : class, new();
        void CreateRegistLocalSession<T>(T regInfo) where T : class;

    }
}

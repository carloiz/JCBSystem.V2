using JCBSystem.Infrastructure.Connection.Interface;
using System;
using System.Data;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JCBSystem.Core.common.EntityManager.Handlers
{
    public class TransactionManagerHandler
    {
        private readonly IConnectionFactory connectionFactorySelector;
        private readonly IDbConnectionFactory dbConnectionFactory;

        public TransactionManagerHandler(IDbConnectionFactory dbConnectionFactory, IConnectionFactory connectionFactorySelector)
        {
            this.dbConnectionFactory = dbConnectionFactory;
            this.connectionFactorySelector = connectionFactorySelector;
        }

        /// <summary>
        /// Processing Transaction Create, Update, Delete
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public async Task HandleAsync(Func<IDbConnection, IDbTransaction, Task> action)
        {
            using (var connection = dbConnectionFactory.CreateConnection())
            {
                await connectionFactorySelector.OpenConnectionAsync(connection);

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        await action(connection, transaction);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback(); // rollback on error
                        MessageBox.Show(
                            $"{ex.Message}",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );
                    }

                    //await action(connection, transaction);
                }
            }
        }
    }
}

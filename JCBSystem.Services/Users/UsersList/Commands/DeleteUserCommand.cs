using JCBSystem.Core.common.FormCustomization;
using JCBSystem.Core.common.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;


namespace JCBSystem.Services.Users.UsersList.Commands
{
    public class DeleteUserCommand
    {
        private readonly IDataManager dataManager;
        private readonly Pagination pagination;

        private string userNumber;

        public DeleteUserCommand(IDataManager dataManager, Pagination pagination) 
        {
            this.dataManager = dataManager;
            this.pagination = pagination;
        }

        public void Initialize(string userNumber)
        {
            this.userNumber = userNumber;   
        }

        public async Task HandlerAsync()
        {
            await dataManager.CommitAndRollbackMethod(async (connection, transaction) =>
            {
                await ProcessDelete(connection, transaction); // Tawagin ang Process method na may transaction at connection
            });
        }

        private async Task ProcessDelete(IDbConnection connection, IDbTransaction transaction)
        {

            string whereCondition = "Usernumber = #";

            await dataManager.DeleteAsync(new List<object> { userNumber }, "Users", connection, transaction, whereCondition);

            transaction.Commit(); // Commit changes  
        }
    }
}

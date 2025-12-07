using JCBSystem.Core.common.FormCustomization;
using JCBSystem.Core.common.Interfaces;
using JCBSystem.LoyTr.Handlers;
using JCBSystem.LoyTr.Interfaces;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JCBSystem.Services.Users.UserManagement.Commands
{
    public class DeleteUserCommand : IRequest
    {
        public string Usernumber { get; set; }
    }

    public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand>
    {
        private readonly IDataManager dataManager;

        public DeleteUserCommandHandler(IDataManager dataManager) 
        {
            this.dataManager = dataManager;
        }

        public async Task HandleAsync(DeleteUserCommand req)
        {
            await dataManager.CommitAndRollbackMethod(async (connection, transaction) =>
            {
                await ProcessDelete(connection, transaction, req); // Tawagin ang Process method na may transaction at connection
            });
        }

        private async Task ProcessDelete(IDbConnection connection, IDbTransaction transaction, DeleteUserCommand req)
        {

            string whereCondition = "Usernumber = #";

            await dataManager.DeleteAsync(new List<object> { req.Usernumber }, "Users", connection, transaction, whereCondition);

            transaction.Commit(); // Commit changes  

            // Display the message for successful shift start
            MessageBox.Show($"Successfully Delete {req.Usernumber} Record.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}

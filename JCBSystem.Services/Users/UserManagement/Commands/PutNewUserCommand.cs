using JCBSystem.Core.common.FormCustomization;
using JCBSystem.Core.common.Helpers;
using JCBSystem.Core.common.Interfaces;
using JCBSystem.Domain.DTO.Users;
using JCBSystem.LoyTr.Handlers;
using JCBSystem.LoyTr.Interfaces;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JCBSystem.Services.Users.UserManagement.Commands
{
    public class PutNewUserCommand : ILoyTrRequest
    {
        public Form Form {  get; set; }
        public string KeyUsernumber { get; set; }
        public string KeyUsername { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string UserLevel { get; set; }
    }

    public class PutNewUserCommandHandler : ILoyTrHandler<PutNewUserCommand>
    {
        private readonly IDataManager dataManager;
        private readonly ILogicsManager logicsManager;

        public PutNewUserCommandHandler(IDataManager dataManager, ILogicsManager logicsManager)
        {
            this.dataManager = dataManager;
            this.logicsManager = logicsManager;
        }

        public async Task HandleAsync(PutNewUserCommand req)
        {
            bool isExist = await logicsManager.CheckIfRecordExists(
              new List<object> { req.KeyUsername, req.Username },
              "Users",
              "Username NOT LIKE # AND Username = #"
          );

            if (isExist)
            {
                MessageBox.Show(
                    "Username Already Exist.",
                    "",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            await dataManager.CommitAndRollbackMethod(async (connection, transaction) =>
            {
                await ProcessUpdate(connection, transaction, req); 
            });
        }


        private async Task ProcessUpdate(IDbConnection connection, IDbTransaction transaction, PutNewUserCommand req)
        {
            string hashPassword = PasswordHelper.HashPassword(req.Password);

            var userUpdateDto = new UsersDto
            {
                UserNumber = req.KeyUsernumber,
                Username = req.Username,
                Password = hashPassword,
                UserLevel = req.UserLevel,
            };

            await dataManager.UpdateAsync(
                entity: userUpdateDto,
                tableName: "Users",
                connection: connection,
                transaction: transaction,
                primaryKey: "UserNumber"
            );


            transaction.Commit(); // Commit changes  

            // Display the message for successful shift start
            MessageBox.Show($"Successfully Update {req.Username} Record.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

            FormHelper.CloseFormWithFade(req.Form, true);
        }
    }
}

using JCBSystem.Core.common.CRUD;
using JCBSystem.Core.common.FormCustomization;
using JCBSystem.Core.common.Helpers;
using JCBSystem.Core.common.Interfaces;
using JCBSystem.Core.common.Logics;
using JCBSystem.Domain.DTO.Users;
using JCBSystem.LoyTr.Handlers;
using JCBSystem.LoyTr.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
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
        private readonly CheckIfRecordExists checkIfRecordExists;

        public PutNewUserCommandHandler(IDataManager dataManager, CheckIfRecordExists checkIfRecordExists)
        {
            this.dataManager = dataManager;
            this.checkIfRecordExists = checkIfRecordExists;
        }

        public async Task HandleAsync(PutNewUserCommand req)
        {
            bool isExist = await checkIfRecordExists.ExecuteAsync(
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
                await ProcessUpdate(connection, transaction, req.Form, req.KeyUsernumber, req.Username, req.Password, req.UserLevel); 
            });
        }


        private async Task ProcessUpdate(IDbConnection connection, IDbTransaction transaction, Form form, string keyUsernumber, string username, string password, string userLevel)
        {
            string hashPassword = PasswordHelper.HashPassword(password);

            var userUpdateDto = new UsersDto
            {
                UserNumber = keyUsernumber,
                Username = username,
                Password = hashPassword,
                UserLevel = userLevel,
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
            MessageBox.Show($"Successfully Update {username} Record.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

            FormHelper.CloseFormWithFade(form, true);
        }
    }
}

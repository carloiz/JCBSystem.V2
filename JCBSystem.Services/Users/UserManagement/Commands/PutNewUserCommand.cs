using JCBSystem.Core.common.CRUD;
using JCBSystem.Core.common.FormCustomization;
using JCBSystem.Core.common.Helpers;
using JCBSystem.Core.common.Interfaces;
using JCBSystem.Core.common.Logics;
using JCBSystem.Domain.DTO.Users;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JCBSystem.Services.Users.UserManagement.Commands
{
    public class PutNewUserCommand
    {
        private readonly IDataManager dataManager;
        private readonly CheckIfRecordExists checkIfRecordExists;

        private string keyUsernumber;
        private string keyUsername;
        private string username;
        private string password;
        private string userLevel;
        private Form form;

        public PutNewUserCommand(IDataManager dataManager, CheckIfRecordExists checkIfRecordExists)
        {
            this.dataManager = dataManager;
            this.checkIfRecordExists = checkIfRecordExists;
        }

        public void Initialize(Form form, string keyUsernumber, string keyUsername,string username, string password, string userLevel)
        {
            this.keyUsernumber = keyUsernumber;
            this.keyUsername = keyUsername;
            this.username = username;
            this.password = password;
            this.userLevel = userLevel;
            this.form = form;   
        }

        public async Task HandleAsync()
        {
            bool isExist = await checkIfRecordExists.ExecuteAsync(
              new List<object> { keyUsername, username },
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
                await ProcessUpdate(connection, transaction); // Tawagin ang Process method na may transaction at connection
            });
        }


        private async Task ProcessUpdate(IDbConnection connection, IDbTransaction transaction)
        {
            string password = PasswordHelper.HashPassword(this.password);

            var userUpdateDto = new UsersDto
            {
                UserNumber = keyUsernumber,
                Username = username,
                Password = password,
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

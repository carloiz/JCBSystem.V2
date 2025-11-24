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
    public class PostNewUserCommand
    {
        private readonly IDataManager dataManager;
        private readonly CheckIfRecordExists checkIfRecordExists;
        private readonly GenerateNextValues generateNextValues;

        private string userName;
        private string userPassword;
        private string userLevel;
        private Form form;

        public PostNewUserCommand(IDataManager dataManager, CheckIfRecordExists checkIfRecordExists, GenerateNextValues generateNextValues)
        {
            this.dataManager = dataManager;
            this.checkIfRecordExists = checkIfRecordExists;
            this.generateNextValues = generateNextValues;
        }

        public void Initialize(Form form, string userName, string userPassword, string userLevel)
        {
            this.userName = userName;
            this.userPassword = userPassword;
            this.userLevel = userLevel;
            this.form = form;
        }

        public async Task HandlerAsync()
        {
            bool isExist = await checkIfRecordExists.ExecuteAsync(
                new List<object> { userName },
                "Users",
                "Username = #"
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
                await ProcessCreate(connection, transaction); // Tawagin ang Process method na may transaction at connection
            });
        }


        private async Task ProcessCreate(IDbConnection connection, IDbTransaction transaction)
        {
            string password = PasswordHelper.HashPassword(userPassword);

            string userId = await generateNextValues.ByIdAsync("Users", "UserNumber", "U");

            DateTime dateToday = SystemDate.GetPhilippineTime();

            var userCreateDto = new UsersDto
            {
                UserNumber = userId,
                Username = userName,
                Password = password,
                UserLevel = userLevel,
                Status = true,
                IsSessionActive = false,
                CurrentToken = null,
                RecordDate = dateToday
            };

            await dataManager.InsertAsync(userCreateDto, "Users", connection, transaction, "UserNumber");

            transaction.Commit(); // Commit changes  


            MessageBox.Show("Successfully Add New Record.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

            FormHelper.CloseFormWithFade(form, true);

        }
    }
}

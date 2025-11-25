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
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JCBSystem.Services.Users.UserManagement.Commands
{
    public class PostNewUserCommand : ILoyTrRequest
    {
        public Form Form {  get; set; }
        public string Username { get; set; }
        public string UserPassword { get; set; }
        public string UserLevel { get; set; }
    }

    public class PostNewUserCommandHandler : ILoyTrHandler<PostNewUserCommand>
    {
        private readonly IDataManager dataManager;
        private readonly CheckIfRecordExists checkIfRecordExists;
        private readonly GenerateNextValues generateNextValues;

        public PostNewUserCommandHandler(IDataManager dataManager, CheckIfRecordExists checkIfRecordExists, GenerateNextValues generateNextValues)
        {
            this.dataManager = dataManager;
            this.checkIfRecordExists = checkIfRecordExists;
            this.generateNextValues = generateNextValues;
        }

        public async Task HandleAsync(PostNewUserCommand request)
        {
            bool isExist = await checkIfRecordExists.ExecuteAsync(
                new List<object> { request.Username },
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
                await ProcessCreate(connection, transaction, request.Form, request.Username, request.UserPassword, request.UserLevel);
            });
        }


        private async Task ProcessCreate(IDbConnection connection, IDbTransaction transaction, Form form, string userName, string userPassword, string userLevel)
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

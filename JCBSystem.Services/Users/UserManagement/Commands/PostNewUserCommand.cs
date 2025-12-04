using JCBSystem.Core.common.FormCustomization;
using JCBSystem.Core.common.Helpers;
using JCBSystem.Core.common.Interfaces;
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
    public class PostNewUserCommand : IRequest
    {
        public Form Form {  get; set; }
        public string Username { get; set; }
        public string UserPassword { get; set; }
        public string UserLevel { get; set; }
    }

    public class PostNewUserCommandHandler : IRequestHandler<PostNewUserCommand>
    {
        private readonly IDataManager dataManager;
        private readonly ILogicsManager logicsManager;

        public PostNewUserCommandHandler(IDataManager dataManager, ILogicsManager logicsManager)
        {
            this.dataManager = dataManager;
            this.logicsManager = logicsManager;
        }

        public async Task HandleAsync(PostNewUserCommand req)
        {
            bool isExist = await logicsManager.CheckIfRecordExists(
                new List<object> { req.Username },
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
                await ProcessCreate(connection, transaction, req);
            });
        }


        private async Task ProcessCreate(IDbConnection connection, IDbTransaction transaction, PostNewUserCommand req)
        {
            string password = PasswordHelper.HashPassword(req.UserPassword);

            string userId = await logicsManager.GenerateNextValuesByIdAsync("Users", "UserNumber", "U");

            DateTime dateToday = SystemDate.GetPhilippineTime();

            var userCreateDto = new UsersDto
            {
                UserNumber = userId,
                Username = req.Username,
                Password = password,
                UserLevel = req.UserLevel,
                Status = true,
                IsSessionActive = false,
                CurrentToken = null,
                RecordDate = dateToday
            };

            await dataManager.InsertAsync(userCreateDto, "Users", connection, transaction, "UserNumber");

            transaction.Commit(); // Commit changes  


            MessageBox.Show("Successfully Add New Record.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

            FormHelper.CloseFormWithFade(req.Form, true);

        }
    }
}

using JCBSystem.Core.common.CRUD;
using JCBSystem.Core.common.Helpers;
using JCBSystem.Core.common.Interfaces;
using JCBSystem.Core.common.Logics;
using JCBSystem.Domain.DTO.Auth;
using JCBSystem.Domain.DTO.Users;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JCBSystem.Services.MainDashboard.Queries
{
    public class GetSessionQuery
    {
        private readonly IDataManager dataManager;
        private readonly ISessionManager sessionManager;
        private readonly RegistryKeys registryKeys;
        private readonly CheckIfRecordExists checkIfRecordExists;

        public GetSessionQuery(IDataManager dataManager, ISessionManager sessionManager, RegistryKeys registryKeys, CheckIfRecordExists checkIfRecordExists)
        {
            this.dataManager = dataManager;
            this.sessionManager = sessionManager;
            this.registryKeys = registryKeys;
            this.checkIfRecordExists = checkIfRecordExists;
        }

        public async Task HandleAsync()
        {
            await dataManager.CommitAndRollbackMethod(async (connection, transaction) =>
            {
                await ProcessSession(connection, transaction); // Tawagin ang Process method na may transaction at connection
            });
        }

        private async Task ProcessSession(IDbConnection connection, IDbTransaction transaction)
        {
            var userRegistInfo = registryKeys.GetRegistLocalSession<RegistUserDto>();

            string token = userRegistInfo.AuthToken;
            string usernumber = userRegistInfo.UserNumber;
            string userlevel = userRegistInfo.UserLevel;

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(usernumber) || string.IsNullOrEmpty(userlevel))
            {
                sessionManager.OnUserLog();
                return;
            }

            token = await DataProtectorHelper.Unprotect(token);
            usernumber = await DataProtectorHelper.Unprotect(usernumber);
            userlevel = await DataProtectorHelper.Unprotect(userlevel);

            if (JwtTokenHelper.IsTokenExpired(token))
            {
                bool isExist = await checkIfRecordExists.ExecuteAsync(
                    new List<object> { usernumber },
                    "Users",
                    "UserNumber = # AND IsSessionActive = true"
                );

                if (!isExist)
                {
                    sessionManager.OnUserLog();
                    throw new KeyNotFoundException("User not found in Session.");
                }

                var userDto = new UserUpdateDto
                {
                    UserNumber = usernumber, // always have this for Primary Key
                    IsSessionActive = false,
                    CurrentToken = null
                };

                await dataManager.UpdateAsync(
                    entity: userDto,
                    tableName: "Users",
                    connection: connection,
                    transaction: transaction,
                    primaryKey: "UserNumber"
                );

                await registryKeys.DeleteRegistLocalSession<RegistUserDto>();

                sessionManager.OnUserLog();

                transaction.Commit(); // Commit changes

                Console.WriteLine("Token Expired", "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            sessionManager.OnUserLog(true,  usernumber);
        }
    }
}

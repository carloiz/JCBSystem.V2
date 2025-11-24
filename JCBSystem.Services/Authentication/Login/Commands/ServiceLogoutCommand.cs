using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using JCBSystem.Core.common.CRUD;
using JCBSystem.Core.common.Helpers;
using JCBSystem.Core.common.Interfaces;
using JCBSystem.Core.common.Logics;
using JCBSystem.Domain.DTO.Auth;
using JCBSystem.Domain.DTO.Users;


namespace JCBSystem.Services.Authentication.Login.Commands
{
    public class ServiceLogoutCommand
    {
        private readonly IDataManager dataManager;
        private readonly ISessionManager sessionManager;
        private readonly RegistryKeys registryKeys;
        private readonly GetFieldsValues getFieldsValues;

        public ServiceLogoutCommand(IDataManager dataManager, ISessionManager sessionManager, RegistryKeys registryKeys, GetFieldsValues getFieldsValues)
        {
            this.dataManager = dataManager;
            this.sessionManager = sessionManager;
            this.registryKeys = registryKeys;
            this.getFieldsValues = getFieldsValues;
        }

        public async Task HandleAsync()
        {
            await dataManager.CommitAndRollbackMethod(async (connection, transaction) =>
            {
                await ProcessLogout(connection, transaction); // Tawagin ang Process method na may transaction at connection
            });
        }

        private async Task ProcessLogout(IDbConnection connection, IDbTransaction transaction)
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


            Dictionary<string, object> GetValues = await
                getFieldsValues.ExecuteAsync(
                    new List<object> { usernumber }, // Parameters
                    "Users",
                    new List<string> { "UserNumber", "IsSessionActive", "CurrentToken" }, // this is for like SUM(Quantity) As TotalQuantity
                    new List<string> { "UserNumber", "IsSessionActive", "CurrentToken" }, // this is fix where the name of field
                    "UserNumber = #"
                );

            string userNumber =
                GetValues.ContainsKey("UserNumber") &&
                !string.IsNullOrEmpty(GetValues["UserNumber"]?.ToString())
                ? Convert.ToString(GetValues["UserNumber"])
                : string.Empty;

            string userToken =
                GetValues.ContainsKey("CurrentToken") &&
                !string.IsNullOrEmpty(GetValues["CurrentToken"]?.ToString())
                ? Convert.ToString(GetValues["CurrentToken"])
                : string.Empty;

            bool userSession =
                GetValues.ContainsKey("IsSessionActive") &&
                !string.IsNullOrEmpty(GetValues["IsSessionActive"]?.ToString())
                ? Convert.ToBoolean(GetValues["IsSessionActive"])
                : false;


            if (userNumber == null)
            {
                throw new KeyNotFoundException("User not found.");
            }

            if (userSession == false)
            {
                throw new KeyNotFoundException("The user is already inactive.");
            }

            if (!PasswordHelper.VerifyPassword(token, userToken))
            {
                // Optionally, handle the case where the token does not match
                throw new KeyNotFoundException("Token does not match.");
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


            // Call the method to delete registry values
            await registryKeys.DeleteRegistLocalSession<RegistUserDto>();


            transaction.Commit(); // Commit changes
            sessionManager.OnUserLog();

        }
    }
}

using JCBSystem.Core.common.FormCustomization;
using JCBSystem.Core.common.Helpers;
using JCBSystem.Core.common.Interfaces;
using JCBSystem.Domain.DTO.Auth;
using JCBSystem.Domain.DTO.Users;
using JCBSystem.LoyTr.Handlers;
using JCBSystem.LoyTr.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace JCBSystem.Services.Authentication.Login.Commands
{
    public class ServiceLoginCommand : IRequest 
    { 
        public string Username { get; set; }    
        public string Password { get; set; }    
        public Form Form { get; set; }    
    }

    public class ServiceLoginCommandHandler : IRequestHandler<ServiceLoginCommand>
    {
        private readonly ILogicsManager logicsManager;
        private readonly IDataManager dataManager;
        private readonly ISessionManager sessionManager;


        public ServiceLoginCommandHandler(ILogicsManager logicsManager, IDataManager dataManager, ISessionManager sessionManager)
        {
            this.logicsManager = logicsManager;
            this.dataManager = dataManager;
            this.sessionManager = sessionManager;
        }

        public async Task HandleAsync(ServiceLoginCommand req)
        {
            await dataManager.CommitAndRollbackMethod(async (connection, transaction) =>
            {
                await Process(connection, transaction, req); // Tawagin ang Process method na may transaction at connection
            });
        }

        private async Task Process(IDbConnection connection, IDbTransaction transaction, ServiceLoginCommand req)
        {
            var (userLoggedIn, existingToken, usernumber) = await IsUserLoggedIn();

            // Check if the user is already authenticated
            if (userLoggedIn)
            {
                if (!JwtTokenHelper.IsTokenExpired(existingToken))
                {
                    throw new ArgumentNullException("Login Failed, User is already authenticated: Local Session");
                }

                Console.WriteLine("Token Already Expired.");
            }

            Dictionary<string, object> GetValues = await
                logicsManager.GetFieldsValues(
                    new List<object> { req.Username }, // Parameters
                    "Users",
                    new List<string> { "Password", "IsSessionActive", "Status", "UserLevel", "UserNumber" }, // this is for like SUM(Quantity) As TotalQuantity
                    new List<string> { "Password", "IsSessionActive", "Status", "UserLevel", "UserNumber" }, // this is fix where the name of field
                    "Username = #"
                );


            string userPassword = GetValues.TryGetValue("Password", out var temp) ? temp?.ToString() : null;

            string userNumber = GetValues.TryGetValue("UserNumber", out var num) && num != null ? num.ToString() : string.Empty;

            string userLevel = GetValues.TryGetValue("UserLevel", out var lvl) && lvl != null ? lvl.ToString() : string.Empty;

            bool userSession = GetValues.TryGetValue("IsSessionActive", out var session) && bool.TryParse(session?.ToString(), out var sessVal) ? sessVal : false;

            bool userStatus =
                GetValues.ContainsKey("Status") &&
                !string.IsNullOrEmpty(GetValues["Status"]?.ToString())
                ? Convert.ToBoolean(GetValues["Status"])
                : false;


            if (userPassword == null || !PasswordHelper.VerifyPassword(req.Password, userPassword))
            {
                throw new Exception("Login Failed, Incorrect Username or Password");
            }


            // Check if the user's database session is already active
            if (userSession && userLoggedIn == false)
            {
                throw new ArgumentNullException("Login Failed, User is already authenticated: Database Session");
            }

            // Check User Status
            if (!userStatus)
            {
                throw new ArgumentNullException("Login failed: User has been deactivated.");
            }

            Dictionary<string, string> keyValues = new Dictionary<string, string>
            {
                { "Username", req.Username },
                { "UserLevel", userLevel }
            };

            var tokenString = JwtTokenHelper.GetJWTToken(keyValues);

            /////// FOR PRIMARY KEY ONLY 1 DATA UPDATE
            var userDto = new UsersDto
            {
                UserNumber = userNumber, // always have this for Primary Key
                IsSessionActive = true,
                CurrentToken = PasswordHelper.HashPassword(tokenString)
            };

            await dataManager.UpdateAsync(
                entity: userDto,
                tableName: "Users",
                connection: connection,
                transaction: transaction,
                primaryKey: "UserNumber"
            );


            // Write to the registry
            var userRegistInfo = new RegistUserDto
            {
                AuthToken = await DataProtectorHelper.Protect(tokenString),
                UserNumber = await DataProtectorHelper.Protect(userNumber),
                UserLevel = await DataProtectorHelper.Protect(userLevel),
            };

            dataManager.CreateRegistLocalSession(userRegistInfo);

            transaction.Commit(); // Commit changes


            sessionManager.OnUserLog(true, userNumber);

            FormHelper.CloseFormWithFade(req.Form);
        }

        private async Task<(bool, string, string)> IsUserLoggedIn()
        {

            var userRegistInfo = dataManager.GetRegistLocalSession<RegistUserDto>();

            if (userRegistInfo != null &&
                !string.IsNullOrEmpty(userRegistInfo.AuthToken) &&
                !string.IsNullOrEmpty(userRegistInfo.UserNumber) &&
                !string.IsNullOrEmpty(userRegistInfo.UserLevel))
            {
                try
                {
                    string token = await DataProtectorHelper.Unprotect(userRegistInfo.AuthToken);
                    string usernumber = await DataProtectorHelper.Unprotect(userRegistInfo.UserNumber);
                    string userlevel = await DataProtectorHelper.Unprotect(userRegistInfo.UserLevel);

                    return (true, token, usernumber);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Decryption failed: {ex.Message}");
                }
            }

            Console.WriteLine("User is not authenticated.");
            return (false, null, null);
        }
    }
}

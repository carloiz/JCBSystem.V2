using JCBSystem.Core.common.Interfaces;
using JCBSystem.Domain.DTO.Users;
using JCBSystem.LoyTr.Handlers;
using JCBSystem.LoyTr.Interfaces;
using JCBSystem.Services.Users.UserManagement.Commands;
using System.Data;
using System.Threading.Tasks;

namespace JCBSystem.Infrastructure.Data.Seeders
{
    public class UserSeederHandler : IRequest { }
    
    public class UserSeederCommandHandler : IRequestHandler<UserSeederHandler>
    {

        private readonly IDataManager dataManager;

        public UserSeederCommandHandler(IDataManager dataManager)
        {
            this.dataManager = dataManager;
        }

        public async Task HandleAsync(UserSeederHandler req)
        {
            await dataManager.CommitAndRollbackMethod(async (connection, transaction) =>
            {
                await ProcessCreate(connection, transaction);
            });
        }


        private async Task ProcessCreate(IDbConnection connection, IDbTransaction transaction)
        {
            await dataManager.CreateAlterTableAsync<UsersDto>(
                tableName: "Users",
                connection: connection,
                transaction: transaction
            );

            transaction.Commit(); // Commit changes  

        }
    }
}

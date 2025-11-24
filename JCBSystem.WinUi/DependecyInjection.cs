using JCBSystem.Core;
using JCBSystem.Core.common.CRUD;
using JCBSystem.Core.common.FormCustomization;
using JCBSystem.Core.common.Interfaces;
using JCBSystem.Core.common.Logics;
using JCBSystem.Infrastructure.Connection;
using JCBSystem.Infrastructure.Connection.Interface;
using JCBSystem.Services.Authentication.Login.Commands;
using JCBSystem.Services.MainDashboard.Queries;
using JCBSystem.Services.Users.UserManagement.Commands;
using JCBSystem.Services.Users.UsersList.Commands;
using JCBSystem.Services.Users.UsersList.Queries;
using JCBSystem.Users;
using Microsoft.Extensions.DependencyInjection;

namespace JCBSystem.WinUi
{
    public static class DependencyInjection
    {
        public static ServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // ✅ Connection factory selector (handles which DB type to use)
            services.AddSingleton<IConnectionFactorySelector, ConnectionFactorySelector>();

            // ✅ Register a single default factory using the selector
            services.AddScoped<IDbConnectionFactory>(sp =>
            {
                // kunin ang selector from DI
                var selector = sp.GetRequiredService<IConnectionFactorySelector>();

                // gamitin ang async result synchronously (safe since startup)
                var factoryTask = selector.GetFactory();
                factoryTask.Wait();
                return factoryTask.Result;
            });

            // ✅ Shared helpers
            services.AddSingleton<FormFactory>();

            // ✅ Logic layer services
            services.AddScoped<IDataManager, DataManager>();
            services.AddScoped<CheckIfRecordExists>();
            services.AddScoped<GenerateNextValues>();
            services.AddScoped<GetComboBoxAttributes>();
            services.AddScoped<GetFieldsValues>();
            services.AddScoped<LoadDataToTextBoxes>();
            services.AddScoped<RegistryKeys>();
            services.AddScoped<Pagination>();

            // ✅ Forms
            services.AddSingleton<ISessionManager, SessionManager>();
            services.AddScoped<MainForm>();
            services.AddScoped<LoginForm>();
            services.AddScoped<UserManagementForm>();
            services.AddScoped<UsersListForm>();


            // Services
            services.AddScoped<ServiceLoginCommand>();
            services.AddScoped<ServiceLogoutCommand>();
            services.AddScoped<GetSessionQuery>();
            services.AddScoped<DeleteUserCommand>();
            services.AddScoped<GetAllUserQuery>();
            services.AddScoped<PostNewUserCommand>();
            services.AddScoped<PutNewUserCommand>();


            return services.BuildServiceProvider();
        }
    }
}

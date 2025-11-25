using JCBSystem.Core;
using JCBSystem.Core.common.CRUD;
using JCBSystem.Core.common.FormCustomization;
using JCBSystem.Core.common.Interfaces;
using JCBSystem.Core.common.Logics;
using JCBSystem.Infrastructure.Connection;
using JCBSystem.Infrastructure.Connection.Interface;
using JCBSystem.Login;
using JCBSystem.LoyTr;
using JCBSystem.Users;
using JCBSystem.WinUi.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace JCBSystem.WinUi
{
    public static class DependencyInjection
    {
        public static ServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // LoyTr
            services.AddLoyTrFromNamespace("JCBSystem", enableLogging: true);

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

            services.AddScoped<ILogicsManager, LogicsManager>();

            services.AddScoped<RegistryKeys>();
            services.AddScoped<Pagination>();

            // ✅ Shared Service
            services.AddSingleton<ISessionManager, SessionManager>();
            services.AddSingleton<TabController>();
            // ✅ Forms
            services.AddScoped<MainForm>();
            services.AddScoped<LoginForm>();
            services.AddScoped<UserManagementForm>();
            services.AddScoped<UsersListForm>();


            return services.BuildServiceProvider();
        }
    }
}

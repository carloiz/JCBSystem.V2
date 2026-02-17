using JCBSystem.Core.common.EntityManager;
using JCBSystem.Core.common.FormCustomization;
using JCBSystem.Core.common.Interfaces;
using JCBSystem.Core.common.Logics;
using JCBSystem.Infrastructure.Connection;
using JCBSystem.Infrastructure.Connection.Interface;
using JCBSystem.LoyTr;
using JCBSystem.WinUi.Shared;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Forms;

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
            services.AddSingleton<IConnectionFactory, ConnectionFactory>();

            // ✅ Register a single default factory using the selector
            services.AddScoped<IDbConnectionFactory>(sp =>
            {
                // kunin ang selector from DI
                var selector = sp.GetRequiredService<IConnectionFactory>();

                // gamitin ang async result synchronously (safe since startup)
                var factoryTask = selector.GetFactory();
                factoryTask.Wait();
                return factoryTask.Result;
            });

            // ✅ Logic layer services
            services.AddScoped<IDataManager, DataManager>();
            services.AddScoped<ILogicsManager, LogicsManager>();

            // ✅ Shared Service
            services.AddSingleton<ISessionManager, SessionManager>();
            services.AddSingleton<TabController>();
            // ✅ Forms
            //services.AddTransient<MainForm>();
            //services.AddTransient<LoginForm>();
            //services.AddTransient<UserManagementForm>();
            //services.AddTransient<UsersListForm>();
            services.Scan(scan => scan
                .FromApplicationDependencies(asm =>
                    !asm.IsDynamic &&
                    !asm.FullName.StartsWith("System") &&
                    !asm.FullName.StartsWith("Microsoft"))
                .AddClasses(c => c.AssignableTo<Form>())    // ← detect all WinForms
                    .AsSelf()
                    .WithTransientLifetime()
            );


            return services.BuildServiceProvider();
        }
    }
}

using JCBSystem.Core.common.EntityManager;
using JCBSystem.Core.common.Interfaces;
using JCBSystem.Infrastructure.Connection;
using JCBSystem.Infrastructure.Connection.Interface;
using JCBSystem.Services.Data.Seeders;
using Microsoft.Extensions.DependencyInjection;
using System;


namespace JCBSystem.DbMIgrationTool
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // 1️⃣ Setup DI container
            var services = new ServiceCollection();

            // 2️⃣ Register dependencies
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

            services.AddScoped<IDataManager, DataManager>();
            services.AddTransient<DatabaseBootstrapper>();

            // 3️⃣ Build provider
            var provider = services.BuildServiceProvider();

            // 4️⃣ Resolve & run
            var bootstrapper = provider.GetRequiredService<DatabaseBootstrapper>();
            bootstrapper.RunAsync();
        }
    }
}

using JCBSystem.LoyTr.Handlers;
using JCBSystem.LoyTr.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace JCBSystem.LoyTr
{
    // ============================================
    // 3. AUTO-REGISTRATION EXTENSION
    // ============================================
    public static class LoyTrServiceCollectionExtensions
    {
        /// <summary>
        /// Automatic registration ng lahat ng LoyTr handlers from ALL assemblies
        /// Auto-loads assemblies based on namespace pattern
        /// </summary>
        public static IServiceCollection AddLoyTr(this IServiceCollection services, bool enableLogging = false)
        {
            // Register ang LoyTr mediator
            services.AddScoped<ILoyTr, LoyTr>();

            // Get ALL loaded assemblies na may handlers
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !IsSystemAssembly(a))
                .ToList();

            if (enableLogging)
            {
                Console.WriteLine($"[LoyTr] Scanning {assemblies.Count} assemblies...");
            }

            RegisterHandlersFromAssemblies(services, assemblies, enableLogging);

            return services;
        }

        /// <summary>
        /// Automatic registration with specific assemblies
        /// services.AddLoyTr(
        //        enableLogging: true,
        //    typeof(ServiceLoginCommand).Assembly
        //  );
        /// </summary>
        public static IServiceCollection AddLoyTr(this IServiceCollection services, params Assembly[] assemblies)
        {
            return AddLoyTr(services, false, assemblies);
        }

        /// <summary>
        /// Automatic registration with specific assemblies and logging
        /// </summary>
        public static IServiceCollection AddLoyTr(this IServiceCollection services, bool enableLogging, params Assembly[] assemblies)
        {
            // Register ang LoyTr mediator
            services.AddScoped<ILoyTr, LoyTr>();

            // Kung walang assemblies na provided, gamitin ang calling assembly
            if (assemblies.Length == 0)
            {
                assemblies = new[] { Assembly.GetCallingAssembly() };
            }

            if (enableLogging)
            {
                Console.WriteLine($"[LoyTr] Scanning {assemblies.Length} specific assemblies...");
            }

            RegisterHandlersFromAssemblies(services, assemblies, enableLogging);

            return services;
        }

        /// <summary>
        /// Automatic registration from specific namespace pattern with AUTO-LOADING
        /// Example: AddLoyTrFromNamespace("JCBSystem")
        /// </summary>
        public static IServiceCollection AddLoyTrFromNamespace(this IServiceCollection services, string namespacePrefix, bool enableLogging = false, bool autoLoadAssemblies = true)
        {
            // Register ang LoyTr mediator
            services.AddScoped<ILoyTr, LoyTr>();

            if (autoLoadAssemblies)
            {
                AutoLoadAssembliesByNamespace(namespacePrefix, enableLogging);
            }

            // Get assemblies na nagsisimula sa specified namespace
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic &&
                            !IsSystemAssembly(a) &&
                            a.GetName().Name.StartsWith(namespacePrefix))
                .ToList();

            if (enableLogging)
            {
                Console.WriteLine($"[LoyTr] Found {assemblies.Count} assemblies with prefix '{namespacePrefix}'");
                foreach (var asm in assemblies)
                {
                    Console.WriteLine($"  - {asm.GetName().Name}");
                }
            }

            RegisterHandlersFromAssemblies(services, assemblies, enableLogging);

            return services;
        }

        /// <summary>
        /// Auto-load assemblies based on namespace pattern
        /// Scans the application's bin directory
        /// </summary>
        private static void AutoLoadAssembliesByNamespace(string namespacePrefix, bool enableLogging)
        {
            try
            {
                // Get the directory where the executing assembly is located
                var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var assemblyFiles = Directory.GetFiles(baseDirectory, $"{namespacePrefix}*.dll");

                if (enableLogging)
                {
                    Console.WriteLine($"[LoyTr] Auto-loading assemblies from: {baseDirectory}");
                    Console.WriteLine($"[LoyTr] Found {assemblyFiles.Length} potential assemblies");
                }

                foreach (var assemblyFile in assemblyFiles)
                {
                    try
                    {
                        var assemblyName = Path.GetFileNameWithoutExtension(assemblyFile);

                        // Check if already loaded
                        if (AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == assemblyName))
                        {
                            if (enableLogging)
                            {
                                Console.WriteLine($"[LoyTr] ✓ Already loaded: {assemblyName}");
                            }
                            continue;
                        }

                        // Load the assembly
                        Assembly.LoadFrom(assemblyFile);

                        if (enableLogging)
                        {
                            Console.WriteLine($"[LoyTr] ✓ Loaded: {assemblyName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        if (enableLogging)
                        {
                            Console.WriteLine($"[LoyTr] ✗ Failed to load {Path.GetFileName(assemblyFile)}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (enableLogging)
                {
                    Console.WriteLine($"[LoyTr] ✗ Error during auto-load: {ex.Message}");
                }
            }
        }

        private static void RegisterHandlersFromAssemblies(IServiceCollection services, IEnumerable<Assembly> assemblies, bool enableLogging = false)
        {
            int totalHandlers = 0;

            // I-scan lahat ng types sa assemblies
            foreach (var assembly in assemblies)
            {
                try
                {
                    var handlerTypes = assembly.GetTypes()
                        .Where(t => t.IsClass && !t.IsAbstract)
                        .SelectMany(t => t.GetInterfaces(), (t, i) => new { Type = t, Interface = i })
                        .Where(x => x.Interface.IsGenericType &&
                                   (x.Interface.GetGenericTypeDefinition() == typeof(ILoyTrHandler<>) ||
                                    x.Interface.GetGenericTypeDefinition() == typeof(ILoyTrHandler<,>)))
                        .ToList();

                    // Register each handler
                    foreach (var handlerType in handlerTypes)
                    {
                        services.AddScoped(handlerType.Interface, handlerType.Type);
                        totalHandlers++;

                        if (enableLogging)
                        {
                            Console.WriteLine($"[LoyTr] ✓ Registered: {handlerType.Type.Name}");
                        }
                    }
                }
                catch (ReflectionTypeLoadException ex)
                {
                    if (enableLogging)
                    {
                        Console.WriteLine($"[LoyTr] ✗ Failed to load assembly: {assembly.GetName().Name}");
                        foreach (var loaderEx in ex.LoaderExceptions)
                        {
                            Console.WriteLine($"    - {loaderEx?.Message}");
                        }
                    }
                    continue;
                }
                catch (Exception ex)
                {
                    if (enableLogging)
                    {
                        Console.WriteLine($"[LoyTr] ✗ Error scanning assembly {assembly.GetName().Name}: {ex.Message}");
                    }
                    continue;
                }
            }

            if (enableLogging)
            {
                Console.WriteLine($"[LoyTr] Total handlers registered: {totalHandlers}");
            }
        }

        private static bool IsSystemAssembly(Assembly assembly)
        {
            var name = assembly.GetName().Name;
            return name.StartsWith("System") ||
                   name.StartsWith("Microsoft") ||
                   name.StartsWith("netstandard") ||
                   name.StartsWith("mscorlib");
        }
    }
}

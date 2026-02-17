using JCBSystem.Infrastructure.Data.Seeders;
using JCBSystem.LoyTr;
using JCBSystem.LoyTr.Interfaces;
using JCBSystem.Services.Data.Seeders;
using Microsoft.Extensions.DependencyInjection;
using System;

using System.Threading.Tasks;
using System.Windows.Forms;

namespace JCBSystem.WinUi
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            // Tawagin ang DI setup mula sa Logic project
            var serviceProvider = DependencyInjection.ConfigureServices();

            // Resolve main form (kung na-register mo na sa DI)
            var mainForm = serviceProvider.GetRequiredService<MainForm>();

            Application.Run(mainForm);
        }

    }

}

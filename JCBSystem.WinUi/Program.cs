using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using JCBSystem.Core;
using JCBSystem.Users;
using Microsoft.Extensions.DependencyInjection;

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

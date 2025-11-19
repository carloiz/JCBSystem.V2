using CrystalDecisions.Shared;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JCBSystem.Infrastructure.Connection.CrystalReport
{
    public class DatabaseHelper
    {
        private static readonly string connectionString = ConfigurationManager.ConnectionStrings["MyOdbcConnection"].ConnectionString;
        // Method to create the Crystal Reports connection
        public ConnectionInfo crystalConnection()
        {
            var crConnectionInfo = new ConnectionInfo();

            try
            {
                // CRYSTAL REPORT CONNECTION
                crConnectionInfo.ServerName = connectionString;
                crConnectionInfo.IntegratedSecurity = true; // Using Windows Authentication
            }
            catch (Exception ex)
            {
                throw new Exception("Error CRYSTAL REPORT Connection" + ex.Message);
            }

            return crConnectionInfo;
        }
    }
}

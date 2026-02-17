using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JCBSystem.Core.common
{
    public static class SystemSettings
    {
        // Define pagination parameters
        public static int pageNumber = 1; // Page number to fetch
        public static int pageSize = 10;  // Number of items per page
        public static int totalPages = 0;

        public static DateTime tokenExpiration = DateTime.UtcNow.AddDays(7);

        public static readonly Color headerForeColor = Color.White;
        public static readonly Color headerBackColor = Color.FromArgb(64, 64, 64);

        public static readonly string dateFormat = "dddd, MMMM dd, yyyy hh:mm tt";
    }
}

using System;

namespace JCBSystem.Core.common.FormCustomization
{
    public static class SystemDate
    {
        public static DateTime GetPhilippineTime()
        {
            // Check if the system has the "Asia/Manila" time zone
            try
            {
                // Try to find the "Asia/Manila" time zone
                TimeZoneInfo philippineTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");

                // Get the current UTC time
                DateTime utcNow = DateTime.UtcNow;

                // Convert UTC time to Philippine Time
                DateTime philippineTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, philippineTimeZone);

                return philippineTime;
            }
            catch (TimeZoneNotFoundException)
            {
                // Handle exception if the time zone is not found
                throw new Exception("The Philippine time zone ('Asia/Manila') is not available on this system.");
            }
        }

    }
}

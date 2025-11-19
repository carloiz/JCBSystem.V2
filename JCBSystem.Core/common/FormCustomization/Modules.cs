using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace JCBSystem.Core.common.FormCustomization
{
    public static class Modules
    {


        public static string ReplaceSharpWithParams(string queryTemplate, bool isOdbc)
        {
            int counter = 0;
            return Regex.Replace(queryTemplate, "#", match =>
            {
                return isOdbc ? "?" : $"@param{counter++}";
            });
        }


        public static DataGridViewCellFormattingEventArgs CellFormatting(DataGridViewCellFormattingEventArgs e)
        {
            // Check if the cell's value is numeric (int, decimal, decimal, etc.)
            if (e.Value != null && (e.Value is double || e.Value is decimal))
            {
                // Format the value with commas and two decimal places
                e.Value = string.Format("₱{0:N2}", e.Value);
                e.FormattingApplied = true;
            }

            return e;
        }

        public static string ConvertToCommaSeparated(decimal? number)
        {
            if (number == null)
            {
                return "₱0.00";
            }
            return string.Format("₱{0:N2}", number);
        }
        public static decimal ConvertToNoComma(string numberWithComma)
        {
            if (string.IsNullOrWhiteSpace(numberWithComma))
            {
                return 0.00m;
            }

            // Remove commas and currency symbol
            string cleanNumber = numberWithComma.Replace(",", "").Replace("₱", "").Trim();

            // Try parsing the cleaned number
            if (decimal.TryParse(cleanNumber, System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.CultureInfo.InvariantCulture, out decimal result))
            {
                return result;
            }

            return 0.00m; // Return 0.00 if parsing fails
        }

    }
}

using System;
using System.Linq;
using System.Windows.Forms;

namespace JCBSystem.Core.common.FormCustomization
{
    public class CommaConvertion
    {

        public void TextKeyDownConvert(TextBox txtNumber, KeyEventArgs e)
        {
            int initialCommaCount = txtNumber.Text.Count(c => c == ',');

            if (e.KeyCode == Keys.Back && txtNumber.Text.Contains("."))
            {
                // Get the current cursor position
                int cursorPosition = txtNumber.SelectionStart;

                // Check if the cursor is right after the dot and it's trying to delete it
                if (cursorPosition > 0 && txtNumber.Text[cursorPosition - 1] == '.')
                {
                    // Prevent dot deletion by suppressing the key press
                    e.SuppressKeyPress = true;

                    // Allow the number before the dot to be deleted
                    txtNumber.Text = txtNumber.Text.Remove(cursorPosition - 2, 1);

                    int newCommaCount = txtNumber.Text.Count(c => c == ',');

                    cursorPosition += (newCommaCount - initialCommaCount) - 2;

                    int lastCursorPostion =
                        double.Parse(txtNumber.Text.Replace("₱", "").Replace(",", "")) < 1
                            ? cursorPosition + 1
                            : cursorPosition;

                    txtNumber.SelectionStart = lastCursorPostion;
                }
            }
        }

        public void TextKeyPressConvert(TextBox txtNumber, KeyPressEventArgs e)
        {
            // Allow control characters (e.g., backspace, delete)
            if (char.IsControl(e.KeyChar))
            {
                // Handle backspace specifically to remove the character before the cursor
                if (e.KeyChar == '\b') // Backspace key
                {
                    int cursorPosition = txtNumber.SelectionStart;

                    // Check if there's a comma before the cursor
                    if (cursorPosition > 0 && txtNumber.Text[cursorPosition - 1] == ',')
                    {
                        // Remove the comma and the character before it
                        txtNumber.Text = txtNumber.Text.Remove(
                            cursorPosition - 1,
                            1
                        );

                        // Adjust cursor position after removal
                        txtNumber.SelectionStart = cursorPosition - 1;

                        // Stop further processing
                        return;
                    }
                }
                return;
            }
            // Allow digits
            if (char.IsDigit(e.KeyChar))
            {
                return;
            }

            // Allow a single decimal point, but only if it hasn't been added yet
            if (e.KeyChar == '.' && !txtNumber.Text.Contains('.'))
            {
                return;
            }
            // If none of the above, block the input
            e.Handled = true;
        }

        public void TextChangeConvert(TextBox txtNumber)
        {
            char firstChar = 'n';
            if (string.IsNullOrEmpty(txtNumber.Text))
            {
                txtNumber.Text = "₱0.00";
                txtNumber.SelectionStart = 2;
                return;
            }

            if (!txtNumber.Text.Contains('₱'))
            {
                txtNumber.Text = "₱" + txtNumber.Text;
                txtNumber.SelectionStart = 2;
                return;
            }

            if (!string.IsNullOrEmpty(txtNumber.Text))
            {
                firstChar = txtNumber.Text[1];
            }

            // Save the current cursor position
            int cursorPosition = txtNumber.SelectionStart;

            // Remove any non-numeric characters except the decimal point and currency symbol
            string input = txtNumber.Text.Replace(",", "").Replace("₱", "");

            // Store the initial comma count before formatting
            int initialCommaCount = txtNumber.Text.Count(c => c == ',');

            if (double.TryParse(input, out double number))
            {
                // Format the number with the Peso symbol, comma separators, and 2 decimal places
                if (number <= 0)
                {
                    // Allow the textbox to be cleared
                    txtNumber.Text = "₱0.00";
                    txtNumber.SelectionStart = 2;
                    return;
                }
                else
                {
                    txtNumber.Text = string.Format("₱{0:N2}", number);
                }

                // Store the new comma count after formatting
                int newCommaCount = txtNumber.Text.Count(c => c == ',');

                // Adjust the cursor position based on the difference in comma counts
                cursorPosition += (newCommaCount - initialCommaCount);

                // Special handling for numbers between 0.99 and 9
                try
                {
                    if (
                        int.Parse(firstChar.ToString()) == 0
                        && double.Parse(txtNumber.Text.Replace(",", "").Replace("₱", ""))
                            > 0
                        && double.Parse(txtNumber.Text.Replace(",", "").Replace("₱", ""))
                            < 10
                    )
                    {
                        cursorPosition--;
                    }
                }
                catch (Exception)
                {
                    txtNumber.SelectionStart = 2;
                    return;
                }

                // Restore the cursor position but ensure it's not beyond the text length
                cursorPosition = Math.Max(
                    0,
                    Math.Min(cursorPosition, txtNumber.Text.Length)
                );
                txtNumber.SelectionStart = cursorPosition;
            }
        }
    }
}

using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JCBSystem.Core.common.FormCustomization
{
    public class Pagination
    {
        // Define pagination parameters
        public int pageNumber = 1; // Page number to fetch
        public int pageSize = 30;  // Number of items per page
        public int totalPages = 0;

        public void UpdatePagination(Panel panel, int totalPages, int pageNumber, Func<int, Task> action, bool isButtonsCentered)
        {
            // Clear existing pagination buttons
            panel.Controls.Clear();

            int buttonSpacing = 5; // Space between buttons
            int totalButtonWidth = 0; // Calculate total width of buttons including spacing

            // Determine the number of buttons to display
            int maxPagesToShow = 8; // Limit to 8 pages (excluding "Previous" and "Next")
            int startPage = Math.Max(1, pageNumber - maxPagesToShow / 2);
            int endPage = Math.Min(totalPages, startPage + maxPagesToShow - 1);

            // Adjust if we're near the first or last pages
            if (endPage - startPage + 1 < maxPagesToShow)
            {
                if (startPage == 1)
                {
                    endPage = Math.Min(totalPages, startPage + maxPagesToShow - 1);
                }
                else if (endPage == totalPages)
                {
                    startPage = Math.Max(1, endPage - maxPagesToShow + 1);
                }
            }

            // Add the "Previous" and "Next" buttons to the calculation
            if (pageNumber > 1) totalButtonWidth += 75 + buttonSpacing; // "Previous" button width
            if (pageNumber < totalPages) totalButtonWidth += 75 + buttonSpacing; // "Next" button width

            // Add widths of dynamic page buttons
            int dynamicButtonCount = endPage - startPage + 1;
            totalButtonWidth += (dynamicButtonCount * 50) + ((dynamicButtonCount - 1) * buttonSpacing);

            // Compute the starting X position
            int x = isButtonsCentered ? (panel.Width - totalButtonWidth) / 2 : buttonSpacing; // Center or left-aligned
            int y = (panel.Height - 30) / 2; // Center vertically (assuming button height is 30)

            // Add "Previous" button
            if (pageNumber > 1)
            {
                Button prevButton = new Button
                {
                    Text = "Previous",
                    Tag = pageNumber - 1,
                    Size = new Size(75, 30),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.White
                };
                prevButton.Click += (sender, e) =>
                {
                    int page = (int)((Button)sender).Tag;
                    PaginationButton_Click(action, page);
                };
                prevButton.Location = new Point(x, y);
                panel.Controls.Add(prevButton);
                x += prevButton.Width + buttonSpacing;
            }

            // Add page buttons dynamically
            for (int i = startPage; i <= endPage; i++)
            {
                Button pageButton = new Button
                {
                    Text = i.ToString(),
                    Tag = i,
                    Size = new Size(50, 30),
                    Enabled = i != pageNumber, // Disable the button for the current page
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.White
                };
                pageButton.Click += (sender, e) =>
                {
                    int page = (int)((Button)sender).Tag;
                    PaginationButton_Click(action, page);
                };
                pageButton.Location = new Point(x, y);
                panel.Controls.Add(pageButton);
                x += pageButton.Width + buttonSpacing;
            }

            // Add "Next" button
            if (pageNumber < totalPages)
            {
                Button nextButton = new Button
                {
                    Text = "Next",
                    Tag = pageNumber + 1,
                    Size = new Size(75, 30),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.White
                };
                nextButton.Click += (sender, e) =>
                {
                    int page = (int)((Button)sender).Tag;
                    PaginationButton_Click(action, page);
                };
                nextButton.Location = new Point(x, y);
                panel.Controls.Add(nextButton);
            }
        }


        private async void PaginationButton_Click(Func<int, Task> action, int pageNumber)
        {
            await action(pageNumber); 
        }
    }
}

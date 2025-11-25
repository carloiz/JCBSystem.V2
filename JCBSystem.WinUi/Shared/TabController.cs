using JCBSystem.Core.common.FormCustomization;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JCBSystem.WinUi.Shared
{
    public class TabController
    {
        private Dictionary<string, Form> openForms = new Dictionary<string, Form>();
        private TabControl tabControlMain = new TabControl();

        public void Initialize(Panel mainPanel)
        {
            tabControlMain = new TabControl
            {
                Dock = DockStyle.Fill,
                DrawMode = TabDrawMode.OwnerDrawFixed,
                SizeMode = TabSizeMode.Normal, // I-set sa Normal para ma-customize ang width ng bawat tab
                Padding = new System.Drawing.Point(45, 8) // Magdagdag ng padding para sa mas magandang itsura
            };

            tabControlMain.DrawItem += TabControlMain_DrawItem;
            tabControlMain.MouseDown += TabControlMain_MouseDown;

            mainPanel.Controls.Add(tabControlMain);
        }

        private void TabControlMain_DrawItem(object sender, DrawItemEventArgs e)
        {
            var tab = tabControlMain.TabPages[e.Index];
            var rect = e.Bounds;
            var text = tab.Text;

            // Tukuyin ang kulay ng background at text base sa kung selected ang tab o hindi
            System.Drawing.Color backColor;
            System.Drawing.Color textColor;

            if (e.State == DrawItemState.Selected)
            {
                backColor = System.Drawing.Color.White; // Selected tab background
                textColor = System.Drawing.Color.Black; // Selected tab text
            }
            else
            {
                backColor = System.Drawing.Color.Gray; // Unselected tab background
                textColor = System.Drawing.Color.White; // Unselected tab text
            }

            // I-fill ang background ng tab
            using (var backBrush = new System.Drawing.SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(backBrush, rect);
            }

            // Gumamit ng Graphics para i-measure ang width ng text
            using (var textBrush = new System.Drawing.SolidBrush(textColor))
            using (var font = new System.Drawing.Font("Arial", 10, System.Drawing.FontStyle.Bold))
            {
                SizeF textSize = e.Graphics.MeasureString(text, font);
                int tabWidth = (int)textSize.Width + 30; // Magdagdag ng padding para sa close button

                // I-update ang width ng tab
                tabControlMain.TabPages[e.Index].Width = tabWidth;

                // I-draw ang text
                e.Graphics.DrawString(text, font, textBrush, rect.X + 5, rect.Y + 7);
            }

            // I-draw ang close button (X)
            var closeRect = new System.Drawing.Rectangle(rect.Right - 18, rect.Top + 6, 12, 12);
            using (var pen = new System.Drawing.Pen(textColor, 2)) // Gamitin ang textColor para sa close button
            {
                e.Graphics.DrawLine(pen, closeRect.Left, closeRect.Top, closeRect.Right, closeRect.Bottom);
                e.Graphics.DrawLine(pen, closeRect.Right, closeRect.Top, closeRect.Left, closeRect.Bottom);
            }

            e.DrawFocusRectangle();
        }

        private void TabControlMain_MouseDown(object sender, MouseEventArgs e)
        {
            for (int i = 0; i < tabControlMain.TabPages.Count; i++)
            {
                var tabRect = tabControlMain.GetTabRect(i);
                var closeRect = new System.Drawing.Rectangle(tabRect.Right - 18, tabRect.Top + 6, 12, 12);

                if (closeRect.Contains(e.Location)) // Check kung na-click ang (X)
                {
                    CloseTab(i);
                    break;
                }
            }
        }

        private void CloseTab(int index)
        {
            var tabPage = tabControlMain.TabPages[index];
            var formName = tabPage.Name;

            if (openForms.ContainsKey(formName))
            {
                FormHelper.CloseFormWithFade(openForms[formName]);
                openForms.Remove(formName);
            }

            // Ang magdi-dispose sa form ay TabControl mismo
            tabControlMain.TabPages.RemoveAt(index);
        }


        public void OpenFormInTab(Form form, string title)
        {
            if (openForms.ContainsKey(title))
            {
                tabControlMain.SelectedTab = tabControlMain.TabPages[title];
                return;
            }

            form.TopLevel = false;
            form.FormBorderStyle = FormBorderStyle.None;
            form.Dock = DockStyle.Fill;

            var tabPage = new TabPage(title) { Name = title };
            tabPage.Controls.Add(form);
            tabControlMain.TabPages.Add(tabPage);
            tabControlMain.SelectedTab = tabPage;

            // I-update ang width ng tab base sa haba ng title
            using (Graphics g = tabControlMain.CreateGraphics())
            {
                SizeF textSize = g.MeasureString(title, tabControlMain.Font);
                int tabWidth = (int)textSize.Width + 30; // Magdagdag ng padding para sa close button

                // I-update ang width ng tab
                tabPage.Width = tabWidth;
            }

            openForms[title] = form;
            FormHelper.OpenFormWithFade(form, false);
        }
    }
}

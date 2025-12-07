using JCBSystem.Core;
using JCBSystem.Core.common.FormCustomization;
using JCBSystem.Core.common.Interfaces;
using JCBSystem.LoyTr.Interfaces;
using JCBSystem.Services.Users.UserManagement.Commands;
using JCBSystem.Services.Users.UserManagement.Queries;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace JCBSystem.Users
{
    public partial class UsersListForm : Form
    {
     
        private readonly IDataManager dataManager;
        private readonly ILoyTr loyTr;
        private readonly IServiceProvider serviceProvider;

        private string userNumber;

        private Dictionary<string, object> rowValue;

        public UsersListForm(ILoyTr loyTr, IServiceProvider serviceProvider)
        {
            InitializeComponent();
            this.loyTr = loyTr;
            this.serviceProvider = serviceProvider;
            //this.formFactory = formFactory;
            _ = this.loyTr.SendAsync(new GetAllUserQuery
            {
                DataGridView = dataGridView1,
                Panel = panel1
            });
        }


        private void dataGridView1_MouseUp(object sender, MouseEventArgs e)
        {
            userNumber = string.Empty;

            if (e.Button == MouseButtons.Right)
            {
                cms1.Show(Cursor.Position);
            }

            if (e.Button == MouseButtons.Right) // Check kung right-click
            {
                var hit = dataGridView1.HitTest(e.X, e.Y); // Alamin kung anong row ang na-click
                if (hit.RowIndex >= 0) // Siguraduhin na valid ang row index
                {
                    dataGridView1.ClearSelection(); // I-clear ang ibang selections
                    dataGridView1.Rows[hit.RowIndex].Selected = true; // I-select ang row

                    // Kunin ang value ng "ID" column
                    object idValue = dataGridView1.Rows[hit.RowIndex].Cells["UserNumber"].Value;
                    object userNameValue = dataGridView1.Rows[hit.RowIndex].Cells["Username"].Value;
                    object roleValue = dataGridView1.Rows[hit.RowIndex].Cells["UserLevel"].Value;

                    userNumber = idValue.ToString();

                    rowValue = new Dictionary<string, object>
                    {
                        { "Usernumber", idValue },
                        { "Username", userNameValue },
                        { "Role", roleValue },
                    };

                }
            }

            if (string.IsNullOrEmpty(userNumber))
            {
                deleteToolStripMenuItem.Visible = false;
                updateToolStripMenuItem.Visible = false;
                return;
            }

            deleteToolStripMenuItem.Visible = true;
            updateToolStripMenuItem.Visible = true;
        }

        private void addNewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var form = serviceProvider.GetRequiredService<UserManagementForm>();
            form.Initialize(this, true, rowValue);
            FormHelper.OpenFormWithFade(form, true);
        }

        private void updateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var form = serviceProvider.GetRequiredService<UserManagementForm>();
            form.Initialize(this, false, rowValue);
            FormHelper.OpenFormWithFade(form, true);
        }

        private async void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            await loyTr.SendAsync(new DeleteUserCommand
            {
                Usernumber = userNumber,
            });

            await loyTr.SendAsync(new GetAllUserQuery
            {
                DataGridView = dataGridView1,
                Panel = panel1
            });
        }
    }
}

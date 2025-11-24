using JCBSystem.Core;
using JCBSystem.Core.common.CRUD;
using JCBSystem.Core.common.FormCustomization;
using JCBSystem.Core.common.Interfaces;
using JCBSystem.Domain.DTO.Users;
using JCBSystem.Services.Authentication.Login.Commands;
using JCBSystem.Services.Users.UsersList.Commands;
using JCBSystem.Services.Users.UsersList.Queries;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace JCBSystem.Users
{
    public partial class UsersListForm : Form
    {
     
        private readonly IDataManager dataManager;
        private readonly DeleteUserCommand deleteUserCommand;
        private readonly GetAllUserQuery getAllUserQuery;
        private readonly FormFactory formFactory;

        private string userNumber;

        private Dictionary<string, object> rowValue;

        public UsersListForm(DeleteUserCommand deleteUserCommand, FormFactory formFactory, GetAllUserQuery getAllUserQuery)
        {
            InitializeComponent();
            this.deleteUserCommand = deleteUserCommand;
            this.formFactory = formFactory;
            this.getAllUserQuery = getAllUserQuery;
            this.getAllUserQuery.Initialize(dataGridView1, panel1);
            _ = this.getAllUserQuery.HandlerAsync();
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
            var form = formFactory.Create<UserManagementForm>();
            form.Initialize(this, true, rowValue);
            FormHelper.OpenFormWithFade(form, true);
        }

        private void updateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var form = formFactory.Create<UserManagementForm>();
            form.Initialize(this, false, rowValue);
            FormHelper.OpenFormWithFade(form, true);
        }

        private async void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.deleteUserCommand.Initialize(userNumber);
            await deleteUserCommand.HandlerAsync();

            getAllUserQuery.Initialize(dataGridView1, panel1);
            await getAllUserQuery.HandlerAsync();

            // Display the message for successful shift start
            MessageBox.Show($"Successfully Delete {userNumber} Record.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}

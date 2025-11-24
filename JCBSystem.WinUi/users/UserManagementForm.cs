using JCBSystem.Core.common.FormCustomization;
using JCBSystem.Services.Users.UserManagement.Commands;
using JCBSystem.Services.Users.UsersList.Queries;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace JCBSystem.Users
{
    public partial class UserManagementForm: Form
    {
        private readonly PostNewUserCommand postNewUserCommand;
        private readonly PutNewUserCommand putNewUserCommand;
        private readonly GetAllUserQuery getAllUserQuery;

        private UsersListForm listForm;
        private bool isNewRecord;
        private Dictionary<string, object> keyValues;

        public UserManagementForm(PostNewUserCommand postNewUserCommand, PutNewUserCommand putNewUserCommand, GetAllUserQuery getAllUserQuery)
        {
            InitializeComponent();
            this.postNewUserCommand = postNewUserCommand;
            this.putNewUserCommand = putNewUserCommand;
            this.getAllUserQuery = getAllUserQuery;
        }

        public void Initialize(UsersListForm listForm, bool isNewRecord, Dictionary<string, object> keyValues = null)
        {
            this.listForm = listForm;
            this.isNewRecord = isNewRecord;
            this.keyValues = keyValues;
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtUsername.Text)
                || string.IsNullOrEmpty(txtPassword.Text)
                || string.IsNullOrEmpty(txtRepassword.Text)
                || string.IsNullOrEmpty(cbRole.Text))
            {
                MessageBox.Show(
                    "Fill-Up All Fields.",
                    "",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            if (txtPassword.Text != txtRepassword.Text)
            {
                MessageBox.Show(
                    "Password and Retype Password Must Same.",
                    "",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            postNewUserCommand.Initialize(this, txtUsername.Text, txtPassword.Text, cbRole.SelectedItem.ToString());

            await postNewUserCommand.HandlerAsync();

            getAllUserQuery.Initialize(listForm.dataGridView1, listForm.panel1);
            await getAllUserQuery.HandlerAsync();

        }

        private async void button2_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtUsername.Text)
              || string.IsNullOrEmpty(txtPassword.Text)
              || string.IsNullOrEmpty(txtRepassword.Text)
              || string.IsNullOrEmpty(cbRole.Text))
            {
                MessageBox.Show(
                    "Fill-Up All Fields.",
                    "",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            if (txtPassword.Text != txtRepassword.Text)
            {
                MessageBox.Show(
                    "Password and Retype Password Must Same.",
                    "",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            putNewUserCommand.Initialize(
                this,
                keyValues["Usernumber"].ToString(), 
                keyValues["Username"].ToString(), 
                txtUsername.Text, 
                txtPassword.Text, 
                cbRole.SelectedItem.ToString()
            );

            await putNewUserCommand.HandleAsync();

            getAllUserQuery.Initialize(listForm.dataGridView1, listForm.panel1);
            await getAllUserQuery.HandlerAsync();

        }


        private void button3_Click(object sender, EventArgs e)
        {
            FormHelper.CloseFormWithFade(this, true);
        }

        private void UserManagementForm_Load(object sender, EventArgs e)
        {
            if (isNewRecord)
            {
                this.button1.Enabled = true;
                this.button2.Enabled = false;
            }
            else
            {
                txtUsername.Text = keyValues["Username"].ToString();
                cbRole.Text = keyValues["Role"].ToString();
                this.button1.Enabled = false;
                this.button2.Enabled = true;
            }
        }
    }
}

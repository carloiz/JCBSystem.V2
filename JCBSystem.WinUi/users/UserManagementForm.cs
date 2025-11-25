using JCBSystem.Core.common.FormCustomization;
using JCBSystem.LoyTr.Interfaces;
using JCBSystem.Services.Users.UserManagement.Commands;
using JCBSystem.Services.Users.UserManagement.Queries;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace JCBSystem.Users
{
    public partial class UserManagementForm: Form
    {
        private readonly ILoyTr loyTr;

        private UsersListForm listForm;
        private bool isNewRecord;
        private Dictionary<string, object> keyValues;

        public UserManagementForm(ILoyTr loyTr)
        {
            InitializeComponent();
            this.loyTr = loyTr;
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

            await loyTr.SendAsync(new PostNewUserCommand
            {
                Form = this,
                Username = txtUsername.Text,
                UserPassword = txtPassword.Text,
                UserLevel = cbRole.SelectedItem.ToString(), 
            });

            await loyTr.SendAsync(new GetAllUserQuery
            {
                DataGridView = listForm.dataGridView1,
                Panel = listForm.panel1
            });
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

            await loyTr.SendAsync(new PutNewUserCommand
            {
                Form = this,
                KeyUsernumber = keyValues["Usernumber"].ToString(),
                KeyUsername = keyValues["Username"].ToString(),
                Username = txtUsername.Text,
                Password = txtPassword.Text,
                UserLevel = cbRole.SelectedItem.ToString(),
            });

            await loyTr.SendAsync(new GetAllUserQuery
            {
                DataGridView = listForm.dataGridView1,
                Panel = listForm.panel1
            });
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

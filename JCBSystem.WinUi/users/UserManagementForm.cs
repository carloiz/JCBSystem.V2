using JCBSystem.Core.common;
using JCBSystem.Core.common.CRUD;
using JCBSystem.Core.common.FormCustomization;
using JCBSystem.Core.common.Helpers;
using JCBSystem.Core.common.Interfaces;
using JCBSystem.Core.common.Logics;
using JCBSystem.Domain.DTO.Users;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JCBSystem.Users
{
    public partial class UserManagementForm: Form
    {
        private readonly IDataManager dataManager;
        private readonly CheckIfRecordExists checkIfRecordExists;
        private readonly GenerateNextValues generateNextValues;
        private UsersListForm listForm;
        private bool isNewRecord;
        private Dictionary<string, object> keyValues;

        public UserManagementForm(IDataManager dataManager, 
               CheckIfRecordExists checkIfRecordExists, 
               GenerateNextValues generateNextValues)
        {
            InitializeComponent();
            this.dataManager = dataManager;
            this.checkIfRecordExists = checkIfRecordExists;
            this.generateNextValues = generateNextValues;
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

            bool isExist = await checkIfRecordExists.ExecuteAsync(
                new List<object> { txtUsername.Text },
                "Users",
                "Username = #"
            );

            if (isExist)
            {
                MessageBox.Show(
                    "Username Already Exist.",
                    "",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            await dataManager.CommitAndRollbackMethod(async (connection, transaction) =>
            {
                await ProcessCreate(connection, transaction); // Tawagin ang Process method na may transaction at connection
            });
        }


        private async Task ProcessCreate(IDbConnection connection, IDbTransaction transaction)
        {
            string password = PasswordHelper.HashPassword(txtPassword.Text);

            string userId = await generateNextValues.ByIdAsync("Users", "UserNumber", "U");

            DateTime dateToday = SystemDate.GetPhilippineTime();

            var userCreateDto = new UsersDto
            {
                UserNumber = userId,
                Username = txtUsername.Text,
                Password = password,
                UserLevel = cbRole.SelectedItem.ToString(),
                Status = true,
                IsSessionActive = false,
                CurrentToken = null,
                RecordDate = dateToday
            };

            await dataManager.InsertAsync(userCreateDto, "Users", connection, transaction, "UserNumber");


            transaction.Commit(); // Commit changes  

            listForm.get_all_data();

            // Display the message for successful shift start
            MessageBox.Show("Successfully Add New Record.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

            FormHelper.CloseFormWithFade(this, true);
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

            bool isExist = await checkIfRecordExists.ExecuteAsync(
                new List<object> { keyValues["Username"].ToString(), txtUsername.Text },
                "Users",
                "Username NOT LIKE # AND Username = #"
            );

            if (isExist)
            {
                MessageBox.Show(
                    "Username Already Exist.",
                    "",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            await dataManager.CommitAndRollbackMethod(async (connection, transaction) =>
            {
                await ProcessUpdate(connection, transaction); // Tawagin ang Process method na may transaction at connection
            });
        }


        private async Task ProcessUpdate(IDbConnection connection, IDbTransaction transaction)
        {
            string password = PasswordHelper.HashPassword(txtPassword.Text);

            var userId = keyValues["Usernumber"].ToString();

            var userUpdateDto = new UsersDto
            {
                UserNumber = userId,
                Username = txtUsername.Text,
                Password = password,
                UserLevel = cbRole.SelectedItem.ToString(),

            };

            await dataManager.UpdateAsync(
                entity: userUpdateDto,
                tableName: "Users",
                connection: connection,
                transaction: transaction,
                primaryKey: "UserNumber"
            );


            transaction.Commit(); // Commit changes  

            listForm.get_all_data();

            // Display the message for successful shift start
            MessageBox.Show($"Successfully Update {userId} Record.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

            FormHelper.CloseFormWithFade(this, true);
        }
    }
}

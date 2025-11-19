using JCBSystem.Core;
using JCBSystem.Core.common.CRUD;
using JCBSystem.Core.common.FormCustomization;
using JCBSystem.Core.common.Interfaces;
using JCBSystem.Domain.DTO.Users;
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
        private readonly Pagination pagination;
        private readonly FormFactory formFactory;

        private string userNumber;

        private Dictionary<string, object> rowValue;

        public UsersListForm(IDataManager dataManager, Pagination pagination, FormFactory formFactory)
        {
            InitializeComponent();
            this.dataManager = dataManager;
            this.pagination = pagination;
            this.formFactory = formFactory;
            get_all_data();
        }


        public async void get_all_data(List<string> image = null)
        {

            string countQuery = $@"SELECT COUNT(*) FROM Users";

            // Query to fetch paginated data
            string dataQuery = $@"SELECT * FROM Users";


            var customHeaders = new Dictionary<string, string>
            {
                { "UserNumber", "ID" },
                { "Username", "Username" },
                { "UserLevel", "Role" },
                { "Status", "Status" },
                { "IsSessionActive", "Session" },
                { "RecordDate", "Record Date" }
            };



            var (result, totalRecords) = await
                dataManager.SearchWithPaginatedAsync<UsersDto>
                (new List<object> { }, countQuery, dataQuery, dataGridView1, image, customHeaders, pagination.pageNumber, pagination.pageSize);

            pagination.totalPages = (int)Math.Ceiling((double)totalRecords / pagination.pageSize);



            pagination.UpdatePagination(panel1, pagination.totalPages, pagination.pageNumber, UpdateRecords, true);


            dataGridView1.ColumnHeadersVisible = (string.IsNullOrEmpty(result)) ? true : false;


            foreach (DataGridViewColumn column in dataGridView1.Columns)
            {
                if (column.Name == "UserNumber" || column.Name == "Username" || column.Name == "UserLevel" || column.Name == "Status" || column.Name == "IsSessionActive")
                {
                    column.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells; // Adjust based on content
                }
                else if (column.Name == "ImageColumn2")
                {
                    column.Width = 40;
                }
                else
                {
                    column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; // Keep other columns evenly distributed
                }
            }
        }


        private Task UpdateRecords(int pageNumber)
        {
            pagination.pageNumber = pageNumber;
            get_all_data();
            return Task.CompletedTask;
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
            await dataManager.CommitAndRollbackMethod(async (connection, transaction) =>
            {
                await ProcessDelete(connection, transaction); // Tawagin ang Process method na may transaction at connection
            });
        }


        private async Task ProcessDelete(IDbConnection connection, IDbTransaction transaction)
        {

            string whereCondition = "Usernumber = #";

            await dataManager.DeleteAsync(new List<object> { userNumber }, "Users", connection, transaction, whereCondition);


            transaction.Commit(); // Commit changes  

            get_all_data();

            // Display the message for successful shift start
            MessageBox.Show($"Successfully Delete {userNumber} Record.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

        }
    }
}

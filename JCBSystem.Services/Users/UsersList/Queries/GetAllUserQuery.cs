using JCBSystem.Core.common.CRUD;
using JCBSystem.Core.common.FormCustomization;
using JCBSystem.Core.common.Interfaces;
using JCBSystem.Domain.DTO.Users;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JCBSystem.Services.Users.UsersList.Queries
{
    public class GetAllUserQuery
    {
        private readonly IDataManager dataManager;
        private readonly Pagination pagination;

        private DataGridView dataGridView;
        private Panel panel;

        public GetAllUserQuery(IDataManager dataManager, Pagination pagination)
        {
            this.dataManager = dataManager;
            this.pagination = pagination;
        }


        public void Initialize(DataGridView dataGridView, Panel panel)
        {
            this.dataGridView = dataGridView;
            this.panel = panel;
        }

        public async Task HandlerAsync(List<string> image = null)
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
                (new List<object> { }, countQuery, dataQuery, dataGridView, image, customHeaders, pagination.pageNumber, pagination.pageSize);

            pagination.totalPages = (int)Math.Ceiling((double)totalRecords / pagination.pageSize);


            pagination.UpdatePagination(panel, pagination.totalPages, pagination.pageNumber, UpdateRecords, true);

            dataGridView.ColumnHeadersVisible = (string.IsNullOrEmpty(result)) ? true : false;


            foreach (DataGridViewColumn column in dataGridView.Columns)
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
            HandlerAsync();
            return Task.CompletedTask;
        }
    }
}

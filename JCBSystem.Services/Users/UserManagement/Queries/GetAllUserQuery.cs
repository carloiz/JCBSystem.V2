using JCBSystem.Core.common.FormCustomization;
using JCBSystem.Core.common.Interfaces;
using JCBSystem.Domain.DTO.Users;
using JCBSystem.LoyTr.Handlers;
using JCBSystem.LoyTr.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JCBSystem.Services.Users.UserManagement.Queries
{
    public class GetAllUserQuery : ILoyTrRequest
    {
        public DataGridView DataGridView { get; set; }  
        public Panel Panel { get; set; }   
        public List<string> Image {  get; set; } = new List<string>();
    }

    public class GetAllUserQueryHandler : ILoyTrHandler<GetAllUserQuery>
    {
        private readonly IDataManager dataManager;
        private readonly Pagination pagination;

        public GetAllUserQueryHandler(IDataManager dataManager, Pagination pagination)
        {
            this.dataManager = dataManager;
            this.pagination = pagination;
        }


        public async Task HandleAsync(GetAllUserQuery getAllUserQuery)
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
                (new List<object> { }, countQuery, dataQuery, getAllUserQuery.DataGridView, getAllUserQuery.Image, customHeaders, pagination.pageNumber, pagination.pageSize);

            pagination.totalPages = (int)Math.Ceiling((double)totalRecords / pagination.pageSize);


            pagination.UpdatePagination(getAllUserQuery.Panel, pagination.totalPages, pagination.pageNumber, UpdateRecords, true);

            getAllUserQuery.DataGridView.ColumnHeadersVisible = (string.IsNullOrEmpty(result)) ? true : false;


            foreach (DataGridViewColumn column in getAllUserQuery.DataGridView.Columns)
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
            //HandleAsync();
            return Task.CompletedTask;
        }
    }
}

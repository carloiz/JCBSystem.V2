using JCBSystem.Core.common;
using JCBSystem.Core.common.FormCustomization;
using JCBSystem.Core.common.Interfaces;
using JCBSystem.Core.common.Models;
using JCBSystem.Domain.Entities.Users;
using JCBSystem.LoyTr.Handlers;
using JCBSystem.LoyTr.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JCBSystem.Services.Users.UserManagement.Queries
{
    public class GetAllUserQuery : IRequest
    {
        public DataGridView DataGridView { get; set; }  
        public Panel Panel { get; set; }   
        public List<string> Image {  get; set; } = new List<string>();
    }

    public class GetAllUserQueryHandler : IRequestHandler<GetAllUserQuery>
    {
        private readonly IDataManager dataManager;

        public GetAllUserQueryHandler(IDataManager dataManager)
        {
            this.dataManager = dataManager;
        }


        public async Task HandleAsync(GetAllUserQuery req)
        {

            string countQuery = $@"SELECT COUNT(*) FROM Users";

            // Query to fetch paginated data
            string dataQuery = $@"SELECT * FROM Users ORDER BY UserNumber";


            var customHeaders = new Dictionary<string, string>
            {
                { "UserNumber", "ID" },
                { "Username", "Username" },
                { "UserLevel", "Role" },
                { "Status", "Status" },
                { "IsSessionActive", "Session" },
                { "RecordDate", "Record Date" }
            };

            var queryRequest = new QueryRequestWithParams
            {

                CountQuery = countQuery,
                DataQuery = dataQuery,
                DataGrid = req.DataGridView,
                ImageColumns = req.Image,
                CustomColumnHeaders = customHeaders,
                PageNumber = SystemSettings.pageNumber,
                PageSize = SystemSettings.pageSize,
            };

            var (result, totalRecords) = await dataManager.SelectAllWithPaginatedAsync<UsersEntity>(queryRequest);

            SystemSettings.totalPages = (int)Math.Ceiling((double)totalRecords / SystemSettings.pageSize);


            Pagination.Update(req.Panel, SystemSettings.totalPages, SystemSettings.pageNumber, UpdateRecords, true);

            req.DataGridView.ColumnHeadersVisible = (string.IsNullOrEmpty(result)) ? true : false;


            foreach (DataGridViewColumn column in req.DataGridView.Columns)
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

            async Task UpdateRecords(int pageNumber)
            {
                SystemSettings.pageNumber = pageNumber;
                await HandleAsync(req);
                SystemSettings.pageNumber = 1;
            }
        }
    }
}

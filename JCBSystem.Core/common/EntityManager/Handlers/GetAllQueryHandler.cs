using JCBSystem.Core.common.FormCustomization;
using JCBSystem.Infrastructure.Connection;
using JCBSystem.Infrastructure.Connection.Interface;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Odbc;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JCBSystem.Core.common.EntityManager.Handlers
{
    public class GetAllQueryHandler
    {

        private readonly IConnectionFactory connectionFactory;
        private readonly IDbConnectionFactory dbConnectionFactory;

        public GetAllQueryHandler(IDbConnectionFactory dbConnectionFactory, IConnectionFactory connectionFactory)
        {
            this.dbConnectionFactory = dbConnectionFactory;
            this.connectionFactory = connectionFactory;
        }
        
        /// <summary>
        /// READ ALL DATA
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="countQuery"></param>
        /// <param name="dataQuery"></param>
        /// <param name="dataGrid"></param>
        /// <param name="imageColumns"></param>
        /// <param name="customColumnHeaders"></param>
        /// <param name="pageNumber"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public async Task<(string, int)>
            HandleAsync<T>(
                string countQuery,
                string dataQuery,
                DataGridView dataGrid,
                List<string> imageColumns,
                Dictionary<string, string> customColumnHeaders, // Bagong parameter para sa custom headers
                int pageNumber = 1,
                int pageSize = 10
            )
            where T : new()
        {
            var resultList = new List<T>();

            // Calculate offset for pagination
            int offset = (pageNumber - 1) * pageSize;
            int totalRecords = 0;

            using (var connection = dbConnectionFactory.CreateConnection())
            {
                await connectionFactory.OpenConnectionAsync(connection);


                bool isOdbc = connection is OdbcConnection;
                bool isNpgSql = connection is NpgsqlConnection;

                string finalCountQuery = Modules.ReplaceSharpWithParams(countQuery, isOdbc);
                string finalDataQuery = Modules.ReplaceSharpWithParams(dataQuery, isOdbc);

                if (isOdbc)
                    finalDataQuery += " LIMIT ? OFFSET ?";
                else if (isNpgSql)
                    finalDataQuery += " LIMIT @PageSize OFFSET @Offset";
                else
                    finalDataQuery += " OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";


                if (!string.IsNullOrEmpty(finalCountQuery))
                {
                    // Execute the count query to get total records
                    using (var countCommand = connection.CreateCommand())
                    {
                        countCommand.CommandText = finalCountQuery;

                        // Gamitin dynamic kung supported ang async, fallback to sync kung hindi
                        if (countCommand is DbCommand dbCountCommand)
                        {
                            var result = await dbCountCommand.ExecuteScalarAsync();
                            totalRecords = Convert.ToInt32(result);
                        }
                        else
                        {
                            var result = countCommand.ExecuteScalar();
                            totalRecords = Convert.ToInt32(result);
                        }
                    }

                }

                // Execute the data query to get paginated records
                // Execute the count query to get total records
                using (var command = connection.CreateCommand())
                {

                    // Explicitly create parameters for pagination
                    var pageSizeParameter = command.CreateParameter();
                    pageSizeParameter.ParameterName = isOdbc ? "?" : "@PageSize";
                    pageSizeParameter.Value = pageSize;
                    command.Parameters.Add(pageSizeParameter);

                    var offsetParameter = command.CreateParameter();
                    offsetParameter.ParameterName = isOdbc ? "?" : "@Offset";
                    offsetParameter.Value = offset;
                    command.Parameters.Add(offsetParameter);


                    command.CommandText = finalDataQuery;

                    // Cast to DbCommand to access ExecuteReaderAsync
                    if (command is DbCommand dbCommand)
                    {
                        using (var reader = await dbCommand.ExecuteReaderAsync())
                        {
                            // Reading paginated data
                            while (await reader.ReadAsync())
                            {
                                T entity = new T();
                                foreach (var prop in typeof(T).GetProperties())
                                {
                                    var columnName = prop.Name;
                                    if (!reader.IsDBNull(reader.GetOrdinal(columnName)))
                                    {
                                        var columnValue = reader[columnName];

                                        // Check if the property is an enum
                                        if (prop.PropertyType.IsEnum)
                                        {
                                            if (int.TryParse(columnValue.ToString(), out int enumValue))
                                            {
                                                prop.SetValue(entity, Enum.ToObject(prop.PropertyType, enumValue));
                                            }
                                        }
                                        // Check if the property is a boolean (bool)
                                        //else if (prop.Name == "Status")
                                        //{
                                        //    // Convert bool to string ("Active" or "Inactive") specifically for the "Status" property
                                        //    bool boolValue = Convert.ToBoolean(columnValue);
                                        //    string displayValue = boolValue ? "Active" : "Inactive";
                                        //    prop.SetValue(entity, displayValue);
                                        //}
                                        // Handle other types
                                        else if (columnValue != null && prop.PropertyType.IsAssignableFrom(columnValue.GetType()))
                                        {
                                            prop.SetValue(entity, columnValue);
                                        }
                                        else if (prop.PropertyType == typeof(string))
                                        {
                                            prop.SetValue(entity, columnValue.ToString());
                                        }
                                        else if (prop.PropertyType == typeof(int) && columnValue is string)
                                        {
                                            prop.SetValue(entity, int.Parse(columnValue.ToString()));
                                        }
                                    }
                                }
                                resultList.Add(entity);
                            }
                        }
                    }
                }
            }

            if (resultList == null || !resultList.Any())
            {
                dataGrid.DataSource = null;
                return ($"No data found in the result.", 0);
            }

            // Bind data to DataGridView
            dataGrid.DataSource = resultList;
            dataGrid.RowHeadersVisible = false;
            dataGrid.EnableHeadersVisualStyles = false;
            dataGrid.ColumnHeadersDefaultCellStyle.BackColor = SystemSettings.headerBackColor;
            dataGrid.ColumnHeadersDefaultCellStyle.ForeColor = SystemSettings.headerForeColor;
            dataGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Arial", 10, FontStyle.Regular);
            dataGrid.ColumnHeadersDefaultCellStyle.Padding = new Padding(5, 5, 5, 5);


            dataGrid.CellFormatting += (sender, e) =>
            {
                // Check if the value in the cell is a DateTime
                if (e.Value is DateTime dateValue)
                {
                    // Format the DateTime for display
                    e.Value = dateValue.ToString(SystemSettings.dateFormat, CultureInfo.InvariantCulture);
                    e.FormattingApplied = true;
                }
            };

            // Center-align header text
            dataGrid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            // Center-align cell content for each column
            foreach (DataGridViewColumn column in dataGrid.Columns)
            {
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            // Set AutoSizeColumnsMode to Fill to evenly distribute the column width
            dataGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // Apply custom column headers
            if (customColumnHeaders != null)
            {
                foreach (var key in customColumnHeaders.Keys)
                {
                    if (dataGrid.Columns.Contains(key))
                    {
                        var column = dataGrid.Columns[key];
                        column.HeaderText = customColumnHeaders[key];
                        column.Visible = true;
                        column.DisplayIndex = customColumnHeaders.Keys.ToList().IndexOf(key);
                    }
                }

                // Hide all other columns
                foreach (DataGridViewColumn column in dataGrid.Columns)
                {
                    if (!customColumnHeaders.ContainsKey(column.Name))
                    {
                        column.Visible = false;
                    }
                }
            }


            // Exclude image columns from AutoSizeColumnsMode.Fill
            if (imageColumns != null && imageColumns.Count > 0)
            {
                foreach (string imageColumnName in imageColumns)
                {
                    if (dataGrid.Columns.Contains(imageColumnName))
                    {
                        // Set a fixed width for the image columns
                        dataGrid.Columns[imageColumnName].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                        dataGrid.Columns[imageColumnName].Width = 35; // Set your desired fixed width for image columns
                        dataGrid.Columns[imageColumnName].DisplayIndex = dataGrid.Columns.Count - 1; // Optional: move to the last position
                    }
                }
            }

            // **NEW: Enable multiline support and row auto-sizing**
            dataGrid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            foreach (DataGridViewColumn column in dataGrid.Columns)
            {
                column.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            }

            // Return result with additional information if necessary
            return (string.Empty, totalRecords);
        }
    }
}

using JCBSystem.Core.common.FormCustomization;
using JCBSystem.Core.common.Models;
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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JCBSystem.Core.common.EntityManager.Handlers
{
    public class GetQueryHandler
    {

        private readonly IConnectionFactory connectionFactorySelector;
        private readonly IDbConnectionFactory dbConnectionFactory;

        public GetQueryHandler(IDbConnectionFactory dbConnectionFactory, IConnectionFactory connectionFactorySelector)
        {
            this.dbConnectionFactory = dbConnectionFactory;
            this.connectionFactorySelector = connectionFactorySelector;
        }

        /// <summary>
        /// READ DATA BY CONDITION
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parameterValues"></param>
        /// <param name="countQuery"></param>
        /// <param name="dataQuery"></param>
        /// <param name="dataGrid"></param>
        /// <param name="imageColumns"></param>
        /// <param name="customColumnHeaders"></param>
        /// <param name="pageNumber"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public async Task<(string, int)>
          HandleAsync<T>(QueryRequestWithParams queryRequest)
          where T : new()
        {
            int index = 0;
            var resultList = new List<T>();

            // Calculate offset for pagination
            int offset = (queryRequest.PageNumber - 1) * queryRequest.PageSize;
            int totalRecords = 0;

            using (var connection = dbConnectionFactory.CreateConnection())
            {
                await connectionFactorySelector.OpenConnectionAsync(connection);

                bool isOdbc = connection is OdbcConnection;
                bool isNpgSql = connection is NpgsqlConnection;

                string finalCountQuery = Modules.ReplaceSharpWithParams(queryRequest.CountQuery, isOdbc);
                string finalDataQuery = Modules.ReplaceSharpWithParams(queryRequest.DataQuery, isOdbc);

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

                        if (queryRequest.ParameterValues.Count > 0) 
                        {
                            foreach (var param in queryRequest.ParameterValues)
                            {
                                string paramName = "@param" + index;

                                var parameter = countCommand.CreateParameter();
                                parameter.ParameterName = paramName;
                                parameter.Value = param;
                                countCommand.Parameters.Add(parameter);

                                index++;
                            }
                        }

                        // Execute the count query to get total records
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
                using (var command = connection.CreateCommand())
                {
                    index = 0;

                    if (queryRequest.ParameterValues.Count > 0)
                    {
                        foreach (var param in queryRequest.ParameterValues)
                        {
                            string paramName = "@param" + index;

                            var parameter = command.CreateParameter();
                            parameter.ParameterName = paramName;
                            parameter.Value = param;
                            command.Parameters.Add(parameter);
                            index++;
                        }
                    }

                    // Explicitly create parameters for pagination
                    // data
                    var pageSizeParameter = command.CreateParameter();
                    pageSizeParameter.ParameterName = isOdbc ? "?" : "@PageSize";
                    pageSizeParameter.Value = queryRequest.PageSize;
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
                                        //// Check if the property is a boolean (bool)
                                        //else if (prop.Name == "Status")
                                        //{
                                        //    // Convert bool to string ("Active" or "Inactive") specifically for the "Status" property
                                        //    bool boolValue = Convert.ToBoolean(columnValue);
                                        //    string displayValue = boolValue ? "Active" : "Inactive";
                                        //    prop.SetValue(entity, displayValue);
                                        //}

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
                queryRequest.DataGrid.DataSource = null;
                return ($"No data found in the result.", 0);
            }

            // Bind data to queryRequest.DataGridView
            queryRequest.DataGrid.DataSource = resultList;
            queryRequest.DataGrid.RowHeadersVisible = false;
            queryRequest.DataGrid.EnableHeadersVisualStyles = false;
            queryRequest.DataGrid.ColumnHeadersDefaultCellStyle.BackColor = SystemSettings.headerBackColor;
            queryRequest.DataGrid.ColumnHeadersDefaultCellStyle.ForeColor = SystemSettings.headerForeColor;
            queryRequest.DataGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Arial", 10, FontStyle.Regular);
            queryRequest.DataGrid.ColumnHeadersDefaultCellStyle.Padding = new Padding(5, 5, 5, 5);

            queryRequest.DataGrid.CellFormatting += (sender, e) =>
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
            queryRequest.DataGrid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            // Center-align cell content for each column
            foreach (DataGridViewColumn column in queryRequest.DataGrid.Columns)
            {
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            // Set AutoSizeColumnsMode to Fill to evenly distribute the column width
            queryRequest.DataGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // Apply custom column headers
            if (queryRequest.CustomColumnHeaders != null)
            {
                foreach (var key in queryRequest.CustomColumnHeaders.Keys)
                {
                    if (queryRequest.DataGrid.Columns.Contains(key))
                    {
                        var column = queryRequest.DataGrid.Columns[key];
                        column.HeaderText = queryRequest.CustomColumnHeaders[key];
                        column.Visible = true;
                        column.DisplayIndex = queryRequest.CustomColumnHeaders.Keys.ToList().IndexOf(key);
                    }
                }

                // Hide all other columns
                foreach (DataGridViewColumn column in queryRequest.DataGrid.Columns)
                {
                    if (!queryRequest.CustomColumnHeaders.ContainsKey(column.Name))
                    {
                        column.Visible = false;
                    }
                }
            }


            // Exclude image columns from AutoSizeColumnsMode.Fill
            if (queryRequest.ImageColumns != null && queryRequest.ImageColumns.Count > 0)
            {
                foreach (string imageColumnName in queryRequest.ImageColumns)
                {
                    if (queryRequest.DataGrid.Columns.Contains(imageColumnName))
                    {
                        // Set a fixed width for the image columns
                        queryRequest.DataGrid.Columns[imageColumnName].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                        queryRequest.DataGrid.Columns[imageColumnName].Width = 35; // Set your desired fixed width for image columns
                        queryRequest.DataGrid.Columns[imageColumnName].DisplayIndex = queryRequest.DataGrid.Columns.Count - 1; // Optional: move to the last position
                    }
                }
            }

            // **NEW: Enable multiline support and row auto-sizing**
            queryRequest.DataGrid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;


            foreach (DataGridViewColumn column in queryRequest.DataGrid.Columns)
            {
                column.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            }

            // Return result with additional information if necessary
            return (string.Empty, totalRecords);


        }
    }
}

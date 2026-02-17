using CrystalDecisions.Shared;
using System.Data.SqlClient;
using System.Data;
using CrystalDecisions.Windows.Forms;
using System.Data.Common;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using CrystalDecisions.CrystalReports.Engine;
using JCBSystem.Infrastructure.Connection.Interface;
using JCBSystem.Infrastructure.Connection.CrystalReport;

namespace JCBSystem.Core.common.CrystalReport
{
    public class CrystalReportConfig
    {
        private readonly IDbConnectionFactory dbConnectionFactory;
        private readonly DatabaseHelper databaseHelper;

        private readonly IConnectionFactory connectionFactory;


        public CrystalReportConfig(IConnectionFactory connectionFactory, 
                                   IDbConnectionFactory dbConnectionFactory, 
                                   DatabaseHelper databaseHelper)
        {
            this.databaseHelper = databaseHelper;
            this.dbConnectionFactory = dbConnectionFactory;
            this.connectionFactory = connectionFactory;
        }

        public async Task GenerateReportWithMultipleSubreports(
            ReportDocument repo, 
            List<(string query, string subreportName, List<SqlParameter> parameters, bool isMainReport)> queryParamsList,
            CrystalReportViewer ReportViewer,
            Dictionary<string, object> _keyValues
        )
        {

            TableLogOnInfo crtableLogoninfo;
            // Set up Crystal Reports connection
            var crConnectionInfo = databaseHelper.crystalConnection();
            Tables CrTables = repo.Database.Tables;
            foreach (Table CrTable in CrTables)
            {
                crtableLogoninfo = CrTable.LogOnInfo;
                crtableLogoninfo.ConnectionInfo = crConnectionInfo;
                CrTable.ApplyLogOnInfo(crtableLogoninfo);
            }

            // Open connection using ADO.NET
            using (var connection = dbConnectionFactory.CreateConnection())
            {

                await connectionFactory.OpenConnectionAsync(connection);

                // Initialize a flag to track whether we've set the main report's data source
                bool mainReportSet = false;

                // Loop through all queries and parameters provided
                foreach (var queryParam in queryParamsList)
                {
                    var query = queryParam.query;
                    var subreportName = queryParam.subreportName;
                    var parameters = queryParam.parameters;
                    var isMainReport = queryParam.isMainReport;
                    var dataTable = new DataTable();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = query;
                        // Add parameters dynamically to the SqlCommand
                        foreach (var param in parameters)
                        {
                            command.Parameters.Add(param);
                        }

                        // Execute the query and load data into DataTable
                        // ✅ FIX: await the async adapter creation
                        var adapter = await connectionFactory.CreateDataAdapter(command);

                        if (adapter is DbDataAdapter dbAdapter)
                        {
                            dbAdapter.Fill(dataTable); // ✔️ This works
                        }
                        else
                        {
                            throw new NotSupportedException($"Adapter type not supported: {adapter.GetType().Name}");
                        }

                    }

                    if (isMainReport)
                    {
                        // Set the data source for the main report only once
                        if (!mainReportSet)
                        {
                            repo.SetDataSource(dataTable);
                            mainReportSet = true;
                        }
                    }
                    else
                    {
                        // Set the data source for the appropriate subreport
                        repo.Subreports[subreportName].SetDataSource(dataTable);
                    }
                }
            }

            if (_keyValues != null)
            {
                // Set parameters for Crystal Report
                foreach (var key in _keyValues.Keys)
                {
                    var value = _keyValues[key];

                    // Debugging: Check the key, value, and type before setting
                    Console.WriteLine($"Setting parameter: {key} = {value} (Type: {value?.GetType()})");

                    try
                    {
                        if (value is int intValue)
                            repo.SetParameterValue(key, intValue);
                        else if (value is decimal decimalValue)
                            repo.SetParameterValue(key, decimalValue);
                        else if (value is double doubleValue)
                            repo.SetParameterValue(key, doubleValue);
                        else if (value is long longValue)
                            repo.SetParameterValue(key, longValue);
                        else if (value is bool boolValue)
                            repo.SetParameterValue(key, boolValue);
                        else if (value is DateTime dateTimeValue)
                            repo.SetParameterValue(key, dateTimeValue);
                        else if (value is string stringValue)
                            repo.SetParameterValue(key, stringValue);
                        else if (value == null)
                            repo.SetParameterValue(key, DBNull.Value); // Assign DBNull for NULL values
                        else
                            repo.SetParameterValue(key, value.ToString()); // Convert unknown types to string
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error setting parameter '{key}': {ex.Message}");
                    }
                }
            }


            ReportViewer.ReportSource = repo;

            // After setting data sources for all subreports, you can finalize the report generation
            // Example: You can now export or display the report as needed.
        }

        public SqlParameter CreateSqlParameter(string parameterName, object value)
        {
            SqlParameter parameter = new SqlParameter(parameterName, GetSqlDbType(value))
            {
                Value = value ?? DBNull.Value // Handle null values
            };
            return parameter;
        }

        public SqlDbType GetSqlDbType(object value)
        {
            if (value == null)
            {
                return SqlDbType.Variant; // Or choose a suitable type like SqlDbType.NVarChar if you prefer.
            }

            Type type = value.GetType();
            if (type == typeof(int))
                return SqlDbType.Int;
            else if (type == typeof(long))
                return SqlDbType.BigInt;
            else if (type == typeof(bool))
                return SqlDbType.Bit;
            else if (type == typeof(string))
                return SqlDbType.NVarChar;
            else if (type == typeof(DateTime))
                return SqlDbType.DateTime;
            else if (type == typeof(decimal))
                return SqlDbType.Decimal;
            else if (type == typeof(double))
                return SqlDbType.Float;
            else if (type == typeof(Guid))
                return SqlDbType.UniqueIdentifier;
            else if (type == typeof(byte[]))
                return SqlDbType.VarBinary;
            else
                return SqlDbType.Variant; // Default type
        }
    }
}

using Npgsql;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Odbc;
using System.Threading.Tasks;
using System.Windows.Forms;
using JCBSystem.Core.common.FormCustomization;
using JCBSystem.Infrastructure.Connection.Interface;

namespace JCBSystem.Core.common.Logics.Handlers
{
    public class LoadDataToTextBoxes
    {

        private readonly IConnectionFactory connectionFactory;
        private readonly IDbConnectionFactory dbConnectionFactory;

        public LoadDataToTextBoxes(IDbConnectionFactory dbConnectionFactory, IConnectionFactory connectionFactory)
        {
            this.dbConnectionFactory = dbConnectionFactory;
            this.connectionFactory = connectionFactory;
        }

        private readonly string dateFormat = "dddd, MMMM dd, yyyy hh:mm tt";


        public async Task
           HandleAsync(
               string query,
               Dictionary<string, object> parameters,
               List<TextBox> textBoxes,
               bool isTextBoxStr,
               bool isFixedNumber,
               Dictionary<int, Type> enumColumns = null
           )
        {
            if (textBoxes == null || textBoxes.Count == 0)
            {
                throw new ArgumentNullException(nameof(textBoxes), "TextBoxes list cannot be null or empty.");
            }

            try
            {
                using (var connection = dbConnectionFactory.CreateConnection())
                {
                    await connectionFactory.OpenConnectionAsync(connection);

                    var isOdbc = connection is OdbcConnection || connection is NpgsqlConnection;

                    string finalQuery = Modules.ReplaceSharpWithParams(query, isOdbc);

                    var command = connection.CreateCommand();

                    command.CommandText = finalQuery;

                    // Add parameters to the command object, making sure none of them are null
                    if (parameters != null && parameters.Count > 0)
                    {
                        foreach (var param in parameters)
                        {
                            if (param.Value == null)
                            {
                                throw new ArgumentNullException(param.Key, $"Parameter {param.Key} cannot be null.");
                            }
                            var dbParam = command.CreateParameter();
                            dbParam.ParameterName = param.Key;
                            dbParam.Value = param.Value ?? DBNull.Value;
                            command.Parameters.Add(dbParam);

                        }
                    }




                    if (command is DbCommand dbCommand)
                    {


                        var reader = await dbCommand.ExecuteReaderAsync();

                        if (reader.HasRows)
                        {
                            while (await reader.ReadAsync())
                            {
                                // Map the data from reader to textboxes dynamically
                                for (int i = 0; i < textBoxes.Count; i++)
                                {
                                    if (i < reader.FieldCount && textBoxes[i] != null)
                                    {
                                        var value = reader[i]?.ToString(); // Get the value from reader

                                        if (string.IsNullOrEmpty(value))
                                        {
                                            textBoxes[i].Text = isTextBoxStr ? "" : "₱0.00"; // Set default value if empty or null
                                        }
                                        else if (enumColumns != null && enumColumns.ContainsKey(i) && int.TryParse(value, out int enumValue))
                                        {
                                            // Check if this field is an enum, based on the provided dictionary
                                            Type enumType = enumColumns[i];
                                            if (Enum.IsDefined(enumType, enumValue))
                                            {
                                                // If it's a valid enum value, convert it to its enum name
                                                textBoxes[i].Text = Enum.GetName(enumType, enumValue);
                                            }
                                            else
                                            {
                                                textBoxes[i].Text = enumValue.ToString(); // Default fallback
                                            }
                                        }
                                        else if (double.TryParse(value, out double number))
                                        {
                                            // If the value is numeric, convert it to comma-separated format
                                            textBoxes[i].Text = isFixedNumber ? value.ToString() : Modules.ConvertToCommaSeparated((decimal)number).ToString();
                                        }
                                        else if (decimal.TryParse(value, out decimal decimalNum))
                                        {
                                            // If the value is numeric, convert it to comma-separated format
                                            textBoxes[i].Text = isFixedNumber ? value.ToString() : Modules.ConvertToCommaSeparated(decimalNum).ToString();
                                        }
                                        else if (bool.TryParse(value, out bool boolean))
                                        {
                                            // If the value is boolean, convert it to string representation
                                            textBoxes[i].Text = boolean.ToString();
                                        }
                                        else if (DateTime.TryParse(value, out DateTime dateTime))
                                        {
                                            // If the value is a DateTime, format it
                                            textBoxes[i].Text = dateTime.ToString(dateFormat);
                                        }
                                        else
                                        {
                                            // If the value is not numeric, just display the raw value
                                            textBoxes[i].Text = value.ToString();
                                        }
                                    }
                                }
                            }
                        }

                        reader.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions here
                MessageBox.Show($"Error: {ex.Message}");
            }
        }
    }
}

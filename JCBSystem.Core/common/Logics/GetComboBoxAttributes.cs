using System.Data.Common;
using System.Data.Odbc;
using System.Threading.Tasks;
using System.Windows.Forms;
using JCBSystem.Core.common.FormCustomization;
using JCBSystem.Infrastructure.Connection.Interface;

namespace JCBSystem.Core.common.Logics
{
    public class GetComboBoxAttributes
    {
        private readonly IConnectionFactorySelector connectionFactorySelector;
        private readonly IDbConnectionFactory dbConnectionFactory;

        public GetComboBoxAttributes(IDbConnectionFactory dbConnectionFactory, IConnectionFactorySelector connectionFactorySelector)
        {
            this.dbConnectionFactory = dbConnectionFactory;
            this.connectionFactorySelector = connectionFactorySelector;
        }

        public async Task ExecuteAsync(ComboBox comboBox, string query)
        {
            comboBox.Items.Clear(); // Clear existing items

            using (var connection = dbConnectionFactory.CreateConnection())
            {
                await connectionFactorySelector.OpenConnectionAsync(connection);

                var isOdbc = connection is OdbcConnection;

                string finalQuery = Modules.ReplaceSharpWithParams(query, isOdbc);

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = finalQuery;
                    // I-execute ang query at kunin ang dataS
                    if (command is DbCommand dbCommand)
                    {
                        using (var reader = await dbCommand.ExecuteReaderAsync())
                        {
                            // Basahin ang mga resulta at idagdag sa comboBox
                            while (await reader.ReadAsync())
                            {
                                // Halimbawa, i-add ang value mula sa unang column
                                comboBox.Items.Add(reader[0].ToString());
                            }
                        }
                    }
                }
            }
        }
    }
}

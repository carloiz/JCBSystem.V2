using JCBSystem.Core.common.Interfaces;
using JCBSystem.Core.common.Logics.Handlers;
using JCBSystem.Infrastructure.Connection.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JCBSystem.Core.common.Logics
{
    public class LogicsManager : ILogicsManager
    {
        private readonly IDbConnectionFactory dbConnectionFactory;
        private readonly IConnectionFactorySelector connectionFactorySelector;

        public LogicsManager(IDbConnectionFactory dbConnectionFactory, IConnectionFactorySelector connectionFactorySelector)
        {
            this.dbConnectionFactory = dbConnectionFactory;
            this.connectionFactorySelector = connectionFactorySelector;
        }

        public Task<bool> CheckIfRecordExists(List<object> filter, string tableName, string whereCondition)
        {
            return new CheckIfRecordExists(dbConnectionFactory, connectionFactorySelector).HandleAsync(filter, tableName, whereCondition);
        }

        public Task<string> GenerateNextValuesByIdAsync(string tableName, string primaryKey, string prefix)
        {
            return new GenerateNextValues(dbConnectionFactory, connectionFactorySelector).ByIdAsync(tableName, primaryKey, prefix);
        }

        public Task<(string WithPrefix, long WithoutPrefix)> GenerateNextValuesByNumberAsync(List<object> filter, string tableName, string key, string whereCondition, string prefix = null)
        {
            return new GenerateNextValues(dbConnectionFactory, connectionFactorySelector).ByNumberAsync(filter, tableName, key, whereCondition, prefix);
        }

        public Task GetComboBoxAttributes(ComboBox comboBox, string query)
        {
            return new GetComboBoxAttributes(dbConnectionFactory, connectionFactorySelector).HandleAsync(comboBox, query);
        }

        public Task<Dictionary<string, object>> GetFieldsValues(List<object> filter, string tableName, List<string> fieldNamesQuery, List<string> fieldNames, string whereCondition)
        {
            return new GetFieldsValues(dbConnectionFactory, connectionFactorySelector).HandleAsync(filter, tableName, fieldNamesQuery, fieldNames, whereCondition);
        }

        public Task LoadDataToTextBoxes(string query, Dictionary<string, object> parameters, List<TextBox> textBoxes, bool isTextBoxStr, bool isFixedNumber, Dictionary<int, Type> enumColumns = null)
        {
            return new LoadDataToTextBoxes(dbConnectionFactory, connectionFactorySelector).HandleAsync(query, parameters, textBoxes, isTextBoxStr, isFixedNumber, enumColumns);
        }
    }
}

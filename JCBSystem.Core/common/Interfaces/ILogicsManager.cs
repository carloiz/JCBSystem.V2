using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JCBSystem.Core.common.Interfaces
{
    public interface ILogicsManager
    {
        Task<bool> CheckIfRecordExists(List<object> filter, string tableName, string whereCondition);
        Task<string> GenerateNextValuesByIdAsync(string tableName, string primaryKey, string prefix);
        Task<(string WithPrefix, long WithoutPrefix)> GenerateNextValuesByNumberAsync(
            List<object> filter,
            string tableName,
            string key,
            string whereCondition,
            string prefix = null
        );

        Task GetComboBoxAttributes(ComboBox comboBox, string query);

        Task<Dictionary<string, object>> GetFieldsValues(
            List<object> filter,
            string tableName,
            List<string> fieldNamesQuery,
            List<string> fieldNames,
            string whereCondition
        );

        Task LoadDataToTextBoxes(
            string query,
            Dictionary<string, object> parameters,
            List<TextBox> textBoxes,
            bool isTextBoxStr,
            bool isFixedNumber,
            Dictionary<int, Type> enumColumns = null
        );

    }
}

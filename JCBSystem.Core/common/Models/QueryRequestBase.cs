using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JCBSystem.Core.common.Models
{
    public class QueryRequestWithParams : QueryRequestBase
    {
        public List<object> ParameterValues { get; set; } = new List<object>();
    }

    public class QueryRequestBase
    {
        public string CountQuery { get; set; }
        public string DataQuery { get; set; }
        public DataGridView DataGrid { get; set; }
        public List<string> ImageColumns { get; set; }
        public Dictionary<string, string> CustomColumnHeaders { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}

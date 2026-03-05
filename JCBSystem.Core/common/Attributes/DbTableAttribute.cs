using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JCBSystem.Core.common.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class DbTableAttribute : Attribute
    {
        public string Name { get; }
        public bool AutoIncrement { get; set; }

        public DbTableAttribute(string name)
        {
            Name = name;
        }
    }
}

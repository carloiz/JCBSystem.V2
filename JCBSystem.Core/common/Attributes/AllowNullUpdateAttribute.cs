using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JCBSystem.Core.common.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class AllowNullUpdateAttribute : Attribute
    {
    }
}

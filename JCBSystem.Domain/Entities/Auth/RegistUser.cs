using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JCBSystem.Domain.Entities.Auth
{
    public class RegistUser
    {
        public string AuthToken { get; set; }
        public string UserNumber { get; set; }
        public string UserLevel { get; set; }
    }
}

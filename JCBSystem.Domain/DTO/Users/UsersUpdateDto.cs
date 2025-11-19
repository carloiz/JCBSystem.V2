using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JCBSystem.Domain.DTO.Users
{
    public class UserUpdateDto
    {
        public string UserNumber { get; set; }
        public bool IsSessionActive { get; set; }
        public string CurrentToken { get; set; }
    }
}

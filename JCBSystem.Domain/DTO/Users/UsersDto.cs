using JCBSystem.Core.common.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JCBSystem.Domain.DTO.Users
{
    public class UsersDto
    {
        [Key]
        public string UserNumber { get; set; }
        public string Username { get; set; }
        public string Password { get; set; } 
        public string UserLevel { get; set; } 
        public bool? Status { get; set; }
        public bool? IsSessionActive { get; set; }
        [AllowNullUpdate]
        public string CurrentToken { get; set; }
        public DateTime? RecordDate { get; set; }
    }
}

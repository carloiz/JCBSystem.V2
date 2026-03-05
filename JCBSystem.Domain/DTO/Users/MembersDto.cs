using JCBSystem.Core.common.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JCBSystem.Domain.DTO.Users
{
    [DbTable("Members")]
    public class MembersDto
    {
        [Key]
        public int Id { get; set; }
        public string Fullname { get; set; }
    }
}

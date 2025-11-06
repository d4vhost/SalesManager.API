using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesManager.UseCases.DTOs.Auth
{
    // DTO for admin to set/replace roles for a user
    public class AdminSetRolesDto
    {
        public List<string> Roles { get; set; } = new List<string>();
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesManager.UseCases.DTOs.Auth
{
    // DTO for admin to force an email change
    public class AdminChangeEmailDto
    {
        [Required]
        [EmailAddress]
        public string NewEmail { get; set; } = string.Empty;
    }
}
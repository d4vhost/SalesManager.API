using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesManager.UseCases.DTOs.Auth
{
    // DTO for admin to force a password change
    public class AdminChangePasswordDto
    {
        [Required]
        // --- MATCHING REQ 18 (PDF) ---
        [StringLength(10, MinimumLength = 4, ErrorMessage = "Password must be between 4 and 10 characters.")]
        public string NewPassword { get; set; } = string.Empty;
    }
}
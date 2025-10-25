using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesManager.UseCases.DTOs.Auth
{
    public class TokenResponseDto
    {
        public bool IsSuccess { get; set; }
        public string Token { get; set; } = "";
        public DateTime Expiration { get; set; }
        public string ErrorMessage { get; set; } = "";
    }
}
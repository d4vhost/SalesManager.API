using SalesManager.UseCases.DTOs.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesManager.UseCases.Interfaces
{
    public interface IAuthService
    {
        Task<TokenResponseDto> LoginAsync(UserLoginRequestDto loginRequest);
        Task<TokenResponseDto> RegisterAsync(UserRegisterRequestDto registerRequest, string role);
    }
}
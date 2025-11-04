using Microsoft.Extensions.DependencyInjection;
using SalesManager.UseCases.Features;
using SalesManager.UseCases.Interfaces;
using System.Reflection;
using FluentValidation;

namespace SalesManager.UseCases
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddUseCasesServices(this IServiceCollection services)
        {
            // Registrar servicios (Interactors)
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<CreateOrderInteractor>();
            services.AddScoped<IEmployeeService, EmployeeService>();

            // Registrar validadores de FluentValidation
            // Requiere "using FluentValidation;"
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            return services;
        }
    }
}
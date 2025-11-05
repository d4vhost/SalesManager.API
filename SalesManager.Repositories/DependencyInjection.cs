using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SalesManager.BusinessObjects.Entities;
using SalesManager.BusinessObjects.Interfaces;
using SalesManager.Repositories.Persistence;
using SalesManager.Repositories.Repositories;
using SalesManager.Repositories.Services;
using SalesManager.UseCases.Interfaces;
using System;

namespace SalesManager.Repositories
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddRepositoriesServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // 1. Configurar DbContext
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            // 2. Configurar Identity
            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                // --- INICIO DE MODIFICACIÓN: Requisito 18 (PDF) ---
                options.Password.RequireDigit = true;         // Requerir un número
                options.Password.RequireLowercase = true;      // Requerir una minúscula
                options.Password.RequireUppercase = true;      // Requerir una mayúscula
                options.Password.RequireNonAlphanumeric = true; // Requerir un caracter especial
                options.Password.RequiredLength = 4;          // Longitud mínima de 4
                // NOTA: La longitud máxima (10) se valida en el DTO/Validador.
                // --- FIN DE MODIFICACIÓN ---

                options.Lockout.MaxFailedAccessAttempts = 4;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            // 3. Registrar Repositorios y Unit of Work
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<ICustomerRepository, CustomerRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<ISupplierRepository, SupplierRepository>();
            services.AddScoped<IEmployeeRepository, EmployeeRepository>();

            // 4. Registrar Servicios
            services.AddSingleton<ILoggerService, LoggerService>();
            services.AddTransient<IPdfGeneratorService, PdfGeneratorService>();

            return services;
        }
    }
}
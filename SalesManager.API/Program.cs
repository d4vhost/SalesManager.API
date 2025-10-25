using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SalesManager.Repositories; // Para AddRepositoriesServices
using SalesManager.UseCases;    // Para AddUseCasesServices
using System.Text;
using Microsoft.AspNetCore.Http; // Para AddHttpContextAccessor

// --- 1. Definir el nombre de la política de CORS ---
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

var builder = WebApplication.CreateBuilder(args);

// --- 2. Agregar servicios de la capa de Repositorios ---
builder.Services.AddRepositoriesServices(builder.Configuration);

// --- 3. Agregar servicios de la capa de Casos de Uso ---
builder.Services.AddUseCasesServices();

// --- 4. Configurar autenticación JWT ---
var jwtSettings = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key no configurada");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero // Elimina el margen de 5 minutos por defecto
    };
});

// --- 5. Configurar CORS (¡CORREGIDO!) ---
builder.Services.AddCors(options =>
{
    // Cambiamos "AllowAll" por un nombre específico
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          // Permitimos SOLO el origen de tu frontend
                          policy.WithOrigins("http://localhost:5173")
                                .AllowAnyMethod()
                                .AllowAnyHeader();
                      });
});

// --- 6. Agregar controladores y Swagger ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Sales Manager API",
        Version = "v1",
        Description = "API para gestión de ventas con Northwind"
    });

    // Configurar JWT en Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingrese 'Bearer' seguido de un espacio y el token JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// --- 7. Configurar el pipeline HTTP ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// IMPORTANTE: El orden importa
// Usamos la política específica que definimos
app.UseCors(MyAllowSpecificOrigins); // Antes de Authentication

app.UseAuthentication(); // Debe ir ANTES de Authorization
app.UseAuthorization();

app.MapControllers();

app.Run();

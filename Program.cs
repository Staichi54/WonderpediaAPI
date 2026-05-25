using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using WonderpediaAPI.Data;
using WonderpediaAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Servicio de correo
builder.Services.AddScoped<EmailService>();

// Base de datos
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// CORS para permitir conexión desde Unity
builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirUnity", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configuración JWT
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

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

        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!))
    };
});

builder.Services.AddEndpointsApiExplorer();

// Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Wonderpedia API",
        Version = "v1",
        Description = "API para la gestión de usuarios, autenticación, progreso académico y envío de correos del videojuego educativo Wonderpedia."
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Ingresa el token JWT así: Bearer TU_TOKEN",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    var securityScheme = new OpenApiSecurityScheme
    {
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "Bearer"
        }
    };

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, new string[] { } }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(c =>
    {
        c.DocumentTitle = "Wonderpedia API";
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Wonderpedia API v1");
    });
}

app.UseHttpsRedirection();

app.UseCors("PermitirUnity");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.MapGet("/", () => new
{
    nombre = "Wonderpedia API",
    version = "1.0",
    descripcion = "API para el manejo de usuarios, inicio de sesión, registro, progreso académico y envío de correos del videojuego Wonderpedia.",
    modulos = new[]
    {
        "Usuarios",
        "Autenticación",
        "Progreso de Inglés",
        "Progreso de Matemáticas",
        "Progreso de Historia",
        "Envío de correo"
    }
})
.ExcludeFromDescription();

await app.RunAsync();

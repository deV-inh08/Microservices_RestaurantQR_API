using Identity.API.API.Middleware;
using Identity.API.Application.Interfaces;
using Identity.API.Application.Services;
using Identity.API.Domain.Entities;
using Identity.API.Infrastructure.Persistence;
using Identity.API.Infrastructure.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;


var builder = WebApplication.CreateBuilder(args);


// EF Core + SQL Server
builder.Services.AddDbContext<IdentityDbContext>(options =>
    // Use SQL Server with connection string from configuration
    options.UseSqlServer(builder.Configuration.GetConnectionString("IdentityDb"),
    // retry on failure
    (sql) => sql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null)
    ));

// JWT 
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
    ?? throw new InvalidOperationException("Jwt section is required in appsettings.json");
builder.Services.AddSingleton(jwtSettings);



// ─── Authentication (validate bằng AccessTokenSecret) ─
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.AccessTokenSecret)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ─── Services ────────────────────────────────────────
builder.Services.AddSingleton<IJwtUtil, JwtUtil>();
builder.Services.AddSingleton<IPasswordUtil, PasswordUtil>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AccountService>();


// ─── Controllers + Swagger ───────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();


// ─── CORS ────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

builder.Services.AddOpenApi();

var app = builder.Build();


// Migration DB + Seeding Super Admin Account
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    dbContext.Database.Migrate();

    if (!await dbContext.Accounts.AnyAsync()) // have account table
    {
        // Add seeding logic here, e.g. create a super admin account
        dbContext.Accounts.Add(new Account
        {
            Name = "Super Admin",
            Email = "superAdmin1@restaurant.com",
            Role = UserRole.SuperAdmin,
            Password = BCrypt.Net.BCrypt.HashPassword("SuperAdmin1@123678"), // Hash the password
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        await dbContext.SaveChangesAsync();

    }
}

// ─── Pipeline ────────────────────────────────────────
app.UseMiddleware<GlobalExceptionMiddleware>();


// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{

//}

app.UseCors();
app.MapOpenApi();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "Identity API");
});
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UseHttpsRedirection();

app.Run();

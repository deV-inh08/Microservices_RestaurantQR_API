using Identity.API.API.Middleware;
using System.Threading.RateLimiting;
using Identity.API.Infrastructure.BackgroundJobs;
using Microsoft.AspNetCore.RateLimiting;
using Identity.API.Application.Interfaces;
using Identity.API.Application.Services;
using Identity.API.Domain.Entities;
using Identity.API.Infrastructure.Persistence;
using Identity.API.Infrastructure.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;


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

        // Custom Forbidden Error
        options.Events = new JwtBearerEvents
        {
            OnForbidden = async (context) =>
            {
                context.Response.StatusCode = 403;
                context.Response.ContentType = "application/json";

                var response = JsonSerializer.Serialize(new
                {
                    message = "Permission denied",
                    statusCode = 403
                }, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                await context.Response.WriteAsync(response);
            }
        };
    });

builder.Services.AddAuthorization();

// ─── Rate Limiting ────────────────────────────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
    // Policy "login" — áp dụng cho POST /api/v1/auth/login
    // Cho phép 5 request/phút mỗi IP
    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0  // Không queue — reject ngay khi quá limit
            }));

    // Policy "api" — áp dụng chung cho toàn bộ API (lỏng hơn)
    options.AddPolicy("api", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    // Custom response khi bị rate limit
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        context.HttpContext.Response.ContentType = "application/json";

        var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry)
            ? (int)retry.TotalSeconds
            : 60;

        context.HttpContext.Response.Headers["Retry-After"] = retryAfter.ToString();

        await context.HttpContext.Response.WriteAsync(
            System.Text.Json.JsonSerializer.Serialize(new
            {
                message = $"Quá nhiều yêu cầu. Vui lòng thử lại sau {retryAfter} giây.",
                statusCode = 429,
                retryAfter
            }, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            }),
            cancellationToken);
    };
});

// ─── Services ────────────────────────────────────────
builder.Services.AddSingleton<IJwtUtil, JwtUtil>();
builder.Services.AddSingleton<IPasswordUtil, PasswordUtil>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AccountService>();
// Cleanup job
builder.Services.AddHostedService<RefreshTokenCleanupJob>();


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
            Email = "superadmin1@restaurant.com",
            Role = UserRole.SuperAdmin,
            Password = BCrypt.Net.BCrypt.HashPassword("SuperAdmin1@123678"),
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
//app.UseHttpsRedirection();

app.UseCors();
app.UseRateLimiter();
app.MapOpenApi();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "Identity API");
});
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

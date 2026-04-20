using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Order.API.API.Middleware;
using Order.API.Application.Interfaces;
using Order.API.Application.Service;
using Order.API.Infrastructure.ExternalServices;
using Order.API.Infrastructure.Persistence;
using Order.API.Infrastructure.Utils;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;


Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;
Microsoft.IdentityModel.Logging.IdentityModelEventSource.LogCompleteSecurityArtifact = true;
var builder = WebApplication.CreateBuilder(args);

// ─── EF Core ──────────────────────────────────────────
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("OrderDb"),
        sql => sql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null)));

// ─── Guest JWT ────────────────────────────────────────
var guestJwtSettings = builder.Configuration.GetSection("GuestJwt").Get<GuestJwtSettings>()
    ?? throw new InvalidOperationException("GuestJwt section is required");
builder.Services.AddSingleton(guestJwtSettings);
builder.Services.AddSingleton<IGuestJwtUtil, GuestJwtUtil>();

// ─── JWT (chỉ validate, không issue) ──────────────────
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];
var jwtSecret = builder.Configuration["Jwt:AccessTokenSecret"];

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
JwtSecurityTokenHandler.DefaultMapInboundClaims = false;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            RoleClaimType = "role",
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSecret!)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = ctx =>
            {

                var token = ctx.Request.Headers["Authorization"].ToString();
                return Task.CompletedTask;
            },
            OnTokenValidated = ctx =>
            {
                var claims = ctx.Principal?.Claims
                    .Select(c => $"{c.Type}={c.Value}");
                Console.WriteLine($"✅ Claims: {string.Join(", ", claims ?? [])}");
                return Task.CompletedTask;
            },
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(
                    new { message = "You are not authenticated or your token is invalid", statusCode = 401 },
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = 403;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(
                    new { message = "You do not have permission to perform this action", statusCode = 403 },
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
            }
        };
    });

builder.Services.AddAuthorization();

// ─── Services ─────────────────────────────────────────
builder.Services.AddScoped<TableService>();
builder.Services.AddScoped<GuestService>();
builder.Services.AddScoped<OrderService>();

builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(p =>
        p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
builder.Services.AddHttpClient<MenuApiClient>(client =>
{
    // Đọc từ config, không hardcode
    client.BaseAddress = new Uri(builder.Configuration["MenuApi:BaseUrl"]
        ?? throw new InvalidOperationException("MenuApi:BaseUrl is not configured"));
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    db.Database.Migrate();
}

app.UseMiddleware<GlobalExceptionMiddleware>();
//app.UseHttpsRedirection();
app.UseCors();
app.MapOpenApi();
app.UseSwaggerUI(o => o.SwaggerEndpoint("/openapi/v1.json", "Order API"));
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
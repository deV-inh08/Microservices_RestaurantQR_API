using Order.API.API.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Order.API.Application.Interfaces;
using Order.API.Application.Service;
using Order.API.Infrastructure.Persistence;
using Order.API.Infrastructure.Utils;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

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

// ─── Authentication — 2 schemes ───────────────────────
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Staff";
    options.DefaultChallengeScheme = "Staff";
    options.DefaultForbidScheme = "Staff";
})
    .AddJwtBearer("Staff", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["StaffJwt:AccessTokenSecret"]!)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["StaffJwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["StaffJwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async ctx =>
            {
                ctx.HandleResponse();
                ctx.Response.StatusCode = 401;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.WriteAsync(JsonSerializer.Serialize(
                    new { message = "Chưa đăng nhập hoặc token không hợp lệ", statusCode = 401 },
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
            },
            OnForbidden = async ctx =>
            {
                ctx.Response.StatusCode = 403;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.WriteAsync(JsonSerializer.Serialize(
                    new { message = "Không có quyền thực hiện hành động này", statusCode = 403 },
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
            }
        };
    })
    .AddJwtBearer("Guest", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(guestJwtSettings.AccessTokenSecret)),
            ValidateIssuer = true,
            ValidIssuer = guestJwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = guestJwtSettings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization(options =>
{
    // Default policy: thử cả 2 schemes
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .AddAuthenticationSchemes("Staff", "Guest")
        .RequireAuthenticatedUser()
        .Build();

    // Policy riêng cho Guest endpoints
    options.AddPolicy("GuestOnly", policy => policy
        .AddAuthenticationSchemes("Guest")
        .RequireAuthenticatedUser()
        .RequireRole("Guest"));

    // Policy riêng cho Staff endpoints
    options.AddPolicy("StaffOnly", policy => policy
        .AddAuthenticationSchemes("Staff")
        .RequireAuthenticatedUser());
});

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

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    db.Database.Migrate();
}

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseCors();
app.MapOpenApi();
app.UseSwaggerUI(o => o.SwaggerEndpoint("/openapi/v1.json", "Order API"));
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
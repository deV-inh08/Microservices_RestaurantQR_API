using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using Reservation.API.API.Middleware;
using Reservation.API.Application.Services;
using Reservation.API.Infrastructure.Persistence;

JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

var builder = WebApplication.CreateBuilder(args);

// ─── MongoDB ──────────────────────────────────────────
var mongoSettings = builder.Configuration.GetSection("MongoDb").Get<MongoDbSettings>()
    ?? throw new InvalidOperationException("MongoDb section is required in appsettings.json");
builder.Services.AddSingleton(mongoSettings);
builder.Services.AddSingleton<ReservationDbContext>();

// ─── JWT (validate only — same AccessTokenSecret as Identity.API) ─────────
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];
var jwtSecret = builder.Configuration["Jwt:AccessTokenSecret"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSecret!)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            RoleClaimType = "role",
            NameClaimType = "email"
        };

        options.Events = new JwtBearerEvents
        {
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
                    new { message = "Permission denied", statusCode = 403 },
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
            }
        };
    });

builder.Services.AddAuthorization();

// ─── Services ─────────────────────────────────────────
builder.Services.AddScoped<ReservationService>();

// ─── Controllers ──────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// ─── CORS ─────────────────────────────────────────────
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

// ─── Ensure MongoDB indexes at startup ────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ReservationDbContext>();
    var indexBuilder = Builders<Reservation.API.Domain.Entities.Reservation>.IndexKeys;

    // Index on reservationDate for date-range queries
    await db.Reservations.Indexes.CreateOneAsync(
        new CreateIndexModel<Reservation.API.Domain.Entities.Reservation>(
            indexBuilder.Descending(r => r.ReservationDate)));

    // Index on status for filtered queries
    await db.Reservations.Indexes.CreateOneAsync(
        new CreateIndexModel<Reservation.API.Domain.Entities.Reservation>(
            indexBuilder.Ascending(r => r.Status)));

    // Index on guestPhone for search
    await db.Reservations.Indexes.CreateOneAsync(
        new CreateIndexModel<Reservation.API.Domain.Entities.Reservation>(
            indexBuilder.Ascending(r => r.GuestPhone)));
}

// ─── Pipeline ─────────────────────────────────────────
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseCors();
app.MapOpenApi();
app.UseSwaggerUI(options =>
    options.SwaggerEndpoint("/openapi/v1.json", "Reservation API"));
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
// Program.cs is the application entry point.
// It registers all services with the Dependency Injection (DI) container
// and configures the middleware pipeline.
using CryptoAlertSystem.Data;
using CryptoAlertSystem.Hubs;
using CryptoAlertSystem.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ─────────────────────────────────────────
// 1. SERILOG — Structured Logging
// Writes logs to Console AND a rolling file log in /logs/
// ─────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/crypto-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7)
    .CreateLogger();

builder.Host.UseSerilog();

// ─────────────────────────────────────────
// 2. ENTITY FRAMEWORK CORE + POSTGRESQL
// Registers AppDbContext with Npgsql (PostgreSQL driver)
// ─────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ─────────────────────────────────────────
// 3. CORS — Must be registered BEFORE Authentication
// AllowAnyOrigin lets our local HTML file connect to the API and SignalR hub
// ─────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ─────────────────────────────────────────
// 4. JWT AUTHENTICATION
// Validates JWT tokens on every request to [Authorize] endpoints and the SignalR hub
// ─────────────────────────────────────────
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);

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
        IssuerSigningKey = new SymmetricSecurityKey(secretKey)
    };

    // SignalR passes the token as a query parameter (?access_token=...)
    // because browsers can't set Authorization headers on WebSocket connections
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/crypto"))
                context.Token = accessToken;

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// ─────────────────────────────────────────
// 5. MEDIATR — CQRS Command/Query Dispatcher
// Scans this assembly and registers all IRequestHandler implementations
// ─────────────────────────────────────────
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// ─────────────────────────────────────────
// 6. SIGNALR — Real-Time Messaging
// Enables WebSocket-based communication between server and browser clients
// ─────────────────────────────────────────
builder.Services.AddSignalR();

// ─────────────────────────────────────────
// 7. BACKGROUND SERVICES
// These run continuously in the background as long as the app is running
// ─────────────────────────────────────────
builder.Services.AddHostedService<CryptoIngestService>();    // Connects to Binance WebSocket
builder.Services.AddHostedService<CryptoBroadcastService>(); // Broadcasts every 500ms

// ─────────────────────────────────────────
// 8. CONTROLLERS + SWAGGER
// ─────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Crypto Alert System API", Version = "v1" });

    // Tell Swagger to send JWT token in the Authorization header for testing
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "Enter: Bearer {your_token}",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ─────────────────────────────────────────
// BUILD THE APP
// ─────────────────────────────────────────
var app = builder.Build();

// ─────────────────────────────────────────
// 9. AUTO-APPLY EF MIGRATIONS ON STARTUP
// This creates the PostgreSQL tables automatically when the app starts
// ─────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    Log.Information("Database migrations applied successfully");
}

// ─────────────────────────────────────────
// 10. MIDDLEWARE PIPELINE — ORDER IS CRITICAL!
//
// Correct order:
// UseCors → UseSerilog → UseAuthentication → UseAuthorization → MapControllers → MapHub
//
// CORS must be first so preflight OPTIONS requests are handled before
// Authentication tries to validate tokens and rejects them.
// ─────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");              // ← MUST BE FIRST

app.UseStaticFiles(); // This line — serves files from wwwroot folder

app.UseSerilogRequestLogging();       // Log every HTTP request

app.UseAuthentication();              // Validate JWT token
app.UseAuthorization();               // Check permissions

app.MapControllers();

// RequireCors on the hub ensures SignalR WebSocket upgrade requests
// also go through the CORS policy correctly
app.MapHub<CryptoHub>("/hubs/crypto").RequireCors("AllowAll");

Log.Information("CryptoAlertSystem started successfully 🚀");

app.Run();
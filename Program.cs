using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ReemRPG.Services.Interfaces;
using ReemRPG.Services;
using ReemRPG.Repositories.Interfaces;
using ReemRPG.Repositories;
using ReemRPG.Data;
using ReemRPG.Middleware;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using HealthChecks.UI.Client;
using Serilog; // Serilog for logging

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog Logging
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day) // Save logs to a file
    .CreateLogger();

builder.Host.UseSerilog(); // Use Serilog for logging

// Configure Database Connection - SQLite for cross-platform compatability 
builder.Services.AddDbContext<ApplicationContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

var smtpPassword = Environment.GetEnvironmentVariable("SmtpPassword") ?? builder.Configuration["EmailSettings:SmtpPassword"];
var jwtKey = "YourSuperSecretLongKeyWithAtLeast32Chars";  // Temporarily hardcoded for debugging


builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ICharacterService, CharacterService>();
builder.Services.AddScoped<IItemService, ItemService>();
builder.Services.AddScoped<ICharacterRepository, CharacterRepository>();

// Configure Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationContext>() // Ensure Database connectivity
    .AddCheck("CustomCheck", () =>
        HealthCheckResult.Healthy("Everything is OK!")
    );

builder.Services.AddHealthChecksUI(setup =>
{
    setup.SetEvaluationTimeInSeconds(15);
    setup.MaximumHistoryEntriesPerEndpoint(50);
}).AddInMemoryStorage();


builder.Services.AddMemoryCache();

// Configure IP Rate Limiting
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173") // Replace with your frontend's origin
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Allow credentials for cookies, authorization headers, etc.
    });
});

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
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Issuer"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});

builder.Services.AddAuthorization();
builder.Services.AddControllers();

var app = builder.Build();

app.UseCors("AllowFrontend"); // Use CORS policy

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Ensure Database Exists
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
    try
    {
        Console.WriteLine("Ensuring database is created...");
        context.Database.EnsureCreated();
        Console.WriteLine("Database creation successful!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database creation failed: {ex.Message}");
    }
}

app.UseSerilogRequestLogging(); // Enable Serilog Request Logging

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseIpRateLimiting();


app.UseHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.UseHealthChecksUI(options =>
{
    options.UIPath = "/health-ui"; // Health UI Dashboard
});

app.Use(async (context, next) =>
{
    context.Response.Headers.Remove("Content-Security-Policy"); // Remove any existing CSP

    context.Response.Headers.Add("Content-Security-Policy",
        "default-src 'self' https://localhost:7193; " + // Allow API requests
        "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " + // Allow inline scripts
        "style-src 'self' 'unsafe-inline' 'sha256-bZoZJUhp5eMZLDxVM8qSaEadRMMD/40rZDP+7VDw2QI='; " + // Allow inline styles
        "font-src 'self' data:; " + // Allow fonts
        "img-src 'self' data:; " + // Allow images
        "connect-src 'self' https://localhost:7193 http://localhost:3000; " + // Allow API requests from frontend
        "frame-ancestors 'self';"); // Prevent iframe embedding

    await next();
});


app.MapHealthChecksUI(); // Serve Health Check UI Dashboard

// Default Weather Forecast Endpoint
app.MapGet("/weatherforecast", () =>
{
    var summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        {
            Date = DateTime.Now.AddDays(index),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = summaries[Random.Shared.Next(summaries.Length)]
        }).ToArray();

    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.Run();

record WeatherForecast
{
    public DateTime Date { get; init; }
    public int TemperatureC { get; init; }
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    public string? Summary { get; init; }
}

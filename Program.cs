using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ReemRPG.Services.Interfaces;
using ReemRPG.Services;
using ReemRPG.Repositories.Interfaces;
using ReemRPG.Repositories;
using ReemRPG.Middleware;
using AspNetCoreRateLimit;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

var smtpPassword = Environment.GetEnvironmentVariable("SmtpPassword") ?? builder.Configuration["EmailSettings:SmtpPassword"];
var jwtKey = Environment.GetEnvironmentVariable("JwtKey") ?? builder.Configuration["Jwt:Key"];

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ICharacterService, CharacterService>();
builder.Services.AddScoped<IItemService, ItemService>();
builder.Services.AddScoped<ICharacterRepository, CharacterRepository>();
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();


// Add CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", builder =>
    {
        builder.WithOrigins("https://yourfrontend.com") // Replace with your actual frontend URL
               .AllowAnyMethod()
               .AllowAnyHeader();
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

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
    context.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Use CORS Middleware
app.UseCors("AllowSpecificOrigins");

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseIpRateLimiting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

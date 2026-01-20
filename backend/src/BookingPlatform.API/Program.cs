using System.Text;
using BookingPlatform.API.Extensions;
using BookingPlatform.Infrastructure;
using BookingPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add environment variables support (for Railway deployment)
builder.Configuration.AddEnvironmentVariables();

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "BookingPlatform")
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Infrastructure services (DbContext, Repositories, etc.)
builder.Services.AddInfrastructure(builder.Configuration);

// Configure JWT Authentication
// Environment variable format: JWT__SECRET (Railway uses __ for nested config)
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? Environment.GetEnvironmentVariable("JWT__SECRET")
    ?? throw new InvalidOperationException("JWT Secret is not configured. Set Jwt:Secret in appsettings or JWT__SECRET environment variable.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "BookingPlatform";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "BookingPlatformClients";

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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Configure CORS - allow any origin
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseGlobalExceptionHandler();
app.UseCorrelationId();

// Enable Swagger in all environments for now (can disable in production later)
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Booking Platform API v1");
});

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.UseTenantResolution();

app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithTags("Health");

try
{
    Log.Information("Starting Booking Platform API");

    // Run migrations and seed (seeding only adds data if not exists)
    await DatabaseSeeder.SeedAsync(app.Services);

    // Get port from environment variable (Railway sets PORT)
    var port = Environment.GetEnvironmentVariable("PORT") ?? "5001";
    var url = $"http://0.0.0.0:{port}";

    if (!app.Environment.IsDevelopment())
    {
        Log.Information("Running in production mode on {Url}", url);
        app.Urls.Clear();
        app.Urls.Add(url);
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

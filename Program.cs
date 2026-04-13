using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "QR Coupon Wallet API", Version = "v1" });

    // Add JWT bearer auth to Swagger
    // Use ApiKey scheme so Swagger Authorize box accepts the full "Bearer <token>" value.
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Scheme = "Bearer",
        Description = "Enter the authorization header value. Example: \"Bearer {token}\""
    };

    c.AddSecurityDefinition("Bearer", securityScheme);

    var securityReq = new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, new string[] { } }
    };

    c.AddSecurityRequirement(securityReq);
});

// Configuration
var configuration = builder.Configuration;
var connectionString = configuration.GetConnectionString("DefaultConnection")
                       ?? "Server=(localdb)\\mssqllocaldb;Database=QRCoupanWalletDb;Trusted_Connection=True;MultipleActiveResultSets=true";

// EF Core
builder.Services.AddDbContext<QRCoupanWalletSystem.Data.AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// Authentication - JWT
var jwtKey = configuration["Jwt:Key"] ?? "ReplaceThisWithASecretKeyForDevOnly!";
var keyBytes = System.Text.Encoding.UTF8.GetBytes(jwtKey);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(keyBytes),
        ValidateLifetime = true
    };
    options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
    {
        OnAuthenticationFailed = ctx =>
        {
            var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("JwtAuth");
            logger.LogError(ctx.Exception, "JWT authentication failed");
            return Task.CompletedTask;
        },
        OnTokenValidated = ctx =>
        {
            var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("JwtAuth");
            var sub = ctx.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            logger.LogInformation("JWT validated for subject {sub}", sub);
            return Task.CompletedTask;
        },
        OnChallenge = ctx =>
        {
            var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("JwtAuth");
            logger.LogWarning("JWT challenge: {error} {errorDescription}", ctx.Error, ctx.ErrorDescription);
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// App services
builder.Services.AddScoped<QRCoupanWalletSystem.Services.IAuthService, QRCoupanWalletSystem.Services.AuthService>();
builder.Services.AddScoped<QRCoupanWalletSystem.Services.ICouponService, QRCoupanWalletSystem.Services.CouponService>();
// additional services registered above

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Normalize Authorization header: allow pasting raw JWT into Swagger Authorize box
// If the header is present but does not include the "Bearer " prefix and looks like a JWT (contains two dots),
// prefix it so the JwtBearer middleware accepts it. This helps when Swagger sends the raw token.
app.Use(async (context, next) =>
{
    if (context.Request.Headers.TryGetValue("Authorization", out var auth) && !string.IsNullOrWhiteSpace(auth))
    {
        var val = auth.ToString();
        if (!val.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            // if value looks like a JWT (three segments separated by dots)
            var segments = val.Split('.');
            if (segments.Length == 3)
            {
                context.Request.Headers["Authorization"] = "Bearer " + val;
            }
        }
    }
    await next();
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

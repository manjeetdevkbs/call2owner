using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Oversight;
using Oversight.Controllers;
using Oversight.Services;
using RestSharp;
using System.Reflection;
using System.Text;
using Utilities;
using Microsoft.EntityFrameworkCore;

var publicEndpoints = new HashSet<string>
{
    "/swagger/index.html",
    "/swagger/docs/v1/apigateway",
    "/swagger/v1/swagger.json",
    "/api/auth/login",
    "/api/auth/resend-verification-email",
    "/api/auth/send-reset-link",
    "/api/auth/set-password",
    "/api/RoleClaims/roles-permissions",
    "/api/auth/getAllUsers"
};

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationInsightsTelemetry(options =>
{
    builder.Configuration.Bind("ApplicationInsights", options);
});

// Add services to the container.
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "AllowOrigin",
        builder =>
        {
            builder.WithOrigins("https://localhost:44351", "http://112.196.3.222:8081")
                                .AllowAnyHeader()
                                .AllowAnyMethod();
        });
});


builder.Services.AddDbContext<DataContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("MailSettings"));

var key = Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Key"] ?? throw new InvalidOperationException("JWT Secret Key not found!"));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            RequireExpirationTime = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddScoped<EmailService>();

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddAuthorization(options =>
{
    // Get all public constants from ModulePermissions
    var modules = typeof(Utilities.Module)
        .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
        .Where(fi => fi.IsLiteral && !fi.IsInitOnly) // Ensures it's a constant
        .Select(fi => fi.GetValue(null)?.ToString()) // Get the value of the constant
        .Where(value => !string.IsNullOrEmpty(value)) // Ensure it's not null or empty
        .ToList();

    // Add each permission as a policy
    foreach (var item in modules)
    {
        options.AddPolicy(item, policy =>
            policy.Requirements.Add(new PermissionRequirement(item)));
    }

    // Get all public constants from ModulePermissions
    var permissions = typeof(Utilities.Permission)
        .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
        .Where(fi => fi.IsLiteral && !fi.IsInitOnly) // Ensures it's a constant
        .Select(fi => fi.GetValue(null)?.ToString()) // Get the value of the constant
        .Where(value => !string.IsNullOrEmpty(value)) // Ensure it's not null or empty
        .ToList();

    // Add each permission as a policy
    foreach (var permission in permissions)
    {
        options.AddPolicy(permission, policy =>
            policy.Requirements.Add(new PermissionRequirement(permission)));
    }
});

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Call2Owner.API", Version = "v1" });

    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer {token}' (without quotes)"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Register the PermissionRequirement handler
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

builder.Services.AddSingleton<IEmailServiceClient, EmailServiceClient>();
builder.Services.AddScoped<IEmailTriggerService, EmailTriggerService>();
builder.Services.AddSingleton(sp => new RestClient());

var app = builder.Build();

//app.Use(async (context, next) =>
//{
//    if (context.Request.Path.StartsWithSegments("/swagger") ||
//        context.Request.Path.StartsWithSegments("/swagger/index.html") ||
//        context.Request.Path.StartsWithSegments("/swagger/v1/swagger.json"))
//    {
//        await next();
//        return;
//    }

//    //if (!context.Request.Headers.ContainsKey("X-From-Gateway") ||
//    //    context.Request.Headers["X-From-Gateway"] != "true")
//    //{
//    //    context.Response.StatusCode = StatusCodes.Status403Forbidden;
//    //    await context.Response.WriteAsync("Access denied. Use API Gateway.");
//    //    return;
//    //}

//    await next();
//});

app.UseCors(options => options.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    // Shows UseCors with CorsPolicyBuilder.
    app.UseCors(builder =>
    {
        builder
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader();
    });
    app.UseSwagger();

    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
    await AuthController.SeedSuperAdminAsync(dbContext);
}

app.UseHttpsRedirection();

// Authentication must come before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.ToLower();

    if (publicEndpoints.Contains(path))
    {
        // Mark as authenticated for public endpoints
        context.User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity());
        await next();
        return;
    }

    await next();
});

app.MapControllers();

app.Run();
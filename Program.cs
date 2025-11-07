using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Rently.Api.Data;
using Azure.Storage.Blobs;
using Rently.Api.Hubs;
using Rently.Api.Services;
using QuestPDF.Infrastructure;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Threading.Tasks;
using System;

var builder = WebApplication.CreateBuilder(args);

// -----------------------------
// 🔐 JWT Settings
// -----------------------------
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var jwtKey = jwtSettings["SecretKey"];

// -----------------------------
// 🗄️ Database
// -----------------------------
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// -----------------------------
// 🌍 Controllers & CORS
// -----------------------------
builder.Services.AddControllers();
builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalDevCors", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// -----------------------------
// ☁️ Azure Blob & Invoice Services
// -----------------------------
builder.Services.AddSingleton(x =>
    new BlobServiceClient(builder.Configuration["AzureBlob:ConnectionString"]));
builder.Services.AddScoped<AzureBlobService>();
builder.Services.AddScoped<InvoiceService>();

// -----------------------------
// 🔔 Alert & Notification Services
// -----------------------------
builder.Services.AddScoped<AlertService>();              // your custom service
builder.Services.AddHostedService<AlertBackgroundService>(); // runs periodically

// -----------------------------
// 📘 Swagger
// -----------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// -----------------------------
// 🔑 Authentication (JWT)
// -----------------------------
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // ✅ allow HTTP for local dev
    options.SaveToken = true;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
        ClockSkew = TimeSpan.Zero
    };

    // ✅ Allow SignalR to use access_token in query string
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) &&
                (path.StartsWithSegments("/hubs/analytics") || path.StartsWithSegments("/hubs/alerts")))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});

// -----------------------------
// ⚙️ Authorization Policies
// -----------------------------
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("ManagerOrAdmin", policy => policy.RequireRole("Admin", "Manager"));
});

// -----------------------------
// 🧩 External Services
// -----------------------------
builder.Services.AddHttpClient<PricingService>();
builder.Services.AddScoped<PricingService>();

// -----------------------------
// 🧾 QuestPDF License
// -----------------------------
QuestPDF.Settings.License = LicenseType.Community;

// -----------------------------
// 🚀 Build App
// -----------------------------
var app = builder.Build();

// -----------------------------
// ⚙️ Middleware Pipeline
// -----------------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseCors("LocalDevCors");
app.UseAuthentication();
app.UseAuthorization();

// -----------------------------
// 🧠 Endpoints
// -----------------------------
app.MapControllers();

// ✅ Map SignalR hubs directly (new .NET 8 style)
app.MapHub<AnalyticsHub>("/hubs/analytics");
app.MapHub<AlertHub>("/hubs/alerts");

// -----------------------------
// 🪵 Logging
// -----------------------------
app.Logger.LogInformation("✅ Hubs running:");
app.Logger.LogInformation("   • AnalyticsHub: /hubs/analytics");
app.Logger.LogInformation("   • AlertHub:     /hubs/alerts");
app.Logger.LogInformation("🚀 Rently API started successfully!");

// -----------------------------
// ▶️ Run the App
// -----------------------------
var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
app.Run($"http://0.0.0.0:{port}");


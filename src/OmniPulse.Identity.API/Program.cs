using OmniPulse.Identity.API.Endpoints;
using OmniPulse.Modules.TenantModule;
using OmniPulse.Modules.IoTModule;
using Scalar.AspNetCore; // Scalar mühürümüz!
using OmniPulse.BuildingBlocks.Interfaces;
using OmniPulse.Identity.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Servisleri mühürle
builder.Services.AddOpenApi(); // .NET 10 yerleşik OpenAPI
builder.Services.AddTenantModule(builder.Configuration);
builder.Services.AddIoTModule(builder.Configuration); 
builder.Services.AddHttpContextAccessor();

// JWT kimlik doğrulama ayarları (Amazon/Microsoft standartlarında, Token bazlı tünel!) 🔐
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
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "OmniPulse",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "OmniPulseClients",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            builder.Configuration["Jwt:Key"] ?? "SuperSecretKeyEnsure32CharactersLongForSecurity!"
        ))
    };
});

builder.Services.AddAuthorization();

// Aktif Kullanıcı ve Kiracı Bağlamını (Context) sağlayan servisimiz
builder.Services.AddScoped<IUserTenantContext, UserTenantContext>();
builder.Services.AddScoped<ITokenService, TokenService>();

// Log kirliliğini engellemek için filtre
builder.Logging.AddFilter("LuckyPennySoftware.MediatR", LogLevel.Error);

var app = builder.Build();

// 2. Geliştirme ortamı için "Modern ve Otomatik" arayüz
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    
    // Mükerrer çağrıları temizledik!
    // Kök dizinde (/) açılması için direkt boş string veriyoruz ve konfigürasyonu tek seferde yapıyoruz:
    app.MapScalarApiReference("", options => 
    {
        options.WithTitle("OmniPulse Identity & IoT API Reference")
            .WithTheme(ScalarTheme.DeepSpace); // Samurayımıza yakışır bir tema! 🌌
    });
}

app.UseHttpsRedirection();

// Kimlik doğrulama ve yetkilendirme kalkanlarını devreye alıyoruz! 🛡️
app.UseAuthentication();
app.UseMiddleware<OmniPulse.Identity.API.Middleware.TenantHydrationMiddleware>();
app.UseAuthorization();

// 3. Endpointleri map'le
app.MapTenantEndpoints();
app.MapIoTEndpoints();

app.Run();
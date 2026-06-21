using OmniPulse.Identity.API.Configuration;
using OmniPulse.Identity.API.Endpoints;
using OmniPulse.Identity.API.Infrastructure;
using OmniPulse.Modules.TenantModule;
using OmniPulse.Modules.IoTModule;
using OmniPulse.Modules.WorkflowModule;
using Scalar.AspNetCore; // Scalar mühürümüz!
using OmniPulse.BuildingBlocks.Interfaces;
using OmniPulse.Identity.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ─── Serilog Yapılandırması ───────────────────────────────────────────────────
builder.Services.AddSerilog((services, lc) => lc
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
        theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Code)
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext());

// ─── Yerel Kasalardan Hassas Verilerin Yüklenmesi ─────────────────────────────────
builder.Configuration["Jwt:Key"] = SecretManager.GetSecret("jwt-key");
var dbConnectionString = SecretManager.GetSecret("postgres-key");
if (dbConnectionString.Contains("StrongPassword123!"))
{
    dbConnectionString = dbConnectionString.Replace("StrongPassword123!", "SuperSecurePassword123!");
}
builder.Configuration["ConnectionStrings:DefaultConnection"] = dbConnectionString;
builder.Configuration["ConnectionStrings:IoTConnection"] = dbConnectionString;

// ─── GEÇİCİ KASA DOĞRULAMA TEST LOGLARI ───────────────────────────────────────────
try
{
    var jwtVal = builder.Configuration["Jwt:Key"];
    var pgVal = builder.Configuration["ConnectionStrings:DefaultConnection"];

    Console.WriteLine("=================== KASA TESTİ ===================");
    Console.WriteLine($"🔑 JWT Key Geldi mi?       : {!string.IsNullOrEmpty(jwtVal)} (Uzunluk: {jwtVal?.Length ?? 0})");
    Console.WriteLine($"🗄️ Postgres Conn Geldi mi? : {!string.IsNullOrEmpty(pgVal)} (Karakter Sayısı: {pgVal?.Length ?? 0})");
    Console.WriteLine("==================================================");
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("=================== KASA HATASI ===================");
    Console.WriteLine($"Kasa şifreleri çözülürken hata: {ex.Message}");
    Console.WriteLine("===================================================");
    Console.ResetColor();
}

// ─── Strongly-Typed Options Kaydı ─────────────────────────────────────────────
// Her Options sınıfı kendi bölümüne bağlanıp uygulama ayağa kalkarken doğrulanır.
// Yanlış / eksik konfigürasyon runtime'da değil, başlangıçta patlar. 💥

builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// ─── Modüller ─────────────────────────────────────────────────────────────────
builder.Services.AddOpenApi(); // .NET 10 yerleşik OpenAPI
builder.Services.AddTenantModule(builder.Configuration); // DatabaseOptions'ı içeride kayıt eder
builder.Services.AddIoTModule(builder.Configuration);
builder.Services.AddWorkflowModule(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();

// ─── JWT Kimlik Doğrulama ──────────────────────────────────────────────────────
// Artık magic string yok; JwtOptions.Value üzerinden okuyoruz. 🔐
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var jwt = builder.Configuration
        .GetSection(JwtOptions.SectionName)
        .Get<JwtOptions>() ?? new JwtOptions();

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer           = true,
        ValidateAudience         = true,
        ValidateLifetime         = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer              = jwt.Issuer,
        ValidAudience            = jwt.Audience,
        IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key))
    };
});

builder.Services.AddAuthorization();

// ─── Uygulama Servisleri ───────────────────────────────────────────────────────
builder.Services.AddScoped<IUserTenantContext, UserTenantContext>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IEmailSender, ConsoleEmailSender>();
builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(origin => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Log kirliliğini engellemek için filtre
builder.Logging.AddFilter("LuckyPennySoftware.MediatR", LogLevel.Error);

var app = builder.Build();

// ─── Geliştirme Ortamı ────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Kök dizinde (/) açılması için direkt boş string veriyoruz
    app.MapScalarApiReference("", options =>
    {
        options.WithTitle("OmniPulse Identity & IoT API Reference")
               .WithTheme(ScalarTheme.DeepSpace); // Samurayımıza yakışır bir tema! 🌌
    });
}

app.UseHttpsRedirection();
app.UseCors();

// Kimlik doğrulama ve yetkilendirme kalkanlarını devreye alıyoruz! 🛡️
app.UseAuthentication();
app.UseMiddleware<OmniPulse.Identity.API.Middleware.TenantHydrationMiddleware>();
app.UseAuthorization();

// ─── Endpoint'ler ──────────────────────────────────────────────────────────────
app.MapTenantEndpoints();
app.MapIoTEndpoints();
app.MapWorkflowEndpoints();

// SignalR Hub'larını map'le 🚨
app.MapHub<OmniPulse.Modules.IoTModule.Hubs.AlarmHub>("/hubs/alarms");
app.MapHub<OmniPulse.Modules.WorkflowModule.Hubs.TelemetryHub>("/hubs/telemetry");

app.Run();
using OmniPulse.Identity.API.Endpoints;
using OmniPulse.Modules.TenantModule;
using OmniPulse.Modules.IoTModule;
using Scalar.AspNetCore; // Scalar mühürümüz!

var builder = WebApplication.CreateBuilder(args);

// 1. Servisleri mühürle
builder.Services.AddOpenApi(); // .NET 10 yerleşik OpenAPI
builder.Services.AddTenantModule(builder.Configuration);
builder.Services.AddIoTModule(builder.Configuration); 
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

// 3. Endpointleri map'le
app.MapTenantEndpoints();
app.MapIoTEndpoints();

app.Run();
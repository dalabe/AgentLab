using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Procesos.Security;
using Procesos.TokenService;
using Procesos.TokenService.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<RegisteredServiceClientsOptions>(builder.Configuration.GetSection(RegisteredServiceClientsOptions.SectionName));

// Rate limiting sobre el endpoint de emisión de tokens: mitiga fuerza bruta / credential
// stuffing contra client_secret. Ver docs/SECURITY.md.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter(RateLimiting.TokenEndpointPolicy, limiterOptions =>
    {
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.PermitLimit = 30;
        limiterOptions.QueueLimit = 0;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
});

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseRateLimiter();

app.MapTokenEndpoint();

app.Run();

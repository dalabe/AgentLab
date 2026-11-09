using ModelContextProtocol.AspNetCore;
using Procesos.Data;
using Procesos.McpServer.Tools;
using Procesos.Security;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddProcesosData(builder.Configuration);

// Procesos.McpServer es el "recurso protegido": solo acepta llamadas con un JWT válido emitido
// por Procesos.TokenService (flujo OAuth2 client_credentials). No tiene CORS habilitado a
// propósito: nunca debe ser llamado directamente desde un navegador, solo servicio-a-servicio
// desde Procesos.ChatApi. Ver docs/SECURITY.md.
builder.Services.AddProcesosResourceAuthentication(builder.Configuration);

builder.Services
    .AddMcpServer()
    .WithHttpTransport(options => options.SessionMode = HttpServerSessionMode.Stateless)
    .WithTools<ProcesoTools>();

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseAuthentication();
app.UseAuthorization();

app.MapMcp().RequireAuthorization(McpSecurityDefaults.McpInvokePolicy);

app.Run();

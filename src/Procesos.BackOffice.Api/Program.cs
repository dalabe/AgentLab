using System.Text.Json.Serialization;
using Procesos.BackOffice.Api.Endpoints;
using Procesos.Data;
using Procesos.Data.Seed;
using Procesos.Data.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddProcesosData(builder.Configuration);

// Los DTOs de alta/edición (ProcesoUpsertDto, EventoUpsertDto) reciben enums (TipoProceso,
// EstadoProceso, TipoEvento) directamente desde el body JSON del BackOffice Angular como texto
// (p.ej. "Civil"), no como el valor numérico subyacente. Igual que ya hacemos al guardarlos en
// la base de datos (HasConversion<string>() en ProcesosDbContext), serializamos/deserializamos
// enums como string en toda la API para que sea consistente y legible de punta a punta.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

const string BackOfficeWebCorsPolicy = "BackOfficeWeb";
var allowedOrigins = builder.Configuration.GetSection("BackOffice:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:4300"];

builder.Services.AddCors(options =>
{
    // Esta API sí es llamada desde un navegador (el BackOffice Angular), así que CORS debe
    // estar habilitado, pero restringido a los orígenes conocidos del BackOffice (nunca "*").
    options.AddPolicy(BackOfficeWebCorsPolicy, policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseCors(BackOfficeWebCorsPolicy);

app.MapPersonasEndpoints();
app.MapProcesosEndpoints();
app.MapDocumentosEndpoints();
app.MapEventosEndpoints();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ProcesosDbContext>();
    var pathResolver = scope.ServiceProvider.GetRequiredService<DocumentPathResolver>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    await DbSeeder.SeedAsync(db, pathResolver, logger);
}

app.Run();

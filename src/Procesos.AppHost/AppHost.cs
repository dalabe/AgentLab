var builder = DistributedApplication.CreateBuilder(args);

// Nota de diseño: este laboratorio usa SQL Server LocalDB real (no un contenedor de Aspire) y
// cada servicio .NET resuelve su propia configuración (connection string, endpoints, secretos)
// desde su propio appsettings.json + User Secrets, igual que si se ejecutara suelto con
// `dotnet run`. Aspire aquí solo orquesta el arranque (orden, un solo F5, dashboard de logs);
// no reemplaza la configuración de cada proyecto. Ver docs/RUNBOOK.md.

var tokenService = builder.AddProject<Projects.Procesos_TokenService>("tokenservice");

builder.AddProject<Projects.Procesos_McpServer>("mcpserver")
    .WaitFor(tokenService);

builder.AddProject<Projects.Procesos_BackOffice_Api>("backoffice-api");

builder.AddProject<Projects.Procesos_ChatApi>("chatapi")
    .WaitFor(tokenService);

// Nota: se evaluó orquestar también las dos apps Angular aquí vía AddJavaScriptApp (paquete
// Aspire.Hosting.JavaScript). En pruebas, el proceso "ng serve" lanzado por esa integración se
// cerraba solo ~2 segundos después de arrancar (reproducible), mientras que los 4 servicios .NET
// de arriba arrancan y permanecen estables de forma confiable bajo Aspire. Por eso las apps
// Angular se corren aparte con `npm start` (ver docs/RUNBOOK.md) en vez de depender de esa
// integración todavía inmadura para Angular CLI.

builder.Build().Run();

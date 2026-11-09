using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Procesos.Data.Storage;

namespace Procesos.Data;

public static class ServiceCollectionExtensions
{
    public const string ConnectionStringName = "ProcesosDb";

    /// <summary>
    /// Registra el <see cref="ProcesosDbContext"/> (SQL Server LocalDB) y las opciones de
    /// almacenamiento de documentos. Se usa tanto en Procesos.BackOffice.Api (lectura/escritura,
    /// dueño de migraciones) como en Procesos.McpServer (solo lectura funcional).
    /// </summary>
    public static IServiceCollection AddProcesosData(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Falta la connection string '{ConnectionStringName}' en configuración (ConnectionStrings:{ConnectionStringName}).");

        services.AddDbContext<ProcesosDbContext>(options => options.UseSqlServer(connectionString));

        services.AddOptions<DocumentStorageOptions>()
            .Configure(options =>
            {
                var configuredRoot = configuration[$"{DocumentStorageOptions.SectionName}:RootPath"];
                options.RootPath = string.IsNullOrWhiteSpace(configuredRoot)
                    ? RepoPaths.ResolveMockDocumentsRoot()
                    : configuredRoot;
            });

        services.AddSingleton<DocumentPathResolver>();

        return services;
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Procesos.Data;

/// <summary>
/// Factory usada solo en tiempo de diseño por las herramientas de EF Core (<c>dotnet ef migrations add</c>,
/// <c>dotnet ef database update</c>) para poder generar/aplicar migraciones directamente sobre este
/// proyecto, sin depender de un proyecto de inicio (startup project) completo.
/// La connection string real en tiempo de ejecución la provee cada API vía <see cref="ServiceCollectionExtensions"/>.
/// </summary>
public sealed class ProcesosDbContextFactory : IDesignTimeDbContextFactory<ProcesosDbContext>
{
    public const string DesignTimeConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=ProcesosLegalesLab;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

    public ProcesosDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ProcesosDbContext>();
        optionsBuilder.UseSqlServer(DesignTimeConnectionString);
        return new ProcesosDbContext(optionsBuilder.Options);
    }
}

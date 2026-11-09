namespace Procesos.Data;

/// <summary>
/// Ubica la raíz del repositorio en tiempo de ejecución buscando hacia arriba el archivo de
/// solución. Permite que cada servicio funcione con valores por defecto razonables cuando se
/// ejecuta suelto (fuera de Aspire), sin necesitar rutas absolutas hardcodeadas por desarrollador.
/// Cuando se corre bajo Procesos.AppHost, este valor se sobreescribe explícitamente vía la
/// variable de entorno/configuración "DocumentStorage:RootPath".
/// </summary>
public static class RepoPaths
{
    private const string SolutionFileName = "ProcesosLegalesLab.slnx";

    public static string FindRepoRoot(string? startDirectory = null)
    {
        var dir = new DirectoryInfo(startDirectory ?? AppContext.BaseDirectory);

        while (dir is not null)
        {
            if (dir.GetFiles(SolutionFileName).Length > 0)
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException(
            $"No se pudo localizar la raíz del repositorio (buscando '{SolutionFileName}') a partir de " +
            $"'{startDirectory ?? AppContext.BaseDirectory}'. Configure 'DocumentStorage:RootPath' explícitamente.");
    }

    public static string ResolveMockDocumentsRoot(string? startDirectory = null)
        => Path.Combine(FindRepoRoot(startDirectory), "mock-data", "documentos");
}

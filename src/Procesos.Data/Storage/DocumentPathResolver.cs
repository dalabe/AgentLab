using Microsoft.Extensions.Options;

namespace Procesos.Data.Storage;

/// <summary>
/// Resuelve rutas relativas de documentos a rutas físicas absolutas, garantizando que el
/// resultado nunca salga de la carpeta raíz configurada (mitigación de path traversal, p.ej.
/// rutas con "..\\..\\" intentando escapar del almacenamiento permitido).
/// </summary>
public sealed class DocumentPathResolver
{
    private readonly string _root;

    public DocumentPathResolver(IOptions<DocumentStorageOptions> options)
    {
        if (string.IsNullOrWhiteSpace(options.Value.RootPath))
        {
            throw new InvalidOperationException(
                "DocumentStorage:RootPath no está configurado. Ver appsettings.json o Procesos.AppHost.");
        }

        _root = Path.GetFullPath(options.Value.RootPath);
    }

    public string RootPath => _root;

    /// <summary>
    /// Convierte una ruta relativa almacenada en la base de datos en una ruta física absoluta.
    /// Lanza <see cref="UnauthorizedAccessException"/> si el resultado escapa de la raíz permitida.
    /// </summary>
    public string ResolveFullPath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new ArgumentException("La ruta relativa del documento no puede ser vacía.", nameof(relativePath));
        }

        var normalizedRelative = relativePath.Replace('/', Path.DirectorySeparatorChar);
        var combined = Path.GetFullPath(Path.Combine(_root, normalizedRelative));

        var rootWithSeparator = _root.EndsWith(Path.DirectorySeparatorChar)
            ? _root
            : _root + Path.DirectorySeparatorChar;

        if (!combined.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(combined, _root, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException(
                $"La ruta '{relativePath}' resuelve fuera de la carpeta de almacenamiento de documentos permitida.");
        }

        return combined;
    }

    public void EnsureRootExists() => Directory.CreateDirectory(_root);
}

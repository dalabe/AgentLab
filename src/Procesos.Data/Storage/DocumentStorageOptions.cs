namespace Procesos.Data.Storage;

/// <summary>Opciones de configuración para la carpeta física de documentos de procesos.</summary>
public sealed class DocumentStorageOptions
{
    public const string SectionName = "DocumentStorage";

    /// <summary>Carpeta raíz absoluta donde viven los documentos físicos (mock-data/documentos).</summary>
    public string RootPath { get; set; } = string.Empty;
}
